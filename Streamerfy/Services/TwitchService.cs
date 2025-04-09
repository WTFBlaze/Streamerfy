using System.Collections.Concurrent;
using System.Windows.Media;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Interfaces;
using TwitchLib.Communication.Models;

namespace Streamerfy.Services
{
    public class TwitchService
    {
        private TwitchClient? _client;
        private IClient? _customClient;

        private readonly SpotifyService _spotify;
        private readonly BlacklistService _blacklist;
        private readonly ConnectionCredentials _credentials;
        private readonly ClientOptions _clientOptions;

        private string _channel = string.Empty;
        public bool IsConnected => _client != null;

        public TwitchService(SpotifyService spotifyService, BlacklistService blacklistService)
        {
            _spotify = spotifyService;
            _blacklist = blacklistService;

            _credentials = new ConnectionCredentials(App.Settings.BotUsername, App.Settings.BotOAuth);
            _clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };
        }

        public void SetChannel(string channel) => _channel = channel;

        public void Connect()
        {
            if (string.IsNullOrWhiteSpace(_channel))
                throw new InvalidOperationException("Twitch channel must be set before connecting.");

            _customClient = new WebSocketClient(_clientOptions);
            _client = new TwitchClient(_customClient);
            _client.Initialize(_credentials, _channel);
            _client.OnConnected += OnConnected;
            _client.OnMessageReceived += OnMessageReceived;
            _client.Connect();
        }

        public void Disconnect()
        {
            if (_client != null)
            {
                _client.OnConnected -= OnConnected;
                _client.OnMessageReceived -= OnMessageReceived;
                _client.Disconnect();
                _client = null;
            }
            _customClient = null;
            MainWindow.Instance.SetConnectionStatus(string.Empty);
            MainWindow.Instance.AddLog("🔌 Disconnected from Twitch.", Colors.OrangeRed);
            MainWindow.Instance.SetConnectButtonContent(false);
        }

        #region Internal Methods
        private void SendMessage(string msg) => _client.SendMessage(_channel, msg);
        private void Reject(string reason) => SendMessage($"❌ {reason}");
        #endregion

        #region Event Methods
        private void OnConnected(object? sender, OnConnectedArgs e)
        {
            MainWindow.Instance.SetConnectionStatus(_channel);
            MainWindow.Instance.AddLog("🔌 Connected to Twitch.", Colors.LimeGreen);
            MainWindow.Instance.SetConnectButtonContent(true);
        }

        private async void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var msg = e.ChatMessage.Message;
            var user = e.ChatMessage;
            var senderName = user.Username;

            if (_blacklist.IsUserBlacklisted(user.Username))
            {
                Reject("You are blacklisted from using Streamerfy Commands!");
                return;
            }

            var split = msg.Split(' ');
            var command = split[0].ToLower();
            var arg1 = split.Length > 1 ? split[1] : null;
            var arg2 = split.Length > 2 ? split[2] : null;

