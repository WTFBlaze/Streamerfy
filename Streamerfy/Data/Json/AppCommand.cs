using Newtonsoft.Json;

namespace Streamerfy.Data.Json
{
    public class AppCommand
    {
        public AppCommand() { }
        public AppCommand(string command, bool allowVip, bool allowSub, bool allowMod)
        {
            Command = command;
            AllowVIP = allowVip;
            AllowSub = allowSub;
            AllowMod = allowMod;
        }

        public string Command { get; set; } = "UnsetCmd";
        public bool AllowVIP { get; set; } = false;
        public bool AllowSub { get; set; } = false;
        public bool AllowMod { get; set; } = false;

        [JsonIgnore]
        public bool AllowEveryone => !AllowVIP && !AllowSub && !AllowMod;
    }
}
