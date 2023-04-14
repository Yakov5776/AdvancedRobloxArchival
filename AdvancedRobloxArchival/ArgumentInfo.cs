using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace AdvancedRobloxArchival
{
    internal class ArgumentInfo
    {
        public Action<string> Action { get; set; }
        public bool RequiresValue { get; set; }
        public string Description { get; set; }

        public ArgumentInfo(Action<string> action, bool requiresValue, string description)
        {
            Action = action;
            RequiresValue = requiresValue;
            Description = description;
        }

        internal static Dictionary<string,ArgumentInfo> argumentActions = new Dictionary<string, ArgumentInfo>
            {
                { "mode",  new ArgumentInfo(value => SetMode(value), true, "Sets the mode for scanning (e.g. ScanAllDrives, ScanSpecificDirectories)") },
                { "identify-file", new ArgumentInfo(value => IdentifyFile(value), true, "Automatically analyzes and returns a JSON-friendly response for a binary") },
                { "help", new ArgumentInfo(_ => ShowHelp(), false, "Returns this help menu") },
                { "?", new ArgumentInfo(_ => ShowHelp(), false, null) },
            };

        internal static void ParseArguments(string[] args)
        {
            string currentArgument = null;
            foreach (var arg in args)
            {
                bool isValue = !arg.StartsWith("-");
                if (isValue)
                {
                    if (currentArgument == null || !argumentActions.ContainsKey(currentArgument))
                    {
                        Console.WriteLine($"Error: Unrecognized argument '{arg}'.");
                        Environment.Exit(1);
                    }
                    else
                    {
                        if (argumentActions[currentArgument].RequiresValue)
                        {
                            argumentActions[currentArgument].Action(arg.Trim('"'));
                            currentArgument = null;
                        }
                        else
                        {
                            Console.WriteLine($"Error: Argument '-{currentArgument}' does not require a value.");
                            Environment.Exit(1);
                        }
                    }
                }
                else
                {
                    if (currentArgument != null)
                    {
                        if (argumentActions[currentArgument].RequiresValue)
                        {
                            Console.WriteLine($"Error: Missing value for argument '-{currentArgument}'.");
                            Environment.Exit(1);
                        }
                        else
                        {
                            argumentActions[currentArgument].Action(null);
                        }
                    }
                    currentArgument = arg.Substring(1).ToLower();
                    if (!argumentActions.ContainsKey(currentArgument))
                    {
                        Console.WriteLine($"Error: Unrecognized argument '{arg}'.");
                        Environment.Exit(1);
                    }
                    if (!argumentActions[currentArgument].RequiresValue)
                    {
                        argumentActions[currentArgument].Action(null);
                        currentArgument = null;
                    }
                }
            }

            if (currentArgument != null)
            {
                if (argumentActions[currentArgument].RequiresValue)
                {
                    Console.WriteLine($"Error: Missing value for argument '-{currentArgument}'.");
                    Environment.Exit(1);
                }
                else
                {
                    argumentActions[currentArgument].Action(null);
                }
            }
        }

        static void SetMode(string value)
        {
            Program.Modes mode;
            if (Enum.TryParse(value, out mode))
            {
                Program.CurrentMode = mode;
            }
            else
            {
                Console.WriteLine($"Error: Unrecognized mode value '{value}'.");
                Environment.Exit(1);
            }
        }

        static void IdentifyFile(string path)
        {
            BinaryArchive binary = BinaryArchive.CheckFileAuthenticity(path);
            JObject FileResponse = null;
            if (binary.Genuine)
                FileResponse = new JObject(
                    new JProperty("version", binary.Version),
                    new JProperty("type", binary.BinaryType.ToString()),
                    new JProperty("pe_date", new PeHeaderReader(binary.Path).FileHeader.TimeDateStamp),
                    new JProperty("digital_signature", "verified"));
            else
                FileResponse = new JObject(new JProperty("digital_signature", "not_verified"));

            Console.Write(FileResponse.ToString(Formatting.None));
            Environment.Exit(0);
        }

        static void ShowHelp()
        {
            Console.WriteLine("Here's a list of available argument:");
            foreach (var arg in argumentActions)
            {
                if (arg.Value.Description != null)
                    Console.WriteLine($"\"{arg.Key}\": {arg.Value.Description} | Requires Value: {arg.Value.RequiresValue}");
            }
            Environment.Exit(0);
        }
    }
}
