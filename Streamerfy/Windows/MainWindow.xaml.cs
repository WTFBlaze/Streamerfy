﻿using Streamerfy.Data.Internal.Enums;
using Streamerfy.Data.Internal.Json;
using Streamerfy.Services;
using System.Diagnostics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Streamerfy.Windows
{
    public partial class MainWindow : Window
    {
        public static MainWindow Instance { get; private set; }
        private int logIndex = 0;
        private readonly List<(string message, Color color)> _pendingLogs = new();
        private readonly object _logLock = new();

        public MainWindow()
        {
            Instance = this;
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            // Remove default padding from the RichTextBox document
            LogBox.Document.PagePadding = new Thickness(0);
            LogBox.Document.Blocks.Clear(); // Optional: ensure clean start

            // Apply Current Log Settings to Fields
            ApplySettingsToUI();
        }

        #region Window Events Logic
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                DragMove();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TitleVersionText.Text = $"Streamerfy v{VersionService.CurrentVersion}";
            ShowTab(ShowTabType.LOGS);
            CheckForUpdates();

            await LanguageService.WaitUntilReadyAsync();
            AddLog(LanguageService.Translate("Message_Ready"), Colors.LimeGreen);
            FlushQueuedLogs();

            LanguageService.ViewModel.ConnectionStatusDynamic = ServiceManager.Twitch.IsConnected ? 
                LanguageService.Translate("ConnectionStatus_Connected", new { CHANNEL = ServiceManager.Twitch.GetChannel() })
                : LanguageService.Translate("ConnectionStatus_Disconnected");

            PopulateLanguageDropdown();

            // Process Auto Connect
            if (AutoConnectToggle.IsChecked == true)
            {
                ServiceManager.Twitch.SetChannel(App.Settings.Channel);
                ServiceManager.Twitch.Connect();
            }
        }

        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }
        #endregion

        #region Tab Switching Logic
        private void ShowTab(ShowTabType type)
        {
            // Set Visbility
            LogsPanel.Visibility = type == ShowTabType.LOGS ? Visibility.Visible : Visibility.Collapsed;
            HistoryPanel.Visibility = type == ShowTabType.HISTORY ? Visibility.Visible : Visibility.Collapsed;
            ChangelogPanel.Visibility = type == ShowTabType.CHANGELOGS ? Visibility.Visible : Visibility.Collapsed;
            SettingsPanel.Visibility = type == ShowTabType.SETTINGS ? Visibility.Visible : Visibility.Collapsed;

            // Set Tab Highlights
            TabLogs.Tag = type == ShowTabType.LOGS ? "selected" : null;
            TabHistory.Tag = type == ShowTabType.HISTORY ? "selected" : null;
            TabChangelog.Tag = type == ShowTabType.CHANGELOGS ? "selected" : null;
            TabSettings.Tag = type == ShowTabType.SETTINGS ? "selected" : null;
        }

        private void Tab_Logs_Click(object sender, RoutedEventArgs e)
            => ShowTab(ShowTabType.LOGS);

        private void Tab_Settings_Click(object sender, RoutedEventArgs e)
            => ShowTab(ShowTabType.SETTINGS);

        private void Tab_Changelog_Click(object sender, RoutedEventArgs e)
            => ShowTab(ShowTabType.CHANGELOGS);

        private void Tab_History_Click(object sender, RoutedEventArgs e)
        {
            ShowTab(ShowTabType.HISTORY);
            RefreshHistoryList();
        }

        #endregion

        #region Logs Logic
        public void AddLog(string message, Color textColor)
        {
            if (!LanguageService.IsInitialized)
            {
                lock (_logLock)
                {
                    _pendingLogs.Add((message, textColor));
                }
                return;
            }

            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => AddLog(message, textColor));
                return;
            }

            var background = (logIndex % 2 == 0)
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3b3b3d"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2a2a2b"));

            var paragraph = new Paragraph
            {
                Background = background,
                Margin = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(4, 2, 4, 2)
            };

            paragraph.Inlines.Add(new Run(message)
            {
                Foreground = new SolidColorBrush(textColor)
            });

            LogBox.Document.Blocks.Add(paragraph);
            logIndex++;

            LogBox.ScrollToEnd();
        }

        public void FlushQueuedLogs()
        {
            List<(string message, Color color)> logsToFlush;
            lock (_logLock)
            {
                logsToFlush = _pendingLogs.ToList();
                _pendingLogs.Clear();
            }

            foreach (var (msg, color) in logsToFlush)
            {
                AddLog(msg, color);
            }
        }

        private async void CheckForUpdates()
        {
            var (updateAvailable, latest) = await VersionService.CheckForUpdatesAsync();
            if (updateAvailable)
                MainWindow.Instance.AddLog(LanguageService.Translate("Message_Update_Available", new { LATEST_VERSION = latest, CURRENT_VERSION = VersionService.CurrentVersion }), Colors.Yellow);
        }
        #endregion

        #region Playback History Logic
        public void RefreshHistoryList()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(RefreshHistoryList);
            }
            else
            {
                if (HistoryPanel.Visibility == Visibility.Visible)
                {
                    HistoryList.ItemsSource = null;
                    HistoryList.ItemsSource = ServiceManager.Playback.Entries.OrderByDescending(h => h.Timestamp);
                }
            }
        }

        private void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            bool confirm = ShowConfirmation("Are you sure you want to clear the playback history? This cannot be undone.");
            if (!confirm) return;

            ServiceManager.Playback.Clear();
            HistoryList.ItemsSource = null;
            HistoryList.ItemsSource = ServiceManager.Playback.Entries.OrderByDescending(h => h.Timestamp);

            ShowInfo("Playback history was successfully cleared.");
        }


        private void Playback_Requeue_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PlaybackEntry entry)
            {
                Task.Run(async () =>
                {
                    await ServiceManager.Spotify.AddToQueue($"https://open.spotify.com/track/{entry.TrackId}", "Streamer");
                    AddLog(LanguageService.Translate("Message_ReQueued", new { SONG = entry.TrackName, ARTIST = entry.ArtistName }), Colors.LightGreen);
                });
            }
        }

        private void Playback_BlacklistTrack_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PlaybackEntry entry)
            {
                if (ShowConfirmation(LanguageService.Translate("Message_Blacklist_Track_Confirmation", new { SONG = entry.TrackName, ARTIST = entry.ArtistName })))
                {
                    ServiceManager.Blacklist.AddTrack(entry.TrackId);
                    AddLog(LanguageService.Translate("Message_Blacklist_Track_Success", new { SONG = entry.TrackName, ARTIST = entry.ArtistName, REQUESTER = "Streamer"}), Colors.OrangeRed);
                }
            }
        }

        private void Playback_BlacklistArtist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PlaybackEntry entry)
            {
                if (ShowConfirmation(LanguageService.Translate("Message_Blacklist_Artist_Confirmation", new { ARTIST = entry.ArtistName })))
                {
                    ServiceManager.Blacklist.AddArtist(entry.ArtistId);
                    AddLog(LanguageService.Translate("Message_Blacklist_Artist_Success", new { ARTIST = entry.ArtistName, REQUESTER = "Streamer" }), Colors.OrangeRed);
                }
            }
        }

        private void Playback_BanUser_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PlaybackEntry entry)
            {
                #region Idiot Proofing lol
                if (entry.RequestedBy == "Unknown")
                {
                    ShowInfo(LanguageService.Translate("Message_Ban_Unknown"));
                    return;
                }

                if (entry.RequestedBy == "Spotify (Autoplay/Shuffle)")
                {
                    ShowInfo(LanguageService.Translate("Message_Ban_Spotify"));
                    return;
                }

                if (entry.RequestedBy == "Streamer")
                {
                    ShowInfo(LanguageService.Translate("Message_Chat_Ban_Streamer"));
                    return;
                }
                #endregion

                if (ShowConfirmation(LanguageService.Translate("Message_Ban_User_Confirmation", new { })))
                {
                    ServiceManager.Blacklist.AddUser(entry.RequestedBy);
                    AddLog(LanguageService.Translate("Message_Ban_Success", new { TARGET = entry.RequestedBy, REQUESTER = "Streamer" }), Colors.OrangeRed);
                }
            }
        }

        #endregion

        #region Changelogs Logic
        private void ChangelogToggle_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button && button.Tag is string panelName)
            {
                var panel = FindName(panelName) as StackPanel;
                if (panel != null) panel.Visibility = Visibility.Visible;
            }
        }

        private void ChangelogToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton button && button.Tag is string panelName)
            {
                var panel = FindName(panelName) as StackPanel;
                if (panel != null) panel.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Settings Logic
        private void ApplySettingsToUI()
        {
            AutoConnectToggle.IsChecked = App.Settings.AutoConnect;
            BlockExplicitToggle.IsChecked = App.Settings.BlockExplicit;
            UsernameField.Text = App.Settings.BotUsername ?? "";
            OAuthField.Text = App.Settings.BotOAuth ?? "";
            ChannelField.Text = App.Settings.Channel ?? "";
            ClientIDField.Text = App.Settings.SpotifyClientId ?? "";
            ClientSecretField.Text = App.Settings.SpotifyClientSecret ?? "";
            CmdPrefixField.Text = App.Settings.CmdPrefix ?? "";
            QueueCmdField.Text = App.Settings.CmdQueue.Command ?? "";
            BlacklistCmdField.Text = App.Settings.CmdBlacklist.Command ?? "";
            UnblacklistCmdField.Text = App.Settings.CmdUnblacklist.Command ?? "";
            BanUserCmdField.Text = App.Settings.CmdBan.Command ?? "";
            UnbanUserCmdField.Text = App.Settings.CmdUnban.Command ?? "";
            QueueAllowVIP.IsChecked = App.Settings.CmdQueue.AllowVIP;
            QueueAllowSub.IsChecked = App.Settings.CmdQueue.AllowSub;
            QueueAllowMod.IsChecked = App.Settings.CmdQueue.AllowMod;
            BlacklistAllowVIP.IsChecked = App.Settings.CmdBlacklist.AllowVIP;
            BlacklistAllowSub.IsChecked = App.Settings.CmdBlacklist.AllowSub;
            BlacklistAllowMod.IsChecked = App.Settings.CmdBlacklist.AllowMod;
            UnblacklistAllowVIP.IsChecked = App.Settings.CmdUnblacklist.AllowVIP;
            UnblacklistAllowSub.IsChecked = App.Settings.CmdUnblacklist.AllowSub;
            UnblacklistAllowMod.IsChecked = App.Settings.CmdUnblacklist.AllowMod;
            BanAllowVIP.IsChecked = App.Settings.CmdBan.AllowVIP;
            BanAllowSub.IsChecked = App.Settings.CmdBan.AllowSub;
            BanAllowMod.IsChecked = App.Settings.CmdBan.AllowMod;
            UnbanAllowVIP.IsChecked = App.Settings.CmdUnban.AllowVIP;
            UnbanAllowSub.IsChecked = App.Settings.CmdUnban.AllowSub;
            UnbanAllowMod.IsChecked = App.Settings.CmdUnban.AllowMod;
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            App.Settings.AutoConnect = AutoConnectToggle.IsChecked == true;
            App.Settings.BlockExplicit = BlockExplicitToggle.IsChecked == true;
            App.Settings.BotUsername = UsernameField.Text;
            App.Settings.BotOAuth = OAuthField.Text;
            App.Settings.Channel = ChannelField.Text;
            App.Settings.SpotifyClientId = ClientIDField.Text;
            App.Settings.SpotifyClientSecret = ClientSecretField.Text;
            App.Settings.CmdPrefix = CmdPrefixField.Text;
            App.Settings.CmdQueue.Command = QueueCmdField.Text;
            App.Settings.CmdBlacklist.Command = BlacklistCmdField.Text;
            App.Settings.CmdUnblacklist.Command = UnblacklistCmdField.Text;
            App.Settings.CmdBan.Command = BanUserCmdField.Text;
            App.Settings.CmdUnban.Command = UnbanUserCmdField.Text;
            App.Settings.CmdQueue.AllowVIP = QueueAllowVIP.IsChecked ?? false;
            App.Settings.CmdQueue.AllowSub = QueueAllowSub.IsChecked ?? false;
            App.Settings.CmdQueue.AllowMod = QueueAllowMod.IsChecked ?? false;
            App.Settings.CmdBlacklist.AllowVIP = BlacklistAllowVIP.IsChecked ?? false;
            App.Settings.CmdBlacklist.AllowSub = BlacklistAllowSub.IsChecked ?? false;
            App.Settings.CmdBlacklist.AllowMod = BlacklistAllowMod.IsChecked ?? false;
            App.Settings.CmdUnblacklist.AllowVIP = UnblacklistAllowVIP.IsChecked ?? false;
            App.Settings.CmdUnblacklist.AllowSub = UnblacklistAllowSub.IsChecked ?? false;
            App.Settings.CmdUnblacklist.AllowMod = UnblacklistAllowMod.IsChecked ?? false;
            App.Settings.CmdBan.AllowVIP = BanAllowVIP.IsChecked ?? false;
            App.Settings.CmdBan.AllowSub = BanAllowSub.IsChecked ?? false;
            App.Settings.CmdBan.AllowMod = BanAllowMod.IsChecked ?? false;
            App.Settings.CmdUnban.AllowVIP = UnbanAllowVIP.IsChecked ?? false;
            App.Settings.CmdUnban.AllowSub = UnbanAllowSub.IsChecked ?? false;
            App.Settings.CmdUnban.AllowMod = UnbanAllowMod.IsChecked ?? false;
            App.SaveAppSettings();
        }

        private async void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LanguageSelector.SelectedItem is string selectedLang && selectedLang != App.Settings.Language)
            {
                App.Settings.Language = selectedLang;
                App.SaveAppSettings();

                await LanguageService.InitializeAsync();
                await LanguageService.WaitUntilReadyAsync();

                SetConnectionStatus(ServiceManager.Twitch.GetChannel());
            }
        }

        private void UsernameField_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = UsernameField;
            int caretIndex = tb.CaretIndex;
            string lower = tb.Text.ToLower();

            if (tb.Text != lower)
            {
                tb.Text = lower;
                tb.CaretIndex = caretIndex;
            }
        }

        private void OpenSettingsFolder_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("explorer.exe", App.RoamingFolder);
        }

        private void TwitchSectionToggle_Checked(object sender, RoutedEventArgs e)
        {
            TwitchSettingsPanel.Visibility = Visibility.Visible;
        }

        private void TwitchSectionToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            TwitchSettingsPanel.Visibility = Visibility.Collapsed;
        }

        private void SpotifySectionToggle_Checked(object sender, RoutedEventArgs e)
        {
            SpotifySettingsPanel.Visibility = Visibility.Visible;
        }

        private void SpotifySectionToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            SpotifySettingsPanel.Visibility = Visibility.Collapsed;
        }

        private void CommandsSectionToggle_Checked(object sender, RoutedEventArgs e)
        {
            CommandsSectionPanel.Visibility = Visibility.Visible;
        }

        private void CommandsSectionToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            CommandsSectionPanel.Visibility = Visibility.Collapsed;
        }

        private void PermissionsSectionToggle_Checked(object sender, RoutedEventArgs e)
        {
            PermissionsSectionPanel.Visibility = Visibility.Visible;
        }

        private void PermissionsSectionToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            PermissionsSectionPanel.Visibility = Visibility.Collapsed;
        }

        #endregion

        #region Misc UI Logic
        public bool ShowConfirmation(string message)
        {
            var dialog = new ConfirmDialog(message)
            {
                Owner = this
            };
            dialog.ShowDialog();
            return dialog.Result;
        }

        public void ShowInfo(string message)
        {
            var dialog = new InfoDialog(message)
            {
                Owner = this
            };
            dialog.ShowDialog();
        }

        public void SetConnectionStatus(string channelName)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => SetConnectionStatus(channelName));
                return;
            }

            LanguageService.ViewModel.ConnectionStatusDynamic = string.IsNullOrWhiteSpace(channelName)
                ? LanguageService.Translate("ConnectionStatus_Disconnected")
                : LanguageService.Translate("ConnectionStatus_Connected", new { CHANNEL = channelName });

            ConnectionStatusText.Foreground = string.IsNullOrWhiteSpace(channelName)
                ? new SolidColorBrush(Colors.Red)
                : new SolidColorBrush(Colors.LimeGreen);
        }

        public void SetConnectButtonContent(bool state)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => SetConnectButtonContent(state));
            }
            else
            {
                LanguageService.ViewModel.ConnectButtonLabel = state
                    ? LanguageService.ViewModel.Button_Disconnect
                    : LanguageService.ViewModel.Button_Connect;
            }
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ServiceManager.Twitch.IsConnected)
            {
                ServiceManager.Twitch.SetChannel(App.Settings.Channel);
                ServiceManager.Twitch.Connect();
            }
            else
            {
                ServiceManager.Twitch.Disconnect();
            }
        }

        private async void PopulateLanguageDropdown()
        {
            var langs = await LanguageService.FetchAvailableLanguagesAsync();
            LanguageSelector.ItemsSource = langs;
            LanguageSelector.SelectedItem = App.Settings.Language ?? "en";
        }
        #endregion
    }
}