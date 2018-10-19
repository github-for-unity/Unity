using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using GitHub.Logging;

namespace GitHub.Unity
{
    public interface IOAuthCallbackManager
    {
        event Action<string, string> OnCallback;
        bool IsRunning { get; }
        void Start();
        void Stop();
    }

    public class OAuthCallbackManager : IOAuthCallbackManager
    {
        const int CallbackPort = 42424;
        public static readonly Uri CallbackUrl = new Uri($"http://localhost:{CallbackPort}/callback");

        private static readonly ILogging logger = LogHelper.GetLogger<OAuthCallbackManager>();
        private static readonly object _lock = new object();


        private readonly CancellationTokenSource cancelSource;

        private HttpListener httpListener;
        public bool IsRunning { get; private set; }

        public event Action<string, string> OnCallback;

        public OAuthCallbackManager()
        {
            cancelSource = new CancellationTokenSource();
        }

        public void Start()
        {
            if (!IsRunning)
            {
                lock(_lock)
                {
                    if (!IsRunning)
                    {
                        logger.Trace("Starting");

                        httpListener = new HttpListener();
                        httpListener.Prefixes.Add(CallbackUrl.AbsoluteUri + "/");
                        httpListener.Start();
                        Task.Factory.StartNew(Listen, cancelSource.Token);
                        IsRunning = true;
                    }
                }
            }
        }

        public void Stop()
        {
            logger.Trace("Stopping");
            cancelSource.Cancel();
        }

        private void Listen()
        {
            try
            {
                using (httpListener)
                {
                    using (cancelSource.Token.Register(httpListener.Stop))
                    {
                        while (true)
                        {
                            var context = httpListener.GetContext();
                            var queryParts = HttpUtility.ParseQueryString(context.Request.Url.Query);

                            var state = queryParts["state"];
                            var code = queryParts["code"];

                            logger.Trace("OnCallback: {0}", state);
                            if (OnCallback != null)
                            {
                                OnCallback(state, code);
                            }

                            context.Response.StatusCode = 200;
                            context.Response.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Trace(ex.Message);
            }
            finally
            {
                IsRunning = false;
                httpListener = null;
            }
        }
    }
}
