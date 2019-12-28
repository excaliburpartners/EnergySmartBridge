using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Reflection;
using log4net;
using Newtonsoft.Json;
using EnergySmartBridge.WebService;
using System.Web;
using System.Collections.Specialized;

namespace EnergySmartBridge.Modules
{
    public class WebServerModule : IModule
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private readonly HttpListener _listener = new HttpListener();
        private readonly Dictionary<string, Func<HttpListenerRequest, object>> prefixmap;

        public WebServerModule()
        {
            if (!HttpListener.IsSupported)
                throw new NotSupportedException(
                    "Needs Windows XP SP2, Server 2003 or later.");

            prefixmap = new Dictionary<string, Func<HttpListenerRequest, object>>(StringComparer.InvariantCultureIgnoreCase);

            try
            {
                _listener.Start();
            }
            catch (Exception ex)
            {
                log.Error("WebServer", ex);
            }
        }

        public void Startup()
        {
            string uri = "http://*:" + Global.webserver_port + "/";

            try
            { 
                _listener.Prefixes.Add(uri);

                log.Info("Listening on " + uri);
            }
            catch (Exception ex)
            {
                log.Error("WebServer", ex);
            }

            ThreadPool.QueueUserWorkItem((o) =>
            {
                try
                {
                    while (_listener.IsListening)
                    {
                        ThreadPool.QueueUserWorkItem((c) =>
                        {
                            var ctx = c as HttpListenerContext;
                            try
                            {
                                string rstr = SendResponse(ctx.Request);
                                byte[] buf = Encoding.UTF8.GetBytes(rstr);
                                ctx.Response.ContentLength64 = buf.Length;
                                ctx.Response.OutputStream.Write(buf, 0, buf.Length);
                            }
                            catch
                            {
                                // suppress any exceptions
                            }
                            finally
                            {
                                // always close the stream
                                ctx.Response.OutputStream.Close();
                            }
                        }, _listener.GetContext());
                    }
                }
                catch (HttpListenerException ex)
                {
                    // Ignore ERROR_OPERATION_ABORTED, which occurs on shutdown
                    if(ex.ErrorCode != 995)
                        log.Error("WebServer", ex);
                }
                catch (Exception ex)
                {
                    log.Error("WebServer", ex);
                }

                log.Debug("Shutdown complete");
            });
        }

        public void Shutdown()
        {
            _listener.Stop();
            _listener.Close();
        }

        /// <summary>
        /// Register a function to call when a URI is visited, for example "/index/"
        /// </summary>
        public void RegisterPrefix(Func<HttpListenerRequest, object> method, params string[] prefixes)
        {
            foreach (string prefix in prefixes)
            {
                prefixmap.Add(prefix, method);
            }
        }

        private string SendResponse(HttpListenerRequest request)
        {
            try
            {
                if (prefixmap.ContainsKey(request.RawUrl))
                {
                    return JsonConvert.SerializeObject(prefixmap[request.RawUrl](request));
                }
                else
                {
                    string content = new System.IO.StreamReader(request.InputStream).ReadToEnd();
                    log.Warn($"URL not found: {request.Url.ToString()}\n{content}");
                    return JsonConvert.SerializeObject("notfound");
                }
            }
            catch (Exception ex)
            {
                log.Error("SendResponse", ex);
                return JsonConvert.SerializeObject("error");
            }
        }
    }
}