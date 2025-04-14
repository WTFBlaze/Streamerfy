using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using Streamerfy.Data.Internal.Service;
using System.Windows.Media;
using Streamerfy.Windows;
using SpotifyAPI.Web.Http;
using System.Reflection;
using Streamerfy.Data.EmbedIO;
using Swan;
using Swan.Logging;

namespace Streamerfy.Services
{
    public class SpotifyService
    {
        private const string CallbackUrl = "http://127.0.0.1:5543/callback";

        private readonly AuthServer _server;
        private SpotifyClient _client;
        private readonly BlacklistService _blacklist;
        private readonly NowPlayingService _nowPlaying;
        private readonly PlaybackHistoryService _playbackHistory;

        private readonly string _clientId;
        private readonly string _clientSecret;

        private string _lastTrackId = "";
        private bool _lastIsPlaying = true;
        private Timer _pollTimer;

        private readonly Dictionary<string, string> _requestedTracks = new();

        public bool IsConnected => _client != null;

        public SpotifyService(BlacklistService blacklist, NowPlayingService nowPlaying, PlaybackHistoryService playbackHistoryService)
        {
            _clientId = App.Settings.SpotifyClientId;
            _clientSecret = App.Settings.SpotifyClientSecret;
            _blacklist = blacklist;
            _nowPlaying = nowPlaying;
            _playbackHistory = playbackHistoryService;

            Logger.UnregisterLogger<ConsoleLogger>(); // Disables the annoying console logging spam. (Will redirect to a proper logger later)
            _server = new AuthServer(new Uri(CallbackUrl), 5543, Assembly.GetExecutingAssembly(), "Streamerfy.Assets.SpotifyAuth.default_site");
            StartAuthFlow();
        }

        #region Initialization Methods
        private void StartAuthFlow()
        {
            Task.Run(async () =>
            {
                await _server.Start();

                _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
                _server.ErrorReceived += OnErrorReceived;

                var loginRequest = new LoginRequest(_server.BaseUri, _clientId, LoginRequest.ResponseType.Code)
                {
                    Scope = new[] {
                        Scopes.UserModifyPlaybackState,
                        Scopes.UserReadPlaybackState,
                        Scopes.UserReadCurrentlyPlaying
                    }
                };

                BrowserUtil.Open(loginRequest.ToUri());
            });
        }

        private void StartPolling()
        {
            _pollTimer = new Timer(async _ =>
            {
                try
                {
                    var playing = await _client.Player.GetCurrentlyPlaying(new());
                    var playback = await _client.Player.GetCurrentPlayback();

                    var track = playing?.Item as FullTrack;
                    bool isPlaying = playback?.IsPlaying ?? false;

                    if (track == null)
                    {
                        _nowPlaying.Clear();
                        _lastTrackId = "";
                        _lastIsPlaying = false;
                        return;
                    }

                    // If track changed
                    if (track.Id != _lastTrackId)
                    {
                        _lastTrackId = track.Id;
                        _lastIsPlaying = isPlaying;
                        OnSongChanged(track, isPlaying);
                    }
                    // If playback state changed
                    else if (isPlaying != _lastIsPlaying)
                    {
                        _lastIsPlaying = isPlaying;
                        OnPlaybackToggled(track, isPlaying);
                    }
                }
                catch (Exception ex)
                {
                    MainWindow.Instance.AddLog(LanguageService.Translate("Message_Polling_Error", new { ERROR = ex.Message }), Colors.OrangeRed);
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5)); // poll every 5s
        }
        #endregion

        #region Event Methods
        private async Task OnErrorReceived(object sender, string error, string? state)
        {
            MainWindow.Instance.AddLog(LanguageService.Translate("Message_Spotify_Authorization_Failure", new { ERROR = error }), Colors.OrangeRed);
            await _server.Stop();
        }

        private async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            await _server.Stop();

            var config = SpotifyClientConfig.CreateDefault();
            var token = await new OAuthClient(config).RequestToken(
                new AuthorizationCodeTokenRequest(_clientId, _clientSecret, response.Code, new Uri(CallbackUrl))
            );

            _client = new SpotifyClient(token);

            MainWindow.Instance.AddLog(LanguageService.Translate("Message_Spotify_Authorization_Success"), Colors.LimeGreen);
            StartPolling();
        }

