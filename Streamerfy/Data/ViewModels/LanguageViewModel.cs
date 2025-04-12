using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Streamerfy.Data.ViewModels
{
    public class LanguageViewModel : INotifyPropertyChanged
    {
        private readonly Dictionary<string, string> _entries = new();

        public event PropertyChangedEventHandler? PropertyChanged;

        public string this[string key]
        {
            get => _entries.TryGetValue(key, out var value) ? value : $"[{key}]";
            set
            {
                if (_entries.ContainsKey(key) && _entries[key] == value)
                    return;

                _entries[key] = value;
                OnPropertyChanged(key);
            }
        }

        protected void OnPropertyChanged(string key) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(key));

        #region Bindings

        #region Tab Buttons
        public string Tabs_Logs
        {
            get => this["Tabs_Logs"];
            set => this["Tabs_Logs"] = value;
        }

        public string Tabs_History
        {
            get => this["Tabs_History"];
            set => this["Tabs_History"] = value;
        }

        public string Tabs_Changelog
        {
            get => this["Tabs_Changelog"];
            set => this["Tabs_Changelog"] = value;
        }

        public string Tabs_Settings
        {
            get => this["Tabs_Settings"];
            set => this["Tabs_Settings"] = value;
        }
        #endregion

        #region Global UI
        private string _connectionStatus = "Not Connected"; // Default fallback
        public string ConnectionStatusDynamic
        {
            get => _connectionStatus;
            set
            {
                if (_connectionStatus != value)
                {
                    _connectionStatus = value;
                    OnPropertyChanged(nameof(ConnectionStatusDynamic));
                }
            }
        }

        private string _connectButtonLabel;
        public string ConnectButtonLabel
        {
            get => _connectButtonLabel;
            set
            {
                if (_connectButtonLabel != value)
                {
                    _connectButtonLabel = value;
                    OnPropertyChanged(nameof(ConnectButtonLabel));
                }
            }
        }

        public string Button_Connect
        {
            get => this["Button_Connect"];
            set => this["Button_Connect"] = value;
        }

        public string Button_Disconnect
        {
            get => this["Button_Disconnect"];
            set => this["Button_Disconnect"] = value;
        }
        #endregion

        #region Logs Page
        public string Logs_Title
        {
            get => this["Logs_Title"];
            set => this["Logs_Title"] = value;
        }
        #endregion

        #region History Page
        public string History_Title
        {
            get => this["History_Title"];
            set => this["History_Title"] = value;
        }

        public string History_Clear
        {
            get => this["History_Clear"];
            set => this["History_Clear"] = value;
        }
        #endregion

        #region Changelogs Page
        public string Changelog_Title
        {
            get => this["Changelog_Title"];
            set => this["Changelog_Title"] = value;
        }
        #endregion

        #region Settings Page
        public string Settings_Title
        {
            get => this["Settings_Title"];
            set => this["Settings_Title"] = value;
        }

        public string Settings_Language
        {
            get => this["Settings_Language"];
            set => this["Settings_Language"] = value;
        }

        public string Settings_Auto_Connect
        {
            get => this["Settings_Auto_Connect"];
            set => this["Settings_Auto_Connect"] = value;
        }

        public string Settings_Block_Explicit
        {
            get => this["Settings_Block_Explicit"];
            set => this["Settings_Block_Explicit"] = value;
        }

        public string Settings_Category_Twitch
        {
            get => this["Settings_Category_Twitch"];
            set => this["Settings_Category_Twitch"] = value;
        }

        public string Settings_Bot_Username
        {
            get => this["Settings_Bot_Username"];
            set => this["Settings_Bot_Username"] = value;
        }

        public string Settings_Bot_OAuth
        {
            get => this["Settings_Bot_OAuth"];
            set => this["Settings_Bot_OAuth"] = value;
        }

        public string Settings_Channel
        {
            get => this["Settings_Channel"];
            set => this["Settings_Channel"] = value;
        }

        public string Settings_Category_Spotify
        {
            get => this["Settings_Category_Spotify"];
            set => this["Settings_Category_Spotify"] = value;
        }

        public string Settings_Spotify_ID
        {
            get => this["Settings_Spotify_ID"];
            set => this["Settings_Spotify_ID"] = value;
        }

        public string Settings_Spotify_Secret
        {
            get => this["Settings_Spotify_Secret"];
            set => this["Settings_Spotify_Secret"] = value;
        }
        public string Settings_Category_Commands
        {
            get => this["Settings_Category_Commands"];
            set => this["Settings_Category_Commands"] = value;
        }

        public string Settings_Command_Prefix
        {
            get => this["Settings_Command_Prefix"];
            set => this["Settings_Command_Prefix"] = value;
        }

        public string Settings_Command_Queue
        {
            get => this["Settings_Command_Queue"];
            set => this["Settings_Command_Queue"] = value;
        }

        public string Settings_Command_Blacklist
        {
            get => this["Settings_Command_Blacklist"];
            set => this["Settings_Command_Blacklist"] = value;
        }

        public string Settings_Command_Unblacklist
        {
            get => this["Settings_Command_Unblacklist"];
            set => this["Settings_Command_Unblacklist"] = value;
        }

        public string Settings_Command_Ban
        {
            get => this["Settings_Command_Ban"];
            set => this["Settings_Command_Ban"] = value;
        }

        public string Settings_Command_Unban
        {
            get => this["Settings_Command_Unban"];
            set => this["Settings_Command_Unban"] = value;
        }

        public string Settings_Category_Permissions
        {
            get => this["Settings_Category_Permissions"];
            set => this["Settings_Category_Permissions"] = value;
        }

        public string Settings_Permissions_Note_1
        {
            get => this["Settings_Permissions_Note_1"];
            set => this["Settings_Permissions_Note_1"] = value;
        }

        public string Settings_Permissions_Note_2
        {
            get => this["Settings_Permissions_Note_2"];
            set => this["Settings_Permissions_Note_2"] = value;
        }

        public string Settings_Permissions_Queue
        {
            get => this["Settings_Permissions_Queue"];
            set => this["Settings_Permissions_Queue"] = value;
        }

        public string Settings_Permissions_Blacklist
        {
            get => this["Settings_Permissions_Blacklist"];
            set => this["Settings_Permissions_Blacklist"] = value;
        }

        public string Settings_Permissions_Unblacklist
        {
            get => this["Settings_Permissions_Unblacklist"];
            set => this["Settings_Permissions_Unblacklist"] = value;
        }

        public string Settings_Permissions_Ban
        {
            get => this["Settings_Permissions_Ban"];
            set => this["Settings_Permissions_Ban"] = value;
        }

        public string Settings_Permissions_Unban
        {
            get => this["Settings_Permissions_Unban"];
            set => this["Settings_Permissions_Unban"] = value;
        }

        public string Settings_Role_VIP
        {
            get => this["Settings_Role_VIP"];
            set => this["Settings_Role_VIP"] = value;
        }

        public string Settings_Role_Subs
        {
            get => this["Settings_Role_Subs"];
            set => this["Settings_Role_Subs"] = value;
        }

        public string Settings_Role_Mods
        {
            get => this["Settings_Role_Mods"];
            set => this["Settings_Role_Mods"] = value;
        }

        public string Settings_Save
        {
            get => this["Settings_Save"];
            set => this["Settings_Save"] = value;
        }
        #endregion

        #endregion
    }
}
