using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedRobloxArchival
{
    internal class WebManager
    {
        private static HttpListener listener;
        private const int Port = 5776;
        public static string HostName { get { return $"http://127.0.0.1:{Port}/"; } }

        public static void Start()
        {
            listener = new HttpListener();
            listener.Prefixes.Add(HostName);
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

        private static string ResponseHtml
        {
            get
            {
                return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta http-equiv=""X-UA-Compatible"" content=""IE=edge"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Web Server</title>
</head>
<body>
    todo
</body>
</html>";
            }
        }

        private static async Task HandleRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            response.ContentType = "text/html; charset=utf-8";

            var buffer = System.Text.Encoding.UTF8.GetBytes(ResponseHtml);
            await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);

            response.Close();
        }
    }
}
