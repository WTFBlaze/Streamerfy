using Streamerfy.Data.Internal.Enums;
using Streamerfy.Services;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static SpotifyAPI.Web.PlayerSetRepeatRequest;
using static Swan.Terminal;

namespace Streamerfy
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
            ShowTab(ShowTabType.LOGS);

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
            SettingsPanel.Visibility = type == ShowTabType.SETTINGS ? Visibility.Visible : Visibility.Collapsed;

            // Set Tab Highlights
            TabLogs.Tag = type == ShowTabType.LOGS ? "selected" : null;
            TabSettings.Tag = type == ShowTabType.SETTINGS ? "selected" : null;
        }

        private void Tab_Logs_Click(object sender, RoutedEventArgs e)
            => ShowTab(ShowTabType.LOGS);

        private void Tab_Settings_Click(object sender, RoutedEventArgs e)
            => ShowTab(ShowTabType.SETTINGS);
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
        #endregion

        #region Misc UI Logic
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