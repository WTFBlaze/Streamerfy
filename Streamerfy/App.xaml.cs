using Newtonsoft.Json;
using Streamerfy.Data.Internal.Json;
using Streamerfy.Services;
using System.Configuration;
using System.Data;
using System.IO;
using System.Text;
using System.Windows;

namespace Streamerfy
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static AppSettings Settings { get; set; }
        public static string RoamingFolder { get; private set; }
        public static string SettingsFile { get; private set; }
        public static string SongBlacklistFile { get; private set; }
        public static string ArtistBlacklistFile { get; private set; }
        public static string UserBlacklistFile { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            if (!SetupRoamingFiles() || !SetupAppSettings()) return;
            ServiceManager.InitializeServices();
        }

        private bool SetupRoamingFiles()
        {
            try
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                RoamingFolder = Path.Combine(appDataPath, "Streamerfy");
                if (!Directory.Exists(RoamingFolder))
                    Directory.CreateDirectory(RoamingFolder);

                SettingsFile = Path.Combine(RoamingFolder, "Settings.json");
                SongBlacklistFile = Path.Combine(RoamingFolder, "Blacklist_Tracks.json");
                ArtistBlacklistFile = Path.Combine(RoamingFolder, "Blacklist_Artists.json");
                UserBlacklistFile = Path.Combine(RoamingFolder, "Blacklist_Users.json");
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There was an issue Setting up Streamerfy! | Err Msg: {ex.Message}");
                return false;
            }
        }

        private bool SetupAppSettings()
        {
            try
            {
                if (!File.Exists(SettingsFile))
                {
                    File.Create(SettingsFile).Close();
                    Settings = new AppSettings();
                    SaveAppSettings();
                }
                else
                {
                    var content = File.ReadAllText(SettingsFile);
                    if (string.IsNullOrEmpty(content))
                        return false;

                    var obj = JsonConvert.DeserializeObject<AppSettings>(content);
                    if (obj == null)
                    {
                        MessageBox.Show("Failed to Setup Streamerfy Settings! | Err Msg: \"Failedto Deserialize Settings.json\"");
                        return false;
                    }

                    Settings = obj;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There was an issue Setting up Streamerfy Settings! | Err Msg: {ex.Message}");
                return false;
            }
            return true;
        }

        public static void SaveAppSettings()
        {
            var content = JsonConvert.SerializeObject(Settings, Formatting.Indented);
            File.WriteAllText(SettingsFile, content);
        }
    }

}
