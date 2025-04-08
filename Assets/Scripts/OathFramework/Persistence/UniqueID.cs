using OathFramework.Utility;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Runtime.InteropServices;
using Unity.Serialization.Json;

namespace OathFramework.Persistence
{
    
    [StructLayout(LayoutKind.Explicit, Size = 8)]
    public readonly unsafe struct UniqueID : IEquatable<UniqueID>
    {
        [FieldOffset(0)] public readonly byte Byte1;
        [FieldOffset(1)] public readonly byte Byte2;
        [FieldOffset(2)] public readonly byte Byte3;
        [FieldOffset(3)] public readonly byte Byte4;
        [FieldOffset(4)] public readonly byte Byte5;
        [FieldOffset(5)] public readonly byte Byte6;
        [FieldOffset(6)] public readonly byte Byte7;
        [FieldOffset(7)] public readonly byte Byte8;

        private static readonly byte[] RandChars;
        
        [SuppressMessage("ReSharper", "StringLiteralTypo")]
        static UniqueID()
        {
            RandChars = Encoding.UTF8.GetBytes("0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        }

        public static UniqueID Generate()
        {
            int count    = RandChars.Length - 1;
            FRandom rand = FRandom.Cache;
            return new UniqueID(
                RandChars[rand.Int(count)],
                RandChars[rand.Int(count)],
                RandChars[rand.Int(count)],
                RandChars[rand.Int(count)],
                RandChars[rand.Int(count)],
                RandChars[rand.Int(count)],
                RandChars[rand.Int(count)],
                RandChars[rand.Int(count)]
            );
        }

        public UniqueID(byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7, byte b8)
        {
            Byte1 = b1;
            Byte2 = b2;
            Byte3 = b3;
            Byte4 = b4;
            Byte5 = b5;
            Byte6 = b6;
            Byte7 = b7;
            Byte8 = b8;
        }
        
        public void Assign(string str)
        {
            Span<byte> b     = stackalloc byte[8] { Byte1, Byte2, Byte3, Byte4, Byte5, Byte6, Byte7, Byte8 };
            Span<char> chars = stackalloc char[8];
            Encoding.UTF8.GetChars(b, chars);
            fixed(char* p = str) {
                p[0] = chars[0];
                p[1] = chars[1];
                p[2] = chars[2];
                p[3] = chars[3];
                p[4] = chars[4];
                p[5] = chars[5];
                p[6] = chars[6];
                p[7] = chars[7];
            }
        }

        public override string ToString() 
            => Encoding.UTF8.GetString(stackalloc byte[8] { Byte1, Byte2, Byte3, Byte4, Byte5, Byte6, Byte7, Byte8 });

        public bool Equals(UniqueID other)      => *(long*)Byte1 == *(long*)other.Byte1;
        public override bool Equals(object obj) => obj is UniqueID other && Equals(other);

        public override int GetHashCode()
        {
            long l1 = *(long*)Byte1;
            unchecked {
                return (int)l1 ^ (int)(l1 >> 32);
            }
        }

        public class JsonAdapter : IJsonAdapter<UniqueID>
        {
            public void Serialize(in JsonSerializationContext<UniqueID> context, UniqueID value)
            {
                Span<byte> b = stackalloc byte[8] {
                    value.Byte1, value.Byte2, value.Byte3, value.Byte4, value.Byte5, value.Byte6, value.Byte7, value.Byte8
                };
                context.Writer.WriteValue(Encoding.UTF8.GetString(b));
            }

            public UniqueID Deserialize(in JsonDeserializationContext<UniqueID> context)
            {
                string str   = context.SerializedValue.ToString();
                Span<byte> b = stackalloc byte[8];
                Encoding.UTF8.GetBytes(str, b);
                return new UniqueID(b[0], b[1], b[2], b[3], b[4], b[5], b[6], b[7]);
            }
        }
    }
}
