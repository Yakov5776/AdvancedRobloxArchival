using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedRobloxArchival
{
    internal class PropertyMatching
    {
        public static BinaryArchive.BinaryTypes GetBinaryTypeFromSignature(string prop)
        {
            string property = prop.Trim();

            if (property.EndsWith("Client") || property.EndsWith("Game")) return BinaryArchive.BinaryTypes.RobloxClient;
            if (property.EndsWith("Studio")) return BinaryArchive.BinaryTypes.RobloxStudio;
            if (property.ToLower() == "roblox compute cloud service") return BinaryArchive.BinaryTypes.RCCService;

            return BinaryArchive.BinaryTypes.Miscellaneous;
        }

        public static bool IsROBLOX(string property) => property?.ToUpper().StartsWith("ROBLOX") ?? false;

        public static bool ConsiderBinaryCandidate(string filename)
        {
            return filename.EndsWith(".exe") && (filename.StartsWith("Roblox") || filename.StartsWith("version-") || filename.StartsWith("RCCService"));
        }
    }
}
