namespace Streamerfy.Services
{
    public class ServiceManager
    {
        public static BlacklistService Blacklist { get; private set; }
        public static SpotifyService Spotify { get; private set; }
        public static TwitchService Twitch { get; private set; }
        public static NowPlayingService NowPlaying { get; private set; }

        public static void InitializeServices()
        {
            Blacklist = new BlacklistService();
            NowPlaying = new NowPlayingService();
            Spotify = new SpotifyService(Blacklist, NowPlaying);
            Twitch = new TwitchService(
                Spotify,
                Blacklist
            );
        }
    }
}
