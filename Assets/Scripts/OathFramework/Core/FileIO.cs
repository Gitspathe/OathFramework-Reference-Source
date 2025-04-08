using Cysharp.Threading.Tasks;
using OathFramework.Pooling;
using OathFramework.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Text;
using UnityEngine;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace OathFramework.Core
{ 

    public static class FileIO
    {
        public static readonly string DataPath       = $"{Application.dataPath}{Path.DirectorySeparatorChar}";
        public static readonly string PersistentPath = Application.persistentDataPath;
        public static readonly string SavePath       = $"{PersistentPath}{Path.DirectorySeparatorChar}data{Path.DirectorySeparatorChar}";
        public static readonly string SnapshotPath   = $"{SavePath}snapshots{Path.DirectorySeparatorChar}";

        private static int bufferLength;
        private static int bufferCount;
        
        private static Dictionary<string, CancellationTokenSource> fileHandles = new();

        private static UTF8Encoding utf8Encoding;
        private static Encoder utf8Encoder;
        private static ArrayPool<byte> byteBuffers;
        private static ArrayPool<char> charBuffers;
        private static bool initialized;
        private static bool useThreadPoolOverride = true;
        private static bool useCompressionOverride = true;

        public static int DefaultBufferLength {
            get {
#if UNITY_IOS || UNITY_ANDROID
                return 1024;
#else
                return 4096;
#endif
            }
        }
        
        public static int DefaultBufferCount {
            get {
#if UNITY_IOS || UNITY_ANDROID
                return 8;
#else
                return 16;
#endif
            }
        }
        
        private static bool IsBufferAvailable => byteBuffers.Count > 0 && charBuffers.Count > 0;

        public static void Initialize()
        {
            if(initialized)
                return;
            
            if(!Directory.Exists(SavePath)) {
                Directory.CreateDirectory(SavePath);
            }
            if(!Directory.Exists(SnapshotPath)) {
                Directory.CreateDirectory(SnapshotPath);
            }

            bufferLength = DefaultBufferLength;
            bufferCount  = DefaultBufferCount;
            if(INISettings.GetNumeric("FileIO/BufferLength", out int val)) {
                bufferLength = Mathf.Clamp(val, 32, ushort.MaxValue);
            }
            if(INISettings.GetNumeric("FileIO/BufferCount", out val)) {
                bufferCount = Mathf.Clamp(val, 1, 1024);
            }
            if(INISettings.GetBool("FileIO/UseThreadPool") == false) {
                useThreadPoolOverride = false;
            }
            if(INISettings.GetBool("FileIO/UseCompression") == false) {
                useCompressionOverride = false;
            }

            utf8Encoding = new UTF8Encoding();
            utf8Encoder  = utf8Encoding.GetEncoder();
            byteBuffers  = new ArrayPool<byte>(bufferLength, bufferCount);
            charBuffers  = new ArrayPool<char>(bufferLength, bufferCount);
            initialized  = true;

            long size = bufferLength * bufferCount * 3;
            Debug.Log($"FileIO initialized - Buffers: {bufferCount}, total {size} bytes.");
        }

        public static async UniTask SaveFile(string path, string data, bool compress = false, bool noHeader = false)
        {
            if(compress && !useCompressionOverride) {
                compress = false;
            }
            
            Initialize();
            string file = StringBuilderCache.Retrieve.Append(path).ToString();
            while(!IsBufferAvailable || fileHandles.TryGetValue(file, out CancellationTokenSource _)) {
                await UniTask.Yield();
            }
            CancellationTokenSource newCts = new();
            fileHandles.Add(file, newCts);
            
            char[] charBuffer = charBuffers.Retrieve();
            byte[] byteBuffer = byteBuffers.Retrieve();
            DeflateStream ds  = null;
            try {
                await using FileStream fs = new(file, FileMode.Create, FileAccess.Write, FileShare.None);
                if(!noHeader) {
                    await WriteHeader(charBuffer, byteBuffer, fs, compress);
                }
                if(compress) {
                    ds = new DeflateStream(fs, CompressionLevel.Optimal, leaveOpen: true);
                }
                
                Stream targetStream = compress ? ds : fs;
                
#if !UNITY_WEBGL
                // Prevent unity frame timing bottleneck.
                if(useThreadPoolOverride) {
                    await UniTask.SwitchToThreadPool();
                }
#endif
                
                int charIndex       = 0;
                int charsRemaining  = data.Length;
                while(charsRemaining > 0) {
                    int chunkSize = Mathf.Min(charBuffer.Length, charsRemaining);
                    data.CopyTo(charIndex, charBuffer, 0, chunkSize);
                    int encodedSize = utf8Encoder.GetBytes(charBuffer, 0, chunkSize, byteBuffer, 0, charsRemaining == chunkSize);
                    await targetStream.WriteAsync(byteBuffer, 0, encodedSize, newCts.Token);
                    charIndex      += chunkSize;
                    charsRemaining -= chunkSize;
                }
                
#if !UNITY_WEBGL
                if(useThreadPoolOverride) {
                    await UniTask.SwitchToMainThread();
                }
#endif
                
                await targetStream.FlushAsync(newCts.Token);
                
                // Have to dispose / flush here because finally{} is retarded with async.
                if(ds != null) {
                    await ds.DisposeAsync();
                }
            } catch(Exception e) {
                Debug.LogError(e);
            } finally {
                await UniTask.SwitchToMainThread();
                newCts.Dispose();
                fileHandles.Remove(file);
                charBuffers.Return(charBuffer);
                byteBuffers.Return(byteBuffer);
                if(ds != null) {
                    await ds.DisposeAsync();
                }
            }
        }

        public static async UniTask<string> LoadFile(string path, bool? compressed = null, bool noHeader = false)
        {
            Initialize();
            string file = StringBuilderCache.Retrieve.Append(path).ToString();
            if(!File.Exists(file))
                throw new FileNotFoundException($"File '{path}' not found.");
            
            while(!IsBufferAvailable || fileHandles.TryGetValue(file, out CancellationTokenSource _)) {
                await UniTask.Yield();
            }
            CancellationTokenSource newCts = new();
            fileHandles.Add(file, newCts);
            
            char[] charBuffer    = charBuffers.Retrieve();
            byte[] byteBuffer    = byteBuffers.Retrieve();
            DeflateStream ds     = null;
            StringBuilder result = new();
            try {
                bool isCompressed         = compressed ?? false;
                await using FileStream fs = new(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                if(!noHeader) {
                    isCompressed = await ReadHeader(charBuffer, byteBuffer, fs);
                }
                if(isCompressed) {
                    ds = new DeflateStream(fs, CompressionMode.Decompress, leaveOpen: true);
                }
                
                Stream sourceStream = isCompressed ? ds : fs;
                
#if !UNITY_WEBGL
                // Prevent unity frame timing bottleneck.
                if(useThreadPoolOverride) {
                    await UniTask.SwitchToThreadPool();
                }
#endif
                
                int byteCount;
                while((byteCount = await sourceStream.ReadAsync(byteBuffer, 0, byteBuffer.Length, newCts.Token)) > 0) {
                    int charsDecoded = utf8Encoding.GetChars(byteBuffer, 0, byteCount, charBuffer, 0);
                    result.Append(charBuffer, 0, charsDecoded);
                }
                
#if !UNITY_WEBGL
                if(useThreadPoolOverride) {
                    await UniTask.SwitchToMainThread();
                }
#endif
                
                // Have to dispose / flush here because finally{} is retarded with async.
                if(ds != null) {
                    await ds.DisposeAsync();
                }
            } catch(Exception e) {
                Debug.LogError(e);
            } finally {
                await UniTask.SwitchToMainThread();
                newCts.Dispose();
                fileHandles.Remove(file);
                charBuffers.Return(charBuffer);
                byteBuffers.Return(byteBuffer);
                if(ds != null) {
                    await ds.DisposeAsync();
                }
            }
            return result.ToString();
        }

        public static bool FileExists(string path)
        {
            Initialize();
            string file = StringBuilderCache.Retrieve.Append(path).ToString();
            return File.Exists(file);
        }

        private static async UniTask<bool> ReadHeader(char[] cBuffer, byte[] bBuffer, Stream stream)
        {
            int bytesRead = await stream.ReadAsync(bBuffer, 0, 9);
            if(bytesRead < 9) {
                return false;
            }
            
            Encoding.UTF8.GetChars(bBuffer.AsSpan(0, 9), cBuffer.AsSpan(0, 9));
            return cBuffer[6] == 'C';
        }

        private static async UniTask WriteHeader(char[] cBuffer, byte[] bBuffer, Stream stream, bool compress)
        {
            cBuffer[0] = '!';
            cBuffer[1] = 'O';
            cBuffer[2] = 'A';
            cBuffer[3] = 'T';
            cBuffer[4] = 'H';
            cBuffer[5] = ' ';
            cBuffer[6] = compress ? 'C' : '-';
            cBuffer[7] = '\n';
            cBuffer[8] = '\n';
            Encoding.UTF8.GetBytes(cBuffer.AsSpan(0, 9), bBuffer.AsSpan(0, 9));
            await stream.WriteAsync(bBuffer, 0, 9);
            await stream.FlushAsync();
        }
    }
    
    public enum LoadResult
    {
        Success,
        Backup,
        Fail
    }

}
