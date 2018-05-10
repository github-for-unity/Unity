using GitHub.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace TestWebServer
{
    public class HttpServer
    {
        private static readonly IDictionary<string, string> mimeTypeMappings =
            new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase) {
                { ".gif", "image/gif" },
                { ".html", "text/html" },
                { ".jpg", "image/jpeg" },
                { ".png", "image/png" },
                { ".txt", "text/plain" },
                { ".md5", "text/plain" },
                { ".zip", "application/zip" }
            };
        private readonly HttpListener listener;
        private readonly string rootDirectory;
        private bool abort;
        private static ILogging Logger = LogHelper.GetLogger<HttpServer>();
        private ManualResetEvent delay = new ManualResetEvent(false);

        /// <summary>
        ///     Construct server with given port.
        /// </summary>
        /// <param name="path">Directory path to serve.</param>
        /// <param name="port">Port of the server.</param>
        public HttpServer(string path = null, int port = 0)
        {
            if (String.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                path = Path.Combine(Path.GetDirectoryName(GetType().Assembly.Location), "files");
            }

            rootDirectory = path;

            if (port == 0)
            {
                //get an empty port
                var l = new TcpListener(IPAddress.Loopback, 0);
                l.Start();
                port = ((IPEndPoint)l.LocalEndpoint).Port;
                l.Stop();
            }
            Port = port;

            listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:" + port + "/");
        }

        /// <summary>
        ///     Stop server and dispose all functions.
        /// </summary>
        public void Stop()
        {
            listener.Stop();
        }

        public void Start()
        {
            try
            {
                Logger.Info($"Starting http server on port {Port} serving from {rootDirectory}");
                listener.Start();
                while (true)
                {
                    try
                    {
                        abort = false;
                        Logger.Info($"Waiting for a request...");
                        var context = listener.GetContext();
                        var thread = new Thread(p => Process((HttpListenerContext)p));
                        thread.Start(context);
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void Abort()
        {
            abort = true;
            delay.Set();
        }

        private void Process(HttpListenerContext context)
        {
            Logger.Info($"Handling request");

            var filename = context.Request.Url.AbsolutePath;
            Logger.Info($"{filename}");
            filename = filename.TrimStart('/');
            filename = filename.Replace('/', '\\');
            filename = Path.Combine(rootDirectory, filename);

            if (!File.Exists(filename))
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.Close();
                return;
            }

            try
            {
                string mime;
                context.Response.ContentType = mimeTypeMappings.TryGetValue(Path.GetExtension(filename), out mime)
                    ? mime
                    : "application/octet-stream";

                context.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                context.Response.AddHeader("Last-Modified", File.GetLastWriteTime(filename).ToString("r"));

                using (var input = new FileStream(filename, FileMode.Open))
                {
                    var length = input.Length;
                    var range = context.Request.Headers["Range"];
                    if (range == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.OK;
                    }
                    else
                    {
                        var parts = range.Split('-');
                        var start = long.Parse(parts[0].Substring("bytes=".Length));
                        var endRange = parts[1];
                        long end = 0;
                        if (!string.IsNullOrEmpty(endRange))
                        {
                            end = long.Parse(endRange);
                        }
                        else
                        {
                            end = length - 1;
                        }

                        length = end - start + 1;

                        if (input.CanSeek && (input.Length > start) && (end <= input.Length))
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.PartialContent;
                            context.Response.Headers.Add("Content-Range", $"{start}-{end}/{input.Length}");
                            input.Seek(start, SeekOrigin.Current);
                        }
                        else
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.RequestedRangeNotSatisfiable;
                        }
                    }

                    if (context.Response.StatusCode != (int)HttpStatusCode.RequestedRangeNotSatisfiable)
                    {
                        context.Response.ContentLength64 = length;

                        Logger.Info($"Writing {length} bytes");

                        delay.Reset();
                        Utils.Copy(input, context.Response.OutputStream, length,
                            progress: (total, __) =>
                            {
                                if (Delay > 0)
                                    delay.WaitOne(Delay);
                                if (abort)
                                    Logger.Info($"aborting after {total} bytes");
                                return !abort;
                            },
                            progressUpdateRate: 0
                            );
                        context.Response.OutputStream.Flush();
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.GetLogger<HttpServer>().Error(ex);
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            finally
            {
                try
                {
                    context.Response.Close();
                }
                catch { }
            }
        }

        public int Delay { get; set; }

        public int Port { get; }
    }

    static class Utils
    {
        public static bool Copy(Stream source, Stream destination, long totalSize = 0, int chunkSize = 8192,
            Func<long, long, bool> progress = null, int progressUpdateRate = 100)
        {
            var buffer = new byte[chunkSize];
            var bytesRead = 0;
            long totalRead = 0;
            var averageSpeed = -1f;
            var lastSpeed = 0f;
            var smoothing = 0.005f;
            long readLastSecond = 0;
            long timeToFinish = 0;
            Stopwatch watch = null;
            var success = true;

            var trackProgress = (totalSize > 0) && (progress != null);
            if (trackProgress)
            {
                watch = new Stopwatch();
            }

            do
            {
                if (trackProgress)
                {
                    watch.Start();
                }

                bytesRead = source.Read(buffer, 0,
                    totalRead + chunkSize > totalSize ? (int)(totalSize - totalRead) : chunkSize);

                if (trackProgress)
                {
                    watch.Stop();
                }

                totalRead += bytesRead;

                if (bytesRead > 0)
                {
                    destination.Write(buffer, 0, bytesRead);
                    if (trackProgress)
                    {
                        readLastSecond += bytesRead;
                        if ((watch.ElapsedMilliseconds >= progressUpdateRate) || (totalRead == totalSize) ||
                            (bytesRead == 0))
                        {
                            watch.Reset();
                            if (bytesRead == 0) // we've reached the end
                            {
                                totalSize = totalRead;
                            }

                            lastSpeed = readLastSecond;
                            readLastSecond = 0;
                            averageSpeed = averageSpeed < 0f
                                ? lastSpeed
                                : smoothing * lastSpeed + (1f - smoothing) * averageSpeed;
                            timeToFinish = Math.Max(1L,
                                (long)((totalSize - totalRead) / (averageSpeed / progressUpdateRate)));

                            success = progress(totalRead, timeToFinish);
                            if (!success)
                            {
                                break;
                            }
                        }
                    }
                    else // we still need to call the callback if it's there, so we can abort if needed
                    {
                        success = progress?.Invoke(totalRead, timeToFinish) ?? true;
                        if (!success)
                        {
                            break;
                        }
                    }
                }
            } while ((bytesRead > 0) && ((totalSize == 0) || (totalSize > totalRead)));

            if (totalRead > 0)
            {
                destination.Flush();
            }

            return success;
        }
    }
}
