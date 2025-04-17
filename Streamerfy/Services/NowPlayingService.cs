using Newtonsoft.Json;
using Streamerfy.Windows;
using System.IO;
using System.Net;
using System.Net.WebSockets;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using System.Windows.Media;

namespace Streamerfy.Services
{
    public class NowPlayingService
    {
        private readonly HttpListener _listener;
        private string _currentSongInfo = "{}";

        public NowPlayingService()
        {
            LoggingService.AddLog("NowPlayingService constructor started", ConsoleColor.Yellow);

            Task.Run(async () =>
            {
                LoggingService.AddLog("Ensuring hosts file entry...", ConsoleColor.Gray);
                try
                {
                    await EnsureHostsFileEntry();
                    LoggingService.AddLog("Hosts file check complete.", ConsoleColor.Gray);
                }
                catch (Exception ex)
                {
                    LoggingService.AddLog("EnsureHostsFileEntry FAILED: " + ex.Message, ConsoleColor.Red);
                }
            });

            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add("http://127.0.0.1:8081/nowplaying/");
                LoggingService.AddLog("HttpListener setup complete.", ConsoleColor.Gray);
            }
            catch (Exception ex)
            {
                LoggingService.AddLog("HttpListener setup FAILED: " + ex.Message, ConsoleColor.Red);
            }

            Task.Run(async () =>
            {
                LoggingService.AddLog("Starting HTTP listener task...", ConsoleColor.Gray);
                try
                {
                    await StartAsync();
                }
                catch (Exception ex)
                {
                    LoggingService.AddLog("StartAsync FAILED: " + ex.Message, ConsoleColor.Red);
                }
            });
        }


        private async Task StartAsync()
        {
            _listener.Start();
            LoggingService.AddLog("Local HTTP server started.", ConsoleColor.Cyan);

            while (_listener.IsListening)
            {
                var context = await _listener.GetContextAsync();
                await ProcessRequestAsync(context);
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var response = context.Response;

            if (context.Request.Url.AbsolutePath == "/nowplaying/api/" || context.Request.Url.AbsolutePath == "/nowplaying/api")
            {
                response.ContentType = "application/json";
                var buffer = Encoding.UTF8.GetBytes(_currentSongInfo);
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            else if (context.Request.Url.AbsolutePath == "/nowplaying/" || context.Request.Url.AbsolutePath == "/nowplaying")
            {
                response.ContentType = "text/html";
                if (File.Exists(App.NowPlayingServerHTMLFile))
                {
                    var htmlContent = await File.ReadAllTextAsync(App.NowPlayingServerHTMLFile);
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
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                var buffer = Encoding.UTF8.GetBytes("Endpoint not found");
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }

            response.OutputStream.Close();
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
            LoggingService.AddLog("Local HTTP server stopped.", ConsoleColor.DarkCyan);
        }

        private async Task EnsureHostsFileEntry()
        {
            const string hostsFilePath = @"C:\Windows\System32\drivers\etc\hosts";
            const string entry = "127.0.0.1 streamerfy.local";

            var lines = File.ReadAllLines(hostsFilePath);
            if (lines.Any(line => line.Trim() == entry))
            {
                // Entry already exists, no need to warn or modify
                return;
            }

            if (!IsAdministrator())
            {
                await LanguageService.WaitUntilReadyAsync();
                MainWindow.Instance.AddLog(LanguageService.Translate("Message_NowPlaying_TikTok_Admin"), Colors.Red);
                return;
            }

            File.AppendAllText(hostsFilePath, Environment.NewLine + entry);
            LoggingService.AddLog("Added streamerfy.local entry to hosts file.", ConsoleColor.Green);
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
                _currentSongInfo = json;
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
                _currentSongInfo = "{}";
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
