using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Streamerfy.Data.Internal.Json;
using Streamerfy.Services;
using Streamerfy.Utils;
using Streamerfy.Windows;
using System.Configuration;
using System.Data;
using System.IO;
using System.Reflection;
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
        public static string PlaybackFile { get; private set; }

        public static string LanguagesFolder { get; private set; }

        public static string BlacklistFolder { get; private set; }
        public static string SongBlacklistFile { get; private set; }
        public static string ArtistBlacklistFile { get; private set; }
        public static string UserBlacklistFile { get; private set; }

        public static string NowPlayingFolder { get; private set; }
        public static string NowPlayingJSONFile { get; private set; }
        public static string NowPlayingHTMLFile { get; private set; }
        public static string NowPlayingServerHTMLFile { get; private set; }
        public static string NowPlayingTXTFile { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += (s, ex) =>
            {
                File.WriteAllText("crash.log", ex.ExceptionObject.ToString());
            };

            DispatcherUnhandledException += (s, ex) =>
            {
                File.WriteAllText("crash_dispatcher.log", ex.Exception.Message + "\n" + ex.Exception.StackTrace);
                ex.Handled = true;
            };

            base.OnStartup(e);

            DebugConsole.Init();

            bool roamingFiles = SetupRoamingFiles();
            bool appSettings = SetupAppSettings();

            if (!roamingFiles || !appSettings)
            {
                MessageBox.Show($"Failed to initialize application settings. (RoamingFiles: {roamingFiles} | AppSettings: {appSettings}) Exiting.");
                return;
            }

            Task.Run(async () =>
            {
                await LanguageService.InitializeAsync();
            });

            EnsureNowPlayingHtmlExists();
            EnsureNowPlayingServerHtmlExists();

            ServiceManager.InitializeServices();

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            ServiceManager.NowPlaying.Clear();
        }

        private bool SetupRoamingFiles()
        {
            try
            {
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

                // Ensure Directories
                RoamingFolder = EnsureDirectoryExistance(appDataPath, "Streamerfy");
                BlacklistFolder = EnsureDirectoryExistance(RoamingFolder, "Blacklist");
                NowPlayingFolder = EnsureDirectoryExistance(RoamingFolder, "NowPlaying");
                LanguagesFolder = EnsureDirectoryExistance(RoamingFolder, "Languages");

                // Ensure Files
                SettingsFile = EnsureFileExistance(RoamingFolder, "Settings.json", false);
                PlaybackFile = EnsureFileExistance(RoamingFolder, "PlaybackHistory.json");
                SongBlacklistFile = EnsureFileExistance(BlacklistFolder, "Blacklist_Tracks.json");
                ArtistBlacklistFile = EnsureFileExistance(BlacklistFolder, "Blacklist_Artists.json");
                UserBlacklistFile = EnsureFileExistance(BlacklistFolder, "Blacklist_Users.json");
                NowPlayingJSONFile = EnsureFileExistance(NowPlayingFolder, "NowPlaying.json");
                NowPlayingHTMLFile = EnsureFileExistance(NowPlayingFolder, "NowPlaying.html", false);
                NowPlayingServerHTMLFile = EnsureFileExistance(NowPlayingFolder, "NowPlayingServer.html", false);
                NowPlayingTXTFile = EnsureFileExistance(NowPlayingFolder, "NowPlaying.txt");

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"There was an issue Setting up Streamerfy! | Err Msg: {ex.Message}");
                return false;
            }
        }

        #region App Settings Methods
        private bool SetupAppSettings()
        {
            try
            {
                if (!File.Exists(SettingsFile))
                {
                    File.Create(SettingsFile).Close();
                    Settings = new AppSettings();
                    SaveAppSettings();
                    return true;
                }

                if (File.ReadAllText(SettingsFile).Length == 0)
                {
                    Settings = new AppSettings();
                    SaveAppSettings();
                }
                else
                {
                    var content = File.ReadAllText(SettingsFile);

                    if (string.IsNullOrEmpty(content))
                    {
                        return false;
                    }

                    var rawJson = JObject.Parse(content);

                    MigrateOldCommand(rawJson, "CmdQueue");
                    MigrateOldCommand(rawJson, "CmdBlacklist");
                    MigrateOldCommand(rawJson, "CmdUnblacklist");
                    MigrateOldCommand(rawJson, "CmdBan");
                    MigrateOldCommand(rawJson, "CmdUnban");

                    var updatedJson = rawJson.ToString();
                    Settings = JsonConvert.DeserializeObject<AppSettings>(updatedJson)!;

                    SaveAppSettings();
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

        private void MigrateOldCommand(JObject rawJson, string propName)
        {
            var token = rawJson[propName];
            if (token != null && token.Type == JTokenType.String)
            {
                var oldValue = token.Value<string>();
                rawJson[propName] = new JObject
                {
                    ["Command"] = oldValue,
                    ["AllowVIP"] = false,
                    ["AllowSub"] = false,
                    ["AllowMod"] = false
                };
            }
        }
        #endregion

        #region Existance Methods
        private void EnsureNowPlayingHtmlExists()
        {
            if (File.Exists(NowPlayingHTMLFile)) 
                return;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                const string resourceName = "Streamerfy.Assets.NowPlayingTemplate.html";

                using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    MessageBox.Show("Failed to load NowPlaying.html from resources.", "Streamerfy", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using var reader = new StreamReader(stream);
                string html = reader.ReadToEnd();
                File.WriteAllText(NowPlayingHTMLFile, html);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error writing NowPlaying.html:\n{ex.Message}", "Streamerfy", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void EnsureNowPlayingServerHtmlExists()
        {
            if (File.Exists(NowPlayingServerHTMLFile))
                return;

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                const string resourceName = "Streamerfy.Assets.NowPlayingServerTemplate.html";

                using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    MessageBox.Show("Failed to load NowPlayingServer.html from resources.", "Streamerfy", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using var reader = new StreamReader(stream);
                string html = reader.ReadToEnd();
                File.WriteAllText(NowPlayingServerHTMLFile, html);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error writing NowPlayingServer.html:\n{ex.Message}", "Streamerfy", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string EnsureDirectoryExistance(string path, string directoryName)
        {
            var fullPath = Path.Combine(path, directoryName);
            if (!Directory.Exists(fullPath))
                Directory.CreateDirectory(fullPath);
            return fullPath;
        }

        private string EnsureFileExistance(string path, string fileName, bool createFile = true)
        {
            var fullPath = Path.Combine(path, fileName);
            if (createFile)
            {
                if (!File.Exists(fullPath))
                    File.Create(fullPath).Close();
            }
            return fullPath;
        }
        #endregion
    }

}
