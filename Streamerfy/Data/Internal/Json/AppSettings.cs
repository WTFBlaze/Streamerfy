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
    }
}
