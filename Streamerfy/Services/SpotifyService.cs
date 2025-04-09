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

        private readonly string _clientId;
        private readonly string _clientSecret;

        public bool IsConnected => _client != null;

        public SpotifyService(BlacklistService blacklist)
        {
            _clientId = App.Settings.SpotifyClientId;
            _clientSecret = App.Settings.SpotifyClientSecret;
            _blacklist = blacklist;

            _server = new EmbedIOAuthServer(new Uri(CallbackUrl), 5543);
            StartAuthFlow();
        }

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
        }

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
    }
}
