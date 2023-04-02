using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedRobloxArchival
{
    internal class PropertyMatching
    {
        public static Archive.BinaryTypes GetBinaryTypeFromSignature(string prop)
        {
            if (prop.EndsWith("Client") || prop.EndsWith("Game")) return Archive.BinaryTypes.RobloxClient;
            if (prop.EndsWith("Studio")) return Archive.BinaryTypes.RobloxStudio;
            if (prop.ToLower() == "roblox compute cloud service") return Archive.BinaryTypes.RCCService;

            return Archive.BinaryTypes.Miscellaneous;
        }

        public static bool IsROBLOX(string property) => property?.ToUpper().StartsWith("ROBLOX") ?? false;
    }
}
