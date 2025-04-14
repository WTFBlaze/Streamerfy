using EmbedIO.Actions;
using EmbedIO;
using SpotifyAPI.Web.Auth;
using System.Reflection;
using System.Text;
using System.Collections.Specialized;

namespace Streamerfy.Data.EmbedIO
{
    /*
     
        Credits for this class go to the folks over at SpotifyAPI-NET. I simply imported their class directly so that I can truly modify the HTML Callback files including the assets.
        Link: https://github.com/JohnnyCrazy/SpotifyAPI-NET

     */
    public class AuthServer : IAuthServer, IDisposable
    {
        private const string AssetsResourcePath = "Streamerfy.Assets.SpotifyAuth.auth_assets";

        private const string DefaultResourcePath = "Streamerfy.Assets.SpotifyAuth.default_site";

        private CancellationTokenSource? _cancelTokenSource;

        private readonly WebServer _webServer;

        public Uri BaseUri { get; }

        public int Port { get; }

        public event Func<object, AuthorizationCodeResponse, Task>? AuthorizationCodeReceived;

        public event Func<object, ImplictGrantResponse, Task>? ImplictGrantReceived;

        public event Func<object, string, string?, Task>? ErrorReceived;

        public AuthServer(Uri baseUri, int port)
            : this(baseUri, port, Assembly.GetExecutingAssembly(), DefaultResourcePath)
        {
        }

        public AuthServer(Uri baseUri, int port, Assembly resourceAssembly, string resourcePath)
        {
            ArgumentNotNull(baseUri, "baseUri");
            BaseUri = baseUri;
            Port = port;
            _webServer = new WebServer(port).WithModule(new ActionModule("/", HttpVerbs.Post, delegate (IHttpContext ctx)
            {
                NameValueCollection queryString = ctx.Request.QueryString;
                string text = queryString["error"];
                if (text != null)
                {
                    this.ErrorReceived?.Invoke(this, text, queryString["state"]);
                    throw new AuthException(text, queryString["state"]);
                }

                string? text2 = queryString.Get("request_type");
                if (text2 == "token")
                {
                    this.ImplictGrantReceived?.Invoke(this, new ImplictGrantResponse(queryString["access_token"], queryString["token_type"], int.Parse(queryString["expires_in"]))
                    {
                        State = queryString["state"]
                    });
                }

                if (text2 == "code")
                {
                    this.AuthorizationCodeReceived?.Invoke(this, new AuthorizationCodeResponse(queryString["code"])
                    {
                        State = queryString["state"]
                    });
                }

                return ctx.SendStringAsync("OK", "text/plain", Encoding.UTF8);
            })).WithEmbeddedResources("/auth_assets", Assembly.GetExecutingAssembly(), AssetsResourcePath).WithEmbeddedResources(baseUri.AbsolutePath, resourceAssembly, resourcePath);
        }

        public Task Start()
        {
            _cancelTokenSource = new CancellationTokenSource();
            _webServer.Start(_cancelTokenSource.Token);
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            _cancelTokenSource?.Cancel();
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _webServer?.Dispose();
            }
        }

        private void ArgumentNotNull(object value, string name)
        {
            if (value != null)
            {
                return;
            }

            throw new ArgumentNullException(name);
        }
    }
}
