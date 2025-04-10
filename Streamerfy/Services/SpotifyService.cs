using SpotifyAPI.Web.Auth;
using SpotifyAPI.Web;
using Streamerfy.Data.Internal.Service;
using System.Windows.Media;

namespace Streamerfy.Services
{
    public class SpotifyService
    {
        private const string CallbackUrl = "http://localhost:5543/callback";

        private readonly EmbedIOAuthServer _server;
        private SpotifyClient _client;
        private readonly BlacklistService _blacklist;
        private readonly NowPlayingService _nowPlaying;

        private readonly string _clientId;
        private readonly string _clientSecret;

        private string _lastTrackId = "";
        private bool _lastIsPlaying = true;
        private Timer _pollTimer;

        public bool IsConnected => _client != null;

        public SpotifyService(BlacklistService blacklist, NowPlayingService nowPlaying)
        {
            _clientId = App.Settings.SpotifyClientId;
            _clientSecret = App.Settings.SpotifyClientSecret;
            _blacklist = blacklist;
            _nowPlaying = nowPlaying;

            _server = new EmbedIOAuthServer(new Uri(CallbackUrl), 5543);
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
                    MainWindow.Instance.AddLog($"⚠️ Polling error: {ex.Message}", Colors.OrangeRed);
                }
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5)); // poll every 5s
        }
        #endregion

        #region Event Methods
        private async Task OnErrorReceived(object sender, string error, string? state)
        {
            MainWindow.Instance.AddLog($"⚠️ Spotify Authorization Error: {error}.", Colors.OrangeRed);
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

            MainWindow.Instance.AddLog("✅ Spotify Authorized!", Colors.LimeGreen);
            StartPolling();
        }

        private void OnSongChanged(FullTrack track, bool isPlaying)
        {
            string trackName = track.Name;
            string artistName = track.Artists.FirstOrDefault()?.Name ?? "Unknown";
            string coverUrl = track.Album.Images.FirstOrDefault()?.Url ?? "";

            MainWindow.Instance.AddLog($"🎵 Now Playing {trackName} - {artistName}", Colors.CornflowerBlue);
            _nowPlaying.Update(trackName, artistName, coverUrl, isPlaying);
        }

        private void OnPlaybackToggled(FullTrack track, bool isPlaying)
        {
            string trackName = track.Name;
            string artistName = track.Artists.FirstOrDefault()?.Name ?? "Unknown";
            string coverUrl = track.Album.Images.FirstOrDefault()?.Url ?? "";

            string statusText = isPlaying ? "▶️ Resumed" : "⏸️ Paused";
            var color = isPlaying ? Colors.LightGreen : Colors.Orange;

            MainWindow.Instance.AddLog($"{statusText} {trackName} - {artistName}", color);
            _nowPlaying.Update(trackName, artistName, coverUrl, isPlaying);
        }
        #endregion

        #region Misc Methods
        public async Task<bool> AddToQueue(string url)
        {
            var track = await GetTrackInfo(url);
            if (track == null)
                return false;

            if (_blacklist.IsTrackBlacklisted(track.ID))
                return false;

            if (_blacklist.IsArtistBlacklisted(track.Artist.ID))
                return false;

            var uri = $"spotify:track:{track.ID}";
            await _client.Player.AddToQueue(new PlayerAddToQueueRequest(uri));

            return true;
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
