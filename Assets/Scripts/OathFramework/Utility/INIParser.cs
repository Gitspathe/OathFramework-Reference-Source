using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace OathFramework.Utility
{
    public static class INIParser
    {
        public static Dictionary<string, string> ParseFile(string filePath)
        {
            return Parse(File.ReadAllText(filePath, Encoding.UTF8));
        }
        
        public static Dictionary<string, string> Parse(string data)
        {
            data                              = data.Trim('\uFEFF');
            Dictionary<string, string> result = new();
            string                     header = "";
            string[]                   lines  = data.Split(new []{ "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach(string line in lines) {
                string str = StripWhitespace(line);
                if(str.StartsWith(";")) {
                    // This is a comment.
                    continue;
                }
                if(str.Contains("[")) {
                    // This is a header.
                    if(!ReadUntil(str.TrimStart('['), ']', out string newHeader)) {
                        Debug.LogWarning("INI read error -> Failed to read header");
                        continue;
                    }
                    
                    header = newHeader.ToLower();
                    continue;
                }
                if(str.Contains("=")) {
                    // This is a key-value pair.
                    int commentIndex = str.IndexOf(';');
                    if(commentIndex != -1) {
                        str = str.Substring(0, commentIndex).Trim();
                    }
                    
                    string[] pair = str.Split("=");
                    if(pair.Length != 2) {
                        Debug.LogWarning("INI read error -> Malformed line");
                        continue;
                    }
                    
                    string key     = StripWhitespace(pair[0]).ToLower();
                    string value   = StripWhitespace(pair[1]).ToLower();
                    string fullKey = !string.IsNullOrWhiteSpace(header) ? $"{header}/{key}" : key;
                    if(!result.TryAdd(fullKey, value)) {
                        Debug.LogWarning($"INI read error -> Skipping duplicate key: {fullKey}");
                    }
                }
            }
            return result;
        }

        public static void WriteToFile(string filePath, List<INIEntry> entries)
        {
            File.WriteAllText(filePath, Format(entries), Encoding.UTF8);
        }

        public static string Format(List<INIEntry> entries)
        {
            StringBuilder sb = new();
            foreach(INIEntry entry in entries) {
                entry.Write(sb);
                if(!(entry is INIEmpty)) {
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        private static string StripWhitespace(string str)
        {
            if(string.IsNullOrWhiteSpace(str))
                return "";
            
            char[] buffer       = new char[str.Length];
            int index           = 0;
            bool insideKeyValue = false;
            foreach(char c in str) {
                if(c == '=') {
                    insideKeyValue  = true;
                    buffer[index++] = c;
                } else if(!char.IsWhiteSpace(c) || insideKeyValue) {
                    buffer[index++] = c;
                }
            }
            return new string(buffer, 0, index);
        }

        private static bool ReadUntil(string str, char delimiter, out string result)
        {
            result = null;
            if(string.IsNullOrWhiteSpace(str))
                return false;
            
            StringBuilder sb = new();
            foreach(char c in str) {
                if(c == '\r' || c == '\n')
                    return false;
                if(c == delimiter)
                    break;

                sb.Append(c);
            }
            result = sb.ToString();
            return true;
        }
    }

    public abstract class INIEntry
    {
        public static INIEmpty Empty()                                         => new();
        public static INIComment Comment(string val)                           => new(val);
        public static INIHeader Header(string val)                             => new(val);
        public static INIValue Value(string key, string val)                   => new(key, val);
        public static INIValue Value(string key, string val, string comment)   => new(key, val, comment);
        public static INIValue ValueEx(string key, string val, string comment) => new(key, val, comment, true);

        public abstract void Write(StringBuilder sb);
    }

    public class INIComment : INIEntry
    {
        private readonly string val;

        public INIComment(string val)
        {
            this.val = val;    
        }

        public override void Write(StringBuilder sb)
        {
            sb.Append(";").Append(val);
        }
    }

    public class INIHeader : INIEntry
    {
        private readonly string val;

        public INIHeader(string val)
        {
            this.val = val;
        }
        
        public override void Write(StringBuilder sb)
        {
            sb.Append("[").Append(val).Append("]");
        }
    }

    public class INIValue : INIEntry
    {
        private readonly string key;
        private readonly string val;
        private readonly string comment;
        private readonly bool skipChar;

        public INIValue(string key, string val)
        {
            this.key = key;
            this.val = val;
        }
        
        public INIValue(string key, string val, string comment, bool skipChar = false)
        {
            this.key      = key;
            this.val      = val;
            this.comment  = comment;
            this.skipChar = skipChar;
        }

        public override void Write(StringBuilder sb)
        {
            sb.Append(key).Append(" = ").Append(val);
            if(string.IsNullOrWhiteSpace(comment))
                return;

            if(skipChar) {
                sb.Append(comment);
            } else {
                sb.Append(" ;").Append(comment);
            }
        }
    }

    public class INIEmpty : INIEntry
    {
        public override void Write(StringBuilder sb)
        {
            sb.AppendLine();
        }
    }
}
