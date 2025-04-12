using Newtonsoft.Json;
using Streamerfy.Windows;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Windows.Media;

namespace Streamerfy.Services
{
    public class NowPlayingService
    {
        private readonly HttpListener _listener;

        public NowPlayingService()
        {
            EnsureHostsFileEntry();
            _listener = new HttpListener();
            _listener.Prefixes.Add("http://localhost:8080/nowplaying/");
            Task.Run(async () =>
            {
                await StartAsync();
            });
        }

        private async Task StartAsync()
        {
            _listener.Start();
            Console.WriteLine("[INFO] Local HTTP server started...");

            while (_listener.IsListening)
            {
                var context = await _listener.GetContextAsync();
                await ProcessRequestAsync(context);
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var response = context.Response;
            response.ContentType = "text/html";

            if (File.Exists(App.NowPlayingHTMLFile))
            {
                var htmlContent = await File.ReadAllTextAsync(App.NowPlayingHTMLFile);
                var buffer = Encoding.UTF8.GetBytes(htmlContent);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                var buffer = Encoding.UTF8.GetBytes("HTML file not found");
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }

            response.OutputStream.Close();
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
            Console.WriteLine("[Info] Local HTTP server stopped...");
        }

        private void EnsureHostsFileEntry()
        {
            const string hostsFilePath = @"C:\Windows\System32\drivers\etc\hosts";
            const string entry = "127.0.0.1 streamerfy.local";

            if (!IsAdministrator())
            {
                MainWindow.Instance?.AddLog(LanguageService.Translate("Message_NowPlaying_TikTok_Admin"), Colors.Red);
                return;
            }

            var lines = File.ReadAllLines(hostsFilePath);
            if (!lines.Any(line => line.Trim() == entry))
            {
                File.AppendAllText(hostsFilePath, Environment.NewLine + entry);
                Console.WriteLine("[INFO] Added streamerfy.local entry to hosts file.");
            }
        }

        private bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

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
                MainWindow.Instance?.AddLog(LanguageService.Translate("Message_NowPlaying_Failure", new { ERROR = ex.Message }), System.Windows.Media.Colors.Red);
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
                MainWindow.Instance?.AddLog(LanguageService.Translate("Message_NowPlaying_Clear_Failure", new { ERROR = ex.Message }), System.Windows.Media.Colors.Red);
            }
        }
    }
}
