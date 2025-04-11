using Streamerfy.Windows;
using System.Windows.Media;

namespace Streamerfy.Services
{
    public class ServiceManager
    {
        public static BlacklistService Blacklist { get; private set; }
        public static SpotifyService Spotify { get; private set; }
        public static PlaybackHistoryService Playback { get; private set; }
        public static TwitchService Twitch { get; private set; }
        public static NowPlayingService NowPlaying { get; private set; }

        public static void InitializeServices()
        {
            Blacklist = new BlacklistService();
            NowPlaying = new NowPlayingService();
            Playback = new PlaybackHistoryService();
            Spotify = new SpotifyService(Blacklist, NowPlaying, Playback);
            Twitch = new TwitchService(
                Spotify,
                Blacklist
            );
        }
    }
}
