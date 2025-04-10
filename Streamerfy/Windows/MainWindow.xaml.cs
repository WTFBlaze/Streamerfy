using Streamerfy.Data.Internal.Enums;
using Streamerfy.Data.Internal.Json;
using Streamerfy.Services;
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

            AddLog("✅ Streamerfy ready for use!", Colors.LimeGreen);
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
        #endregion

        #region Tab Switching Logic
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            TitleVersionText.Text = $"Streamerfy v{VersionService.CurrentVersion}";
            ShowTab(ShowTabType.LOGS);
            CheckForUpdates();

            // Process Auto Connect
            if (AutoConnectToggle.IsChecked == true)
            {
                ServiceManager.Twitch.SetChannel(App.Settings.Channel);
                ServiceManager.Twitch.Connect();

                // Update UI, e.g.:
                ConnectionStatusText.Text = "● Connected";
                ConnectionStatusText.Foreground = new SolidColorBrush(Colors.LimeGreen);
                SetConnectButtonContent(false);
            }
        }

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
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => AddLog(message, textColor));
            }
            else
            {
                var background = (logIndex % 2 == 0)
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3b3b3d")) // normal
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2a2a2b")); // darker

                var paragraph = new Paragraph
                {
                    Background = background,
                    Margin = new Thickness(0, 0, 0, 1), // small spacing between logs
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
        }

        private async void CheckForUpdates()
        {
            var (updateAvailable, latest) = await VersionService.CheckForUpdatesAsync();
            if (updateAvailable)
                MainWindow.Instance.AddLog($"🔔 A new version of Streamerfy ({latest}) is available (You're on {VersionService.CurrentVersion})! Download at https://github.com/WTFBlaze/Streamerfy/releases", Colors.Yellow);
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
                    AddLog($"🔁 Re-added {entry.TrackName} by {entry.ArtistName} to queue.", Colors.LightGreen);
                });
            }
        }

        private void Playback_BlacklistTrack_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PlaybackEntry entry)
            {
                if (ShowConfirmation($"Are you sure you want to blacklist this track?\n\n{entry.TrackName} by {entry.ArtistName}"))
                {
                    ServiceManager.Blacklist.AddTrack(entry.TrackId);
                    AddLog($"🚫 Blacklisted track: {entry.TrackName}", Colors.OrangeRed);
                }
            }
        }

        private void Playback_BlacklistArtist_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is PlaybackEntry entry)
            {
                if (ShowConfirmation($"Are you sure you want to blacklist this artist?\n\n{entry.ArtistName}"))
                {
                    ServiceManager.Blacklist.AddArtist(entry.ArtistId);
                    AddLog($"🧑‍🎤🚫 Blacklisted artist: {entry.ArtistName}", Colors.OrangeRed);
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
                    ShowInfo("Unknown user to ban!");
                    return;
                }

                if (entry.RequestedBy == "Spotify (Autoplay/Shuffle)")
                {
                    ShowInfo("You cannot ban Spotify!");
                    return;
                }

                if (entry.RequestedBy == "Streamer")
                {
                    ShowInfo("You cannot ban yourself!");
                    return;
                }
                #endregion

                if (ShowConfirmation($"Are you sure you want to blacklist this artist?\n\n{entry.ArtistName}"))
                {
                    ServiceManager.Blacklist.AddUser(entry.RequestedBy);
                    AddLog($"🔨 Banned requester: {entry.RequestedBy}", Colors.OrangeRed);
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
            QueueCmdField.Text = App.Settings.CmdQueue ?? "";
            BlacklistCmdField.Text = App.Settings.CmdBlacklist ?? "";
            UnblacklistCmdField.Text = App.Settings.CmdUnblacklist ?? "";
            BanUserCmdField.Text = App.Settings.CmdBan ?? "";
            UnbanUserCmdField.Text = App.Settings.CmdUnban ?? "";
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
            App.Settings.CmdQueue = QueueCmdField.Text;
            App.Settings.CmdBlacklist = BlacklistCmdField.Text;
            App.Settings.CmdUnblacklist = UnblacklistCmdField.Text;
            App.Settings.CmdBan = BanUserCmdField.Text;
            App.Settings.CmdUnban = UnbanUserCmdField.Text;
            App.SaveAppSettings();
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
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(channelName))
                {
                    ConnectionStatusText.Text = $"● Connected to {channelName}";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Colors.LimeGreen);
                }
                else
                {
                    ConnectionStatusText.Text = $"● Not Connected";
                    ConnectionStatusText.Foreground = new SolidColorBrush(Colors.Red);
                }
            }
        }

        public void SetConnectButtonContent(bool state)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(() => SetConnectButtonContent(state));
            }
            else
            {
                ConnectButton.Content = state ? "Disconnect" : "Connect";
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
        #endregion
    }
}