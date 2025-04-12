using Streamerfy.Data.Json;

namespace Streamerfy.Data.Internal.Json
{
    public class AppSettings
    {
        public bool AutoConnect { get; set; } = false;
        public bool BlockExplicit { get; set; } = false;
        public string BotUsername { get; set; } = string.Empty;
        public string BotOAuth { get; set; } = string.Empty;
        public string Channel { get; set; } = string.Empty;
        public string SpotifyClientId { get; set; } = string.Empty;
        public string SpotifyClientSecret { get; set; } = string.Empty;
        public string Language { get; set; } = "en";
        public string CmdPrefix { get; set; } = "!";
        public AppCommand CmdQueue { get; set; } = new AppCommand("queue", false, false, false);
        public AppCommand CmdBlacklist { get; set; } = new AppCommand("blacklist", false, false, true);
        public AppCommand CmdUnblacklist { get; set; } = new AppCommand("unblacklist", false, false, true);
        public AppCommand CmdBan { get; set; } = new AppCommand("ban", false, false, true);
        public AppCommand CmdUnban { get; set; } = new AppCommand("unban", false, false, true);
    }
}