            switch (command)
            {
                case var cmd when cmd is "!player" or "!request" or "!queue":
                    {
                        if (arg1 == null)
                        {
                            Reject("Please provide a Spotify link.");
                            return;
                        }

                        var url = arg1;
                        var track = await _spotify.GetTrackInfo(url);
                        if (track == null)
                        {
                            Reject("Could not find that track.");
                            MainWindow.Instance.AddLog($"⚠️ Couldn't find {senderName}'s song!", Colors.OrangeRed);
                            return;
                        }

                        if (_blacklist.IsTrackBlacklisted(track.ID))
                        {
                            Reject("That song is blacklisted!");
                            MainWindow.Instance.AddLog($"⚠️ {senderName} tried to request a blacklist song!", Colors.OrangeRed);
                            return;
                        }

                        if (_blacklist.IsArtistBlacklisted(track.Artist.ID))
                        {
                            Reject("That artist is blacklisted!");
                            MainWindow.Instance.AddLog($"⚠️ {senderName} tried to request a blacklist artist!", Colors.OrangeRed);
                            return;
                        }

                        if (App.Settings.BlockExplicit && track.Explicit)
                        {
                            Reject("The streamer isn't allowing explicit songs!");
                            MainWindow.Instance.AddLog($"⚠️ {senderName} tried to request an explicit song!", Colors.OrangeRed);
                            return;
                        }

                        bool success = await _spotify.AddToQueue(url);

                        if (!success)
                        {
                            SendMessage("⚠️ Could not add the song to the queue (Something went wrong).");
                            MainWindow.Instance.AddLog($"⚠️ Failed to add {senderName}'s song to queue!", Colors.OrangeRed);
                        }
                        else
                        {
                            SendMessage($"✅ Queued: {track.Name} by {track.Artist.Name}");
                            MainWindow.Instance.AddLog($"🎵 Added {track.Name} - {track.Artist.Name} (Requested by {senderName})", Colors.SkyBlue);
                        }
                    }
                    break;

                case "!blacklist":
                    {
                        if (!user.IsModerator && !user.IsBroadcaster) return;
                        if (arg1 == null || arg2 == null)
                        {
                            Reject("Usage: !blacklist [track|artist] [url]");
                            return;
                        }

                        switch (arg1.ToLower())
                        {
                            case "song":
                            case "track":
                                {
                                    var track = await _spotify.GetTrackInfo(arg2);
                                    if (track == null)
                                    {
                                        Reject("Could not find track by that URL!");
                                        return;
                                    }

                                    if (_blacklist.IsTrackBlacklisted(track.ID))
                                        Reject("That song is already blacklisted!");
                                    else
                                    {
                                        _blacklist.AddTrack(track.ID);
                                        SendMessage($"✅ Added {track.Name} - {track.Artist.Name} to the blacklist.");
                                        MainWindow.Instance.AddLog($"✅ Added {track.Name} - {track.Artist.Name} to the blacklist (Requested by {senderName})", Colors.Yellow);
                                    }
                                }
                                break;

                            case "band":
                            case "artist":
                                {
                                    var artist = await _spotify.GetArtistInfo(arg2);
                                    if (artist == null)
                                    {
                                        Reject("Could not find artist by that URL!");
                                        return;
                                    }

                                    if (_blacklist.IsArtistBlacklisted(artist.ID))
                                        Reject("That artist is already blacklisted!");
                                    else
                                    {
                                        _blacklist.AddArtist(artist.ID);
                                        SendMessage($"✅ Added {artist.Name} to the blacklist.");
                                        MainWindow.Instance.AddLog($"✅ Added {artist.Name} to the blacklist (Requested by {senderName})", Colors.Yellow);
                                    }

                                }
                                break;
                        }
                    }
                    break;

                case "!unblacklist":
                    {
                        if (!user.IsModerator && !user.IsBroadcaster) return;
                        if (arg1 == null || arg2 == null)
                        {
                            Reject("Usage: !unblacklist [track|artist] [url]");
                            return;
                        }

                        switch (arg1.ToLower())
                        {
                            case "song":
                            case "track":
                                {
                                    var track = await _spotify.GetTrackInfo(arg2);
                                    if (track == null)
                                    {
                                        Reject("Could not find track by that URL!");
                                        return;
                                    }

                                    if (!_blacklist.IsTrackBlacklisted(track.ID))
                                        Reject("That song is not blacklisted!");
                                    else
                                    {
                                        _blacklist.RemoveTrack(track.ID);
                                        SendMessage($"✅ Removed {track.Name} - {track.Artist.Name} from the blacklist.");
                                        MainWindow.Instance.AddLog($"✅ Removed {track.Name} - {track.Artist.Name} from the blacklist (Requested by {senderName})", Colors.YellowGreen);
                                    }
                                }
                                break;

                            case "band":
                            case "artist":
                                {
                                    var artist = await _spotify.GetArtistInfo(arg2);
                                    if (artist == null)
                                    {
                                        Reject("Could not find artist by that URL!");
                                        return;
                                    }

                                    if (!_blacklist.IsArtistBlacklisted(artist.ID))
                                        Reject("That artist is not blacklisted!");
                                    else
                                    {
                                        _blacklist.RemoveArtist(artist.ID);
                                        SendMessage($"✅ Removed {artist.Name} from the blacklist.");
                                        MainWindow.Instance.AddLog($"✅ Removed {artist.Name} from the blacklist (Requested by {senderName})", Colors.YellowGreen);
                                    }

                                }
                                break;
                        }

                    }
                    break;

                case "!rban":
                    {
                        if (!user.IsModerator && !user.IsBroadcaster) return;
                        if (arg1 == null)
                        {
                            Reject("Usage: !rban [username]");
                            return;
                        }

                        if (arg1.ToLower().Equals(_client.JoinedChannels[0].Channel.ToLower()))
                        {
                            Reject("You cannot blacklist the streamer!");
                            return;
                        }

                        if (_blacklist.IsUserBlacklisted(arg1.ToLower()))
                        {
                            Reject($"{arg1} is already blacklisted!");
                            return;
                        }

                        _blacklist.AddUser(arg1.ToLower());
                        MainWindow.Instance.AddLog($"✅ Blacklisted {arg1} from using Streamerfy commands (Requested by {senderName})", Colors.Orange);
                        SendMessage($"✅ Blacklisted {arg1} from using Streamerfy commands.");
                    }
                    break;

                case "!runban":
                    {
                        if (!user.IsModerator && !user.IsBroadcaster) return;
                        if (arg1 == null)
                        {
                            Reject("Usage: !runban [username]");
                            return;
                        }

                        if (!_blacklist.IsUserBlacklisted(arg1.ToLower()))
                        {
                            Reject($"{arg1} is not blacklisted!");
                            return;
                        }

                        _blacklist.RemoveUser(arg1.ToLower());
                        MainWindow.Instance.AddLog($"✅ Unblacklisted {arg1} from using Streamerfy commands (Requested by {senderName})", Colors.Orange);
                        SendMessage($"✅ Unblacklisted {arg1} from using Streamerfy commands.");
                    }
                    break;
            }
        }
        #endregion
    }
}
