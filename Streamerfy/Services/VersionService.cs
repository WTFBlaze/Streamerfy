using System.Net.Http;
using System.Text.Json;

namespace Streamerfy.Services
{
    public static class VersionService
    {
        private const string LatestReleaseApi = "https://api.github.com/repos/WTFBlaze/Streamerfy/releases/latest";
        public const string CurrentVersion = "1.0.7";

        public static async Task<(bool isUpdateAvailable, string latestVersion)> CheckForUpdatesAsync()
        {
            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.UserAgent.ParseAdd($"Streamerfy/{CurrentVersion}");

                var response = await http.GetStringAsync(LatestReleaseApi);
                using var doc = JsonDocument.Parse(response);

                var latestTag = doc.RootElement.GetProperty("tag_name").GetString();

                if (!string.IsNullOrWhiteSpace(latestTag) && latestTag != CurrentVersion)
                {
                    return (true, latestTag);
                }
            }
            catch
            {
                // optional: log or ignore
            }

            return (false, "");
        }
    }
}
