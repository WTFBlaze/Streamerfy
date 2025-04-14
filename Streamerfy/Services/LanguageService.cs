using Streamerfy.Data.ViewModels;
using Streamerfy.Windows;
using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace Streamerfy.Services
{
    public static class LanguageService
    {
        private const string GitHubRepoBase = "https://raw.githubusercontent.com/WTFBlaze/Streamerfy/master/Languages/";

        private static Dictionary<string, string> _translations = new();
        private static TaskCompletionSource<bool> _readyTcs = new();

        public static LanguageViewModel ViewModel { get; } = new();
        public static bool IsInitialized { get; private set; } = false;
        public static Task WaitUntilReadyAsync() => _readyTcs.Task;

        public static async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                IsInitialized = false;
                _readyTcs.TrySetResult(false);
                LoggingService.AddLog("Re-initializing LanguageService...", ConsoleColor.Yellow);
            }
            else LoggingService.AddLog("Initializing LanguageService...", ConsoleColor.Yellow);

            string lang = App.Settings.Language ?? "en";
            string path = Path.Combine(App.LanguagesFolder, $"{lang}.json");

            LoggingService.AddLog($"Selected language: {lang}", ConsoleColor.DarkYellow);
            LoggingService.AddLog($"Checking file at: {path}", ConsoleColor.DarkYellow);

            bool needsDownload = !File.Exists(path) || !await IsLanguageFileUpToDate(lang, path);
            if (needsDownload)
            {
                LoggingService.AddLog("Language file not found. Attempting to download...", ConsoleColor.Cyan);

                bool success = await TryDownloadLanguageFile(lang);
                if (!success)
                {
                    LoggingService.AddLog("Failed to download language file. Falling back to English.", ConsoleColor.Yellow);

                    lang = "en";
                    App.Settings.Language = "en";
                    App.SaveAppSettings();

                    if (!await TryDownloadLanguageFile("en"))
                    {
                        LoggingService.AddLog("Failed to download English fallback. Localization disabled.", ConsoleColor.Red);
                        return;
                    }
                }
                path = Path.Combine(App.LanguagesFolder, $"{lang}.json");
            }

            LoggingService.AddLog("Loading language file from disk...", ConsoleColor.Yellow);
            LoadFromFile(path);
            LoggingService.AddLog("Applying translations to UI...", ConsoleColor.Cyan);
            ApplyToViewModel();

            _readyTcs.TrySetResult(true);
            MainWindow.Instance?.FlushQueuedLogs();
            LoggingService.AddLog("LanguageService Initialized.", ConsoleColor.Green);
            IsInitialized = true;
        }

        public static async Task<List<string>> FetchAvailableLanguagesAsync()
        {
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd("Streamerfy");

                var url = "https://api.github.com/repos/WTFBlaze/Streamerfy/contents/Languages";
                var response = await http.GetStringAsync(url);

                var files = JsonDocument.Parse(response).RootElement;
                var langs = new List<string>();

                foreach (var file in files.EnumerateArray())
                {
                    var name = file.GetProperty("name").GetString();
                    if (name != null && name.EndsWith(".json"))
                    {
                        langs.Add(Path.GetFileNameWithoutExtension(name));
                    }
                }

                return langs;
            }
            catch (Exception ex)
            {
                LoggingService.AddLog($"Failed to fetch language list: {ex.Message}", ConsoleColor.Red);
                return new List<string> { "en" };
            }
        }


        private static async Task<bool> IsLanguageFileUpToDate(string lang, string localPath)
        {
            try
            {
                LoggingService.AddLog("Checking for language file updates...", ConsoleColor.Yellow);

                var http = new HttpClient();
                var onlineJson = await http.GetStringAsync($"{GitHubRepoBase}{lang}.json");
                var onlineVersion = JsonDocument.Parse(onlineJson).RootElement.GetProperty("version").GetString();

                var localJson = await File.ReadAllTextAsync(localPath);
                var localVersion = JsonDocument.Parse(localJson).RootElement.GetProperty("version").GetString();

                LoggingService.AddLog($"Online version: {onlineVersion}", ConsoleColor.Cyan);
                LoggingService.AddLog($"Local version: {localVersion}", ConsoleColor.DarkCyan);

                return localVersion == onlineVersion;
            }
            catch (Exception ex)
            {
                LoggingService.AddLog($"Failed to check language version: {ex.Message}", ConsoleColor.Red);
                return false;
            }
        }

        private static async Task<bool> TryDownloadLanguageFile(string lang)
        {
            try
            {
                var url = $"{GitHubRepoBase}{lang}.json";
                LoggingService.AddLog($"Downloading language file: {url}", ConsoleColor.Yellow);

                using var http = new HttpClient();
                var data = await http.GetStringAsync(url);
                var path = Path.Combine(App.LanguagesFolder, $"{lang}.json");

                await File.WriteAllTextAsync(path, data);

                LoggingService.AddLog("Language file downloaded.", ConsoleColor.Green);
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.AddLog($"Failed to download '{lang}' language file: {ex.Message}", ConsoleColor.Red);
                return false;
            }
        }

        private static void LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                LoggingService.AddLog($"Language file does not exist: {path}", ConsoleColor.Red);
                return;
            }

            LoggingService.AddLog($"Reading file: {path}", ConsoleColor.Cyan);

            var json = File.ReadAllText(path);
            var doc = JsonDocument.Parse(json);

            _translations.Clear();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                switch (prop.Name)
                {
                    case "language":
                        {
                            var language = prop.Value.GetString() ?? "Unknown";
                            LoggingService.AddLog($"Language file name: {language}", ConsoleColor.Magenta);
                        }
                        break;

                    case "version":
                        {
                            var currentVersion = prop.Value.GetString() ?? "0.0.0";
                            LoggingService.AddLog($"Language file version: {currentVersion}", ConsoleColor.Magenta);
                        }
                        break;

                    case "authors":
                        {
                            var authors = prop.Value.GetString() ?? "Unknown";
                            LoggingService.AddLog($"Language file author(s): {authors}", ConsoleColor.Magenta);
                        }
                        break;

                    default:
                        _translations[prop.Name] = prop.Value.GetString() ?? "";
                        break;
                }
            }

            LoggingService.AddLog($"Loaded {_translations.Count} translations.", ConsoleColor.Green);
        }

        private static void ApplyToViewModel()
        {
            LoggingService.AddLog("Applying translations to ViewModel...", ConsoleColor.Yellow);

            foreach (var pair in _translations)
            {
                ViewModel[pair.Key] = pair.Value;
            }

            ViewModel.ConnectButtonLabel = ViewModel.Button_Connect;
            LoggingService.AddLog("Applied translations to ViewModel.", ConsoleColor.Green);
        }

        public static string Translate(string key, object? placeholders = null)
        {
            //Console.WriteLine($"[LanguageService] Translate() called with key = '{key}'");

            if (!_translations.TryGetValue(key, out var template))
            {
                //Console.WriteLine($"[LanguageService] ❌ Key '{key}' not found in _translations.");
                return $"[{key}]";
            }

            //Console.WriteLine($"[LanguageService] ✅ Found key. Template = \"{template}\"");

            if (placeholders == null)
            {
                //Console.WriteLine("[LanguageService] No placeholders provided. Returning template as-is.");
                return template;
            }

            var result = template;
            var props = placeholders.GetType().GetProperties();

            foreach (var prop in props)
            {
                var placeholder = $"{{{prop.Name}}}";
                var value = prop.GetValue(placeholders)?.ToString() ?? "";
                //Console.WriteLine($"[LanguageService] Replacing {placeholder} with '{value}'");
                result = result.Replace(placeholder, value);
            }

            //Console.WriteLine($"[LanguageService] Final result: \"{result}\"");
            return result;
        }
    }
}
