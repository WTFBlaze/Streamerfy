using Newtonsoft.Json;
using System.IO;
using System.Text.Json;

namespace Streamerfy.Services
{
    public class NowPlayingService
    {
        public void Update(string title, string artist, string coverUrl, bool isPlaying)
        {
            try
            {
                var data = new
                {
                    title,
                    artist,
                    coverUrl,
                    isPlaying
                };

                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(App.NowPlayingJSONFile, json);
                File.WriteAllText(App.NowPlayingTXTFile, $"{title} - {artist}");
            }
            catch (Exception ex)
            {
                MainWindow.Instance?.AddLog($"❌ Failed to write NowPlaying.json: {ex.Message}", System.Windows.Media.Colors.Red);
            }
        }


        public void Clear()
        {
            try
            {
                if (File.Exists(App.NowPlayingJSONFile))
                    File.WriteAllText(App.NowPlayingJSONFile, "{}");

                if (File.Exists(App.NowPlayingTXTFile))
                    File.WriteAllText(App.NowPlayingTXTFile, string.Empty);
            }
            catch (Exception ex)
            {
                MainWindow.Instance?.AddLog($"❌ Failed to clear NowPlaying.json: {ex.Message}", System.Windows.Media.Colors.Red);
            }
        }
    }
}
