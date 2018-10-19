using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using GitHub.Logging;

namespace GitHub.Unity
{
    public interface IOAuthCallbackListener
    {
        void Listen(string state, CancellationToken cancel, Action<string> codeCallback);
    }

    public class OAuthCallbackListener : IOAuthCallbackListener
    {
        private static readonly ILogging logger = LogHelper.GetLogger<OAuthCallbackListener>();

        const int CallbackPort = 42424;

        static readonly string CallbackUrl = $"http://localhost:{CallbackPort}/";
        private string state;
        private CancellationToken cancel;
        private Action<string> codeCallback;
        private HttpListener httpListener;

        public void Listen(string state, CancellationToken cancel, Action<string> codeCallback)
        {
            this.state = state;
            this.cancel = cancel;
            this.codeCallback = codeCallback;

            httpListener = new HttpListener();
            httpListener.Prefixes.Add(CallbackUrl);
            httpListener.Start();
            Task.Factory.StartNew(Start, cancel);
        }

        private void Start()
        {
            try
            {
                using (httpListener)
                {
                    using (cancel.Register(httpListener.Stop))
                    {
                        while (true)
                        {
                            var context = httpListener.GetContext();
                            var queryParts = HttpUtility.ParseQueryString(context.Request.Url.Query);

                            if (queryParts["state"] == state)
                            {
                                context.Response.Close();
                                codeCallback(queryParts["code"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warning(ex, "OAuthCallbackListener Error");
            }
            finally
            {
                httpListener = null;
            }
        }
    }
}
