namespace Streamerfy.Services
{
    public class ServiceManager
    {
        public static BlacklistService Blacklist { get; private set; }
        public static SpotifyService Spotify { get; private set; }
        public static TwitchService Twitch { get; private set; }

        public static void InitializeServices()
        {
            Blacklist = new BlacklistService();
            Spotify = new SpotifyService(Blacklist);
            Twitch = new TwitchService(
                Spotify,
                Blacklist
            );
        }
    }
}
