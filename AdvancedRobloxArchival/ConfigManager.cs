﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedRobloxArchival
{
    internal class ConfigManager
    {
        public const string ConfigFilename = "config.json";
        private static bool ConfigInitialized = false;
        private static JObject _settings;
        public static JObject Settings {
            get {
                if (!ConfigInitialized)
                {
                    if (!ConfigExist()) CreateDefaultConfig();
                    else InitializeConfig();
                }
                return _settings;
            }
        }

        public static bool CheckKey(string key) => Settings.ContainsKey(key);

        private static void CreateDefaultConfig()
        {
            _settings = new JObject(new JProperty("ConfigVersion", Program.version.ToString()));
            ConfigInitialized = true;
        }

        public static bool ConfigExist() => File.Exists(ConfigFilename);

        private static void InitializeConfig()
        {
            _settings = JObject.Parse(File.ReadAllText(ConfigFilename));
            ConfigInitialized = true;
        }

        public static void FlushConfig() => File.WriteAllText(ConfigFilename, _settings.ToString());
    }
}
