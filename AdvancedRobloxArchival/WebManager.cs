using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace AdvancedRobloxArchival
{
    internal class WebManager
    {
        private static HttpListener listener;
        private const int Port = 5776;
        public static string HostName => $"http://127.0.0.1:{Port}";

        private static Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, Task>> pageRegisters = new Dictionary<string, Func<HttpListenerRequest, HttpListenerResponse, Task>>
        {
            {"/", Page_Home },
            {"/css/bootstrap.css", (req, res) => ReturnData(req, res, Encoding.UTF8.GetBytes(Properties.Resources.BootstrapCSS), ContentType.CSS) },
            {"/js/bootstrap.js", (req, res) => ReturnData(req, res, Encoding.UTF8.GetBytes(Properties.Resources.BootstrapJS), ContentType.JavaScript) },
            {"/js/jquery.js", (req, res) => ReturnData(req, res, Encoding.UTF8.GetBytes(Properties.Resources.JQueryJS), ContentType.JavaScript) }
        };

        private static Dictionary<string, string> siteVariables = new Dictionary<string, string>
        {
            ["revision"] = Program.versionString
        };

        private static string HomeHTML
        {
            get {
                string homeHtml = Properties.Resources.HomeHTML;

                foreach (var variable in siteVariables)
                {
                    homeHtml = homeHtml.Replace($"{{{variable.Key}}}", variable.Value);
                }

                return homeHtml;
            }
        }

        public static void Start()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(HostName + "/");
            listener.Start();

            Task processingTask = ProcessRequestsAsync();
        }

        public static void Stop() => listener.Stop();

        private static async Task ProcessRequestsAsync()
        {
            while (listener.IsListening)
            {
                var context = await listener.GetContextAsync();
                await HandleRequestAsync(context);
            }
        }

        private enum ContentType
        {
            [StringValue("application/javascript")]
            JavaScript,

            [StringValue("text/css")]
            CSS
        }

        private class StringValueAttribute : Attribute
        {
            public string Value { get; }

            public StringValueAttribute(string value)
            {
                Value = value;
            }
        }

        private static string GetContentType(ContentType contentType)
        {
            var attribute = contentType
                .GetType()
                .GetMember(contentType.ToString())
                .FirstOrDefault()?
                .GetCustomAttributes(typeof(StringValueAttribute), false)
                .OfType<StringValueAttribute>()
                .FirstOrDefault();

            return attribute?.Value;
        }

        private static async Task HandleError(HttpListenerRequest req, HttpListenerResponse res, string errString, int errCode = 500)
        {
            res.StatusCode = errCode;
            var buffer = Encoding.UTF8.GetBytes($"<html><head><title>{errString}</title></head><body><center><h1>{errString}</h1></center><hr><center>Archiver r{Program.versionString}</center></body></html>");
            await res.OutputStream.WriteAsync(buffer, 0, buffer.Length);

        }

        private static async Task ReturnFile(HttpListenerRequest req, HttpListenerResponse res, string path) => await ReturnData(req, res, File.ReadAllBytes(path), MimeMapping.GetMimeMapping(path));
        private static async Task ReturnData(HttpListenerRequest req, HttpListenerResponse res, byte[] data, ContentType ContentType) => await ReturnData(req, res, data, GetContentType(ContentType));
        private static async Task ReturnData(HttpListenerRequest req, HttpListenerResponse res, byte[] data, string ContentType = "application/octet-stream")
        {
            res.ContentType = ContentType;
            await res.OutputStream.WriteAsync(data, 0, data.Length);
        }

        private static async Task HandleRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            response.ContentType = "text/html; charset=utf-8";

            string requestedPath = request.Url.AbsolutePath;
            if (pageRegisters.ContainsKey(requestedPath))
            {
                Func<HttpListenerRequest, HttpListenerResponse, Task> action = pageRegisters[requestedPath];
                await action.Invoke(request, response);
            }
            else await HandleError(request, response, "404 - Not Found", 404);

            response.Close();
        }

        private static async Task Page_Home(HttpListenerRequest req, HttpListenerResponse res)
        {
            var buffer = Encoding.UTF8.GetBytes(HomeHTML);
            await res.OutputStream.WriteAsync(buffer, 0, buffer.Length);
        }
    }
}
