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
        private static string _currentVersion = "0.0.0";
        private static TaskCompletionSource<bool> _readyTcs = new();

        public static LanguageViewModel ViewModel { get; } = new();
        public static bool IsInitialized { get; private set; } = false;
        public static Task WaitUntilReadyAsync() => _readyTcs.Task;

        public static async Task InitializeAsync()
        {
            Console.WriteLine("[INFO] Initializing LanguageService...");
            IsInitialized = false;

            string lang = App.Settings.Language ?? "en";
            string path = Path.Combine(App.LanguagesFolder, $"{lang}.json");

            Console.WriteLine($"[INFO] Selected language: {lang}");
            Console.WriteLine($"[INFO] Checking file at: {path}");

            bool needsDownload = !File.Exists(path) || !await IsLanguageFileUpToDate(lang, path);
            if (needsDownload)
            {
                Console.WriteLine("[INFO] Language file not found. Attempting to download...");

                bool success = await TryDownloadLanguageFile(lang);
                if (!success)
                {
                    Console.WriteLine("[WARN] Failed to download language file. Falling back to English.");

                    lang = "en";
                    App.Settings.Language = "en";
                    App.SaveAppSettings();

                    if (!await TryDownloadLanguageFile("en"))
                    {
                        Console.WriteLine("[ERROR] Failed to download English fallback. Localization disabled.");
                        return;
                    }
                }
                path = Path.Combine(App.LanguagesFolder, $"{lang}.json");
            }

            Console.WriteLine("[INFO] Loading language file from disk...");
            LoadFromFile(path);
            Console.WriteLine("[INFO] Applying translations to UI...");
            ApplyToViewModel();

            _readyTcs.TrySetResult(true);
            if (MainWindow.Instance == null)
                Console.WriteLine("MAINWINDOW:INSTANCE IS NULL!");
            MainWindow.Instance?.FlushQueuedLogs();
            Console.WriteLine("[SUCCESS] LanguageService initialized.");
            IsInitialized = true;
        }

        private static async Task<bool> IsLanguageFileUpToDate(string lang, string localPath)
        {
            try
            {
                Console.WriteLine("[INFO] Checking for language file updates...");

                var http = new HttpClient();
                var onlineJson = await http.GetStringAsync($"{GitHubRepoBase}{lang}.json");
                var onlineVersion = JsonDocument.Parse(onlineJson).RootElement.GetProperty("version").GetString();

                var localJson = await File.ReadAllTextAsync(localPath);
                var localVersion = JsonDocument.Parse(localJson).RootElement.GetProperty("version").GetString();

                Console.WriteLine($"[DEBUG] Online version: {onlineVersion}");
                Console.WriteLine($"[DEBUG] Local version:  {localVersion}");

                return localVersion == onlineVersion;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to check language version: {ex.Message}");
                return false;
            }
        }

        private static async Task<bool> TryDownloadLanguageFile(string lang)
        {
            try
            {
                var url = $"{GitHubRepoBase}{lang}.json";
                Console.WriteLine($"[INFO] Downloading language file: {url}");

                using var http = new HttpClient();
                var data = await http.GetStringAsync(url);
                var path = Path.Combine(App.LanguagesFolder, $"{lang}.json");

                await File.WriteAllTextAsync(path, data);

                Console.WriteLine("[SUCCESS] Language file downloaded.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to download '{lang}' language file: {ex.Message}");
                return false;
            }
        }

        private static void LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                Console.WriteLine($"[ERROR] Language file does not exist: {path}");
                return;
            }

            Console.WriteLine($"[INFO] Reading file: {path}");

            var json = File.ReadAllText(path);
            var doc = JsonDocument.Parse(json);

            _translations.Clear();
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Name == "version")
                {
                    _currentVersion = prop.Value.GetString() ?? "0.0.0";
                    Console.WriteLine($"[INFO] Language file version: {_currentVersion}");
                }
                else
                {
                    _translations[prop.Name] = prop.Value.GetString() ?? "";
                }
            }

            Console.WriteLine($"[INFO] Loaded {_translations.Count} translations.");
        }

        private static void ApplyToViewModel()
        {
            Console.WriteLine("[INFO] Applying translations to ViewModel...");

            foreach (var pair in _translations)
            {
                ViewModel[pair.Key] = pair.Value;
                Console.WriteLine($"[BIND] {pair.Key} = {pair.Value}");
            }

            ViewModel.ConnectButtonLabel = ViewModel.Button_Connect;
        }

        public static string Translate(string key, object? placeholders = null)
        {
            Console.WriteLine($"[LanguageService] Translate() called with key = '{key}'");

            if (!_translations.TryGetValue(key, out var template))
            {
                Console.WriteLine($"[LanguageService] ❌ Key '{key}' not found in _translations.");
                return $"[{key}]";
            }

            Console.WriteLine($"[LanguageService] ✅ Found key. Template = \"{template}\"");

            if (placeholders == null)
            {
                Console.WriteLine("[LanguageService] No placeholders provided. Returning template as-is.");
                return template;
            }

            var result = template;
            var props = placeholders.GetType().GetProperties();

            foreach (var prop in props)
            {
                var placeholder = $"{{{prop.Name}}}";
                var value = prop.GetValue(placeholders)?.ToString() ?? "";
                Console.WriteLine($"[LanguageService] Replacing {placeholder} with '{value}'");
                result = result.Replace(placeholder, value);
            }

            Console.WriteLine($"[LanguageService] Final result: \"{result}\"");
            return result;
        }
    }
}
