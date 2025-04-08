using OathFramework.ProcGen;
using OathFramework.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace OathFramework.Core
{
    public static class INISettings
    {
        public static bool IsLoaded { get; private set; }

        private static Dictionary<string, string> values = new();
        private static string path                       = $"{Application.dataPath}{Path.DirectorySeparatorChar}oath_config.ini";

        public static void Load()
        {
            if(IsLoaded)
                return;

            // All exceptions need to be caught, otherwise this code path will lead to broken init.
            try {
                if(!File.Exists(path)) {
                    CreateDefault();
                }
            } catch(Exception e) {
                Debug.LogError($"Failed to check for existing INI settings, or set default: {e.Message}");
            }

            try {
                values = INIParser.ParseFile(path);
            } catch(Exception e) {
                Debug.LogError($"Failed to load INI settings: {e.Message}");
            }

            SetInitialSettings();
            IsLoaded = true;
        }

        private static void SetInitialSettings()
        {
            if(GetBool("Performance/DynamicShaderLoading") == false) {
                Shader.maximumChunksOverride = 0;
            }
            if(GetNumeric("Performance/AsyncBudgetBackground", out int val)) {
                AsyncFrameBudgets.Background = Mathf.Clamp(val, 1, 100);
            }
            if(GetNumeric("Performance/AsyncBudgetLow", out val)) {
                AsyncFrameBudgets.Low = Mathf.Clamp(val, 1, 1000);
            }
            if(GetNumeric("Performance/AsyncBudgetMedium", out val)) {
                AsyncFrameBudgets.Medium = Mathf.Clamp(val, 1, 1000);
            }
            if(GetNumeric("Performance/AsyncBudgetHigh", out val)) {
                AsyncFrameBudgets.High = Mathf.Clamp(val, 1, 1000);
            }
            if(GetBool("Performance/ParallelTerrainGen") == false) {
                ProcGenUtil.ParallelOperations = false;
            }
        }

        public static bool? GetBool(string key)
        {
            key = key.ToLower();
            if(!values.TryGetValue(key, out string value) || value.ToLower() == "default")
                return null;

            value = value.ToLower();
            if(value == "1" || value == "true")
                return true;
            if(value == "0" || value == "false")
                return false;
            
            return null;
        }

        public static bool GetString(string key, out string value)
        {
            value = "";
            key   = key.ToLower();
            if(!values.TryGetValue(key, out value))
                return false;
            
            value = value.ToLower();
            return true;
        }

        public static bool GetNumeric(string key, out int value)
        {
            value = -1;
            key   = key.ToLower();
            if(!values.TryGetValue(key, out string sVal))
                return false;
            
            return sVal.ToLower() != "default" && int.TryParse(sVal, out value);
        }
        
        private static void CreateDefault()
        {
            INIParser.WriteToFile(path, new List<INIEntry> {
                INIEntry.Comment(" *** OathFramework Config ***"),
                INIEntry.Comment(" Modifying these settings can cause instability or bugs. Don't change them unless you know what you're doing."),
                INIEntry.Empty(),
                INIEntry.Header("Performance"),
                INIEntry.ValueEx("DynamicShaderLoading", "default", $"  ;true/false. default = true."),
                INIEntry.ValueEx("AsyncBudgetBackground", "default", $" ;1-10. default = {AsyncFrameBudgets.Background}."),
                INIEntry.ValueEx("AsyncBudgetLow", "default", $"        ;1-1000. default = {AsyncFrameBudgets.Low}."),
                INIEntry.ValueEx("AsyncBudgetMedium", "default", $"     ;1-1000. default = {AsyncFrameBudgets.Medium}."),
                INIEntry.ValueEx("AsyncBudgetHigh", "default", $"       ;1-1000. default = {AsyncFrameBudgets.High}."),
                INIEntry.ValueEx("PrecompileShaders", "default", $"     ;true/false. default = true."),
                INIEntry.ValueEx("JobWorkerThreadCount", "default", $"  ;-1 = automatic | 1-1024."),
                INIEntry.ValueEx("ParallelTerrainGen", "default", $"    ;true/false, default = true."),
                INIEntry.ValueEx("AsyncMapTileGen", "default", $"       ;true/false, default = true."),
                INIEntry.ValueEx("GCMode", "default", $"                ;aggressive, moderate, optimal. default = optimal"),
                INIEntry.Empty(),
                INIEntry.Header("Physics"),
                INIEntry.ValueEx("PhysicsTickRate", "default", $"       ;30-120. default = 60."),
                INIEntry.Empty(),
                INIEntry.Header("Pooling"),
                INIEntry.ValueEx("PoolConfig", "default", $"            ;performance, optimal, memory, off. default = optimal."),
                INIEntry.Empty(),
                INIEntry.Header("FileIO"),
                INIEntry.ValueEx("BufferLength", "default", $"          ;32-65535. default = {FileIO.DefaultBufferLength}."),
                INIEntry.ValueEx("BufferCount", "default", $"           ;1-1024. default = {FileIO.DefaultBufferCount}."),
                INIEntry.ValueEx("UseThreadPool", "default", $"         ;true/false. default = true."),
                INIEntry.ValueEx("UseCompression", "default", $"        ;true/false. default = true."),
                INIEntry.ValueEx("PersistenceFormat", "default", $"     ;debug, performance. default = performance."),
                INIEntry.Empty(),
                INIEntry.Header("AI"),
                INIEntry.ValueEx("ProcessTimeSlicing", "default", $"    ;1-30. Higher is faster and less accurate."),
                INIEntry.ValueEx("NavmeshTimeSlicing", "default", $"    ;1-30. Higher is faster and less accurate."),
                INIEntry.ValueEx("MultithreadedAI", "default", $"       ;true/false, default = true.")
            });
        }
    }
}
