using System;
using System.Collections.Generic;

namespace AdvancedRobloxArchival
{
    internal class PropertyMatching
    {
        private static Dictionary<string, BinaryArchive.BinaryTypes> signatureMap => new Dictionary<string, BinaryArchive.BinaryTypes>()
        {
            { "Client", BinaryArchive.BinaryTypes.RobloxClient },
            { "Game", BinaryArchive.BinaryTypes.RobloxClient },
            { "Studio", BinaryArchive.BinaryTypes.RobloxStudio },
            { "Compute Cloud Service", BinaryArchive.BinaryTypes.RCCService }
        };


        public static BinaryArchive.BinaryTypes GetBinaryTypeFromSignature(string prop)
        {
            string property = prop.Trim();

            foreach (var signature in signatureMap)
            {
                if (property.EndsWith(signature.Key, StringComparison.OrdinalIgnoreCase))
                {
                    return signature.Value;
                }
            }

            return BinaryArchive.BinaryTypes.Miscellaneous;
        }

        public static bool IsROBLOX(string property) => property?.ToUpper().StartsWith("ROBLOX") ?? false;

        public static bool ConsiderBinaryCandidate(string filename)
        {
            return IsBinary(filename) && (filename.StartsWith("Roblox") || filename.StartsWith("version-") || filename.StartsWith("RCCService") || filename.StartsWith("0."));
        }

        public static bool IsBinary(string filename) => filename.EndsWith(".exe");
    }
}
