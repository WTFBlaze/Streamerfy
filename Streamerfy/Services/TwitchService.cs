using Streamerfy.Windows;
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
            MainWindow.Instance.AddLog(LanguageService.Translate("Message_Twitch_Disconnected"), Colors.OrangeRed);
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
            MainWindow.Instance.AddLog(LanguageService.Translate("Message_Twitch_Connected"), Colors.LimeGreen);
            MainWindow.Instance.SetConnectButtonContent(true);
        }

        private async void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var msg = e.ChatMessage.Message;
            var user = e.ChatMessage;
            var senderName = user.Username;

            if (_blacklist.IsUserBlacklisted(user.Username))
            {
                Reject(LanguageService.Translate("Message_Chat_UserBlacklisted"));
                return;
            }

            if (!e.ChatMessage.Message.StartsWith(App.Settings.CmdPrefix))
                return;

            var split = msg.Split(' ');
            var rawCommand = split[0];
            var prefix = App.Settings.CmdPrefix;

            var command = rawCommand.StartsWith(prefix)
                ? rawCommand.Substring(prefix.Length).ToLower()
                : rawCommand.ToLower();

            var arg1 = split.Length > 1 ? split[1] : null;
            var arg2 = split.Length > 2 ? split[2] : null;

            switch (command)
            {
                case var cmd when cmd == App.Settings.CmdQueue:
                    {
                        if (arg1 == null)
                        {
                            Reject(LanguageService.Translate("Message_Chat_Missing_Link"));
                            return;
                        }

                        var url = arg1;
                        var track = await _spotify.GetTrackInfo(url);
                        if (track == null)
                        {
                            Reject(LanguageService.Translate("Message_Chat_Track_Not_Found"));
                            MainWindow.Instance.AddLog(LanguageService.Translate("Message_Track_Not_Found", new { REQUESTER = senderName }), Colors.OrangeRed);
                            return;
                        }

                        if (_blacklist.IsTrackBlacklisted(track.ID))
                        {
                            Reject(LanguageService.Translate("Message_Chat_Track_Blacklisted"));
                            MainWindow.Instance.AddLog(LanguageService.Translate("Message_Track_Blacklisted", new { REQUESTER = senderName }), Colors.OrangeRed);
                            return;
                        }

                        if (_blacklist.IsArtistBlacklisted(track.Artist.ID))
                        {
                            Reject(LanguageService.Translate("Message_Chat_Artist_Blacklisted"));
                            MainWindow.Instance.AddLog(LanguageService.Translate("Message_Artist_Blacklisted", new { REQUESTER = senderName }), Colors.OrangeRed);
                            return;
                        }

                        if (App.Settings.BlockExplicit && track.Explicit)
                        {
                            Reject(LanguageService.Translate("Message_Chat_No_Explict"));
                            MainWindow.Instance.AddLog(LanguageService.Translate("Message_No_Explicit", new { REQUESTER = senderName }), Colors.OrangeRed);
                            return;
                        }

                        bool success = await _spotify.AddToQueue(url);

                        if (!success)
                        {
                            SendMessage(LanguageService.Translate("Message_Chat_Queue_Failure"));
                            MainWindow.Instance.AddLog(LanguageService.Translate("Message_Queue_Failure", new { REQUESTER = senderName }), Colors.OrangeRed);
                        }
                        else
                        {
                            SendMessage(LanguageService.Translate("Message_Chat_Queue_Success", new { SONG = track.Name, ARTIST = track.Artist.Name }));
                            MainWindow.Instance.AddLog(LanguageService.Translate("Message_Queue_Success", new { SONG = track.Name, ARTIST = track.Artist.Name, REQUESTER = senderName }), Colors.SkyBlue);
                        }
                    }
                    break;

                case var cmd when cmd == App.Settings.CmdBlacklist:
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
                                        Reject(LanguageService.Translate("Message_Chat_Track_Not_Found"));
                                        return;
                                    }

                                    if (_blacklist.IsTrackBlacklisted(track.ID))
                                        Reject(LanguageService.Translate("Message_Blacklist_Track_Exists"));
                                    else
                                    {
                                        _blacklist.AddTrack(track.ID);
                                        SendMessage(LanguageService.Translate("Message_Chat_Blacklist_Track_Success", new { SONG = track.Name, ARTIST = track.Artist.Name }));
                                        MainWindow.Instance.AddLog(LanguageService.Translate("Message_Blacklist_Track_Success", new { SONG = track.Name, ARTIST = track.Artist.Name, REQUESTER = senderName }), Colors.Yellow);
                                    }
                                }
                                break;

                            case "band":
                            case "artist":
                                {
                                    var artist = await _spotify.GetArtistInfo(arg2);
                                    if (artist == null)
                                    {
                                        Reject(LanguageService.Translate("Message_Chat_Artist_Not_Found"));
                                        return;
                                    }

                                    if (_blacklist.IsArtistBlacklisted(artist.ID))
                                        Reject(LanguageService.Translate("Message_Blacklist_Artist_Exists"));
                                    else
                                    {
                                        _blacklist.AddArtist(artist.ID);
                                        SendMessage(LanguageService.Translate("Message_Chat_Blacklist_Artist_Success", new { ARTIST = artist.Name }));
                                        MainWindow.Instance.AddLog(LanguageService.Translate("Message_Blacklist_Artist_Success", new { ARTIST = artist.Name, REQUESTER = senderName }), Colors.Yellow);
                                    }

                                }
                                break;
                        }
                    }
                    break;

                case var cmd when cmd == App.Settings.CmdUnblacklist:
                    {
                        if (!user.IsModerator && !user.IsBroadcaster) return;
                        if (arg1 == null || arg2 == null)
                        {
                            Reject(LanguageService.Translate("Message_Unblacklist_Usage"));
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
                                        Reject(LanguageService.Translate("Message_Chat_Track_Not_Found"));
                                        return;
                                    }

                                    if (!_blacklist.IsTrackBlacklisted(track.ID))
                                        Reject(LanguageService.Translate("Message_Unblacklist_Track_Not_Found"));
                                    else
                                    {
                                        _blacklist.RemoveTrack(track.ID);
                                        SendMessage(LanguageService.Translate("Message_Chat_Unblacklist_Track_Success", new { SONG = track.Name, ARTIST = track.Artist.Name }));
                                        MainWindow.Instance.AddLog(LanguageService.Translate("Message_Unblacklist_Track_Success", new { SONG = track.Name, ARTIST = track.Artist.Name, REQUESTER = senderName }), Colors.YellowGreen);
                                    }
                                }
                                break;

                            case "band":
                            case "artist":
                                {
                                    var artist = await _spotify.GetArtistInfo(arg2);
                                    if (artist == null)
                                    {
                                        Reject(LanguageService.Translate("Message_Chat_Artist_Not_Found"));
                                        return;
                                    }

                                    if (!_blacklist.IsArtistBlacklisted(artist.ID))
                                        Reject(LanguageService.Translate("Message_Unblacklist_Artist_Not_Found"));
                                    else
                                    {
                                        _blacklist.RemoveArtist(artist.ID);
                                        SendMessage(LanguageService.Translate("Message_Chat_Unblacklist_Artist_Success", new { ARTIST = artist.Name }));
                                        MainWindow.Instance.AddLog(LanguageService.Translate("Message_Unblacklist_Artist_Success", new { ARTIST = artist.Name, REQUESTER = senderName }), Colors.YellowGreen);
                                    }

                                }
                                break;
                        }

                    }
                    break;

                case var cmd when cmd == App.Settings.CmdBan:
                    {
                        if (!user.IsModerator && !user.IsBroadcaster) return;
                        if (arg1 == null)
                        {
                            Reject(LanguageService.Translate("Message_Ban_Usage"));
                            return;
                        }

                        if (arg1.ToLower().Equals(_client.JoinedChannels[0].Channel.ToLower()))
                        {
                            Reject(LanguageService.Translate("Message_Chat_Ban_Streamer"));
                            return;
                        }

                        if (_blacklist.IsUserBlacklisted(arg1.ToLower()))
                        {
                            Reject(LanguageService.Translate("Message_Chat_Ban_Exists", new { TARGET = arg1 }));
                            return;
                        }

                        _blacklist.AddUser(arg1.ToLower());
                        SendMessage(LanguageService.Translate("Message_Chat_Ban_Success", new { TARGET = arg1 }));
                        MainWindow.Instance.AddLog(LanguageService.Translate("Message_Ban_Success", new { TARGET = arg1, REQUESTER = senderName }), Colors.Orange);
                    }
                    break;

                case var cmd when cmd == App.Settings.CmdUnban:
                    {
                        if (!user.IsModerator && !user.IsBroadcaster) return;
                        if (arg1 == null)
                        {
                            Reject(LanguageService.Translate("Message_Unban_Usage"));
                            return;
                        }

                        if (!_blacklist.IsUserBlacklisted(arg1.ToLower()))
                        {
                            Reject(LanguageService.Translate("Message_Chat_Unban_Not_Found", new { TARGET = arg1 }));
                            return;
                        }

                        _blacklist.RemoveUser(arg1.ToLower());
                        SendMessage(LanguageService.Translate("Message_Chat_Unban_Success", new { TARGET = arg1 }));
                        MainWindow.Instance.AddLog(LanguageService.Translate("Message_Unban_Success", new { TARGET = arg1, REQUESTER = senderName }), Colors.Orange);
                    }
                    break;
            }
        }
        #endregion
    }
}