        private void OnSongChanged(FullTrack track, bool isPlaying)
        {
            string trackName = track.Name;
            string artistName = track.Artists.FirstOrDefault()?.Name ?? "Unknown";
            string coverUrl = track.Album.Images.FirstOrDefault()?.Url ?? "";
            string trackId = track.Id;

            // Check if the track was requested by someone
            string requestedBy = _requestedTracks.TryGetValue(trackId, out var name)
                ? name
                : "Spotify (Autoplay/Shuffle)";

            MainWindow.Instance.AddLog(LanguageService.Translate("Message_NowPlaying", new { SONG = trackName, ARTIST = artistName, REQUESTER = requestedBy }), Colors.CornflowerBlue);
            _nowPlaying.Update(trackName, artistName, coverUrl, isPlaying);
            _playbackHistory.Add(new SpotifyTrack
            {
                AlbumArtUrl = coverUrl,
                ID = trackId,
                Explicit = track.Explicit,
                Name = trackName,
                Artist = new()
                {
                    ID = track.Artists.FirstOrDefault()?.Id ?? "",
                    Name = artistName
                }
            }, requestedBy);

            // Remove from memory once used
            _requestedTracks.Remove(trackId);

            MainWindow.Instance.RefreshHistoryList();
        }


        private void OnPlaybackToggled(FullTrack track, bool isPlaying)
        {
            string trackName = track.Name;
            string artistName = track.Artists.FirstOrDefault()?.Name ?? "Unknown";
            string coverUrl = track.Album.Images.FirstOrDefault()?.Url ?? "";

            string statusText = LanguageService.Translate(isPlaying ? "Message_Song_Resumed" : "Message_Song_Paused");
            var color = isPlaying ? Colors.LightGreen : Colors.Orange;

            MainWindow.Instance.AddLog(LanguageService.Translate("Message_Playback_Notice", new { STATE = statusText, SONG = trackName, ARTIST = artistName }), color);
            _nowPlaying.Update(trackName, artistName, coverUrl, isPlaying);
        }
        #endregion

        #region Misc Methods
        public async Task<bool> AddToQueue(string url, string requestedBy = "Unknown")
        {
            var track = await GetTrackInfo(url);
            if (track == null) return false;

            if (_blacklist.IsTrackBlacklisted(track.ID)) return false;
            if (_blacklist.IsArtistBlacklisted(track.Artist.ID)) return false;

            var uri = $"spotify:track:{track.ID}";
            await _client.Player.AddToQueue(new PlayerAddToQueueRequest(uri));

            // Store who requested it
            _requestedTracks[track.ID] = requestedBy;

            return true;
        }

        public async Task<bool> AddToQueue(SpotifyTrack track, string requestedBy = "Unknown")
        {
            if (_blacklist.IsTrackBlacklisted(track.ID)) return false;
            if (_blacklist.IsArtistBlacklisted(track.Artist.ID ?? "")) return false;

            var uri = $"spotify:track:{track.ID}";
            await _client.Player.AddToQueue(new PlayerAddToQueueRequest(uri));
            _requestedTracks[track.ID] = requestedBy;
            return true;
        }

        public async Task<SpotifyTrack?> SearchTrack(string query)
        {
            var searchRequest = new SearchRequest(SearchRequest.Types.Track, query);
            var searchResponse = await _client.Search.Item(searchRequest);
            var track = searchResponse.Tracks?.Items?.FirstOrDefault();

            if (track == null)
                return null;

            return new SpotifyTrack
            {
                AlbumArtUrl = track.Album.Images.FirstOrDefault()?.Url ?? "",
                ID = track.Id,
                Explicit = track.Explicit,
                Name = track.Name,
                Artist = new SpotifyArtist
                {
                    ID = track.Artists.FirstOrDefault()?.Id ?? "",
                    Name = track.Artists.FirstOrDefault()?.Name ?? "Unknown"
                }
            };

        }

        // Track URL: https://open.spotify.com/track/5TXDeTFVRVY7Cvt0Dw4vWW
        public async Task<SpotifyTrack?> GetTrackInfo(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Host != "open.spotify.com")
                return null;

            var segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments.Length != 2 || segments[0] != "track")
                return null;

            var id = segments[1];
            var track = await _client.Tracks.Get(id);

            return new SpotifyTrack
            {
                ID = track.Id,
                Name = track.Name,
                Explicit = track.Explicit,
                Artist = new SpotifyArtist
                {
                    ID = track.Artists.FirstOrDefault()?.Id ?? "",
                    Name = track.Artists.FirstOrDefault()?.Name ?? "Unknown"
                }
            };
        }

        // Artist URL: https://open.spotify.com/artist/15UsOTVnJzReFVN1VCnxy4
        public async Task<SpotifyArtist?> GetArtistInfo(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Host != "open.spotify.com")
                return null;

            var segments = uri.AbsolutePath.Trim('/').Split('/');
            if (segments.Length != 2 || segments[0] != "artist")
                return null;

            var id = segments[1];
            var artist = await _client.Artists.Get(id);

            return new SpotifyArtist
            {
                ID = artist.Id,
                Name = artist.Name
            };
        }
        #endregion
    }
}
