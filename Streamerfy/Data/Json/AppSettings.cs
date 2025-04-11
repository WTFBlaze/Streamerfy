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
        public string CmdQueue { get; set; } = "queue";
        public string CmdBlacklist { get; set; } = "blacklist";
        public string CmdUnblacklist { get; set; } = "unblacklist";
        public string CmdBan { get; set; } = "ban";
        public string CmdUnban { get; set; } = "unban";
    }
}
