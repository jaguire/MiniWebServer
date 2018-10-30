using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Nano.Web.Core;
using Nano.Web.Core.Host.HttpListener;
using Serilog;
using Serilog.Events;

namespace MiniWebServer
{
    public static class Program
    {
        private static readonly Configuration Config = Configuration.Create();

        private static void Main()
        {
            OutputTitle();
            InitLogger();
            var exit = CreateExitEvent();

            var baseUrl = $"http://{Config.Domain}:{Config.Port}";
            var nano = new NanoConfiguration
            {
                ApplicationName = "MiniWebServer",
                EnableVerboseErrors = true
            };

            // logging
            nano.GlobalEventHandler.PostInvokeHandlers.Add(context =>
            {
                var level = context.Response.HttpStatusCode == 200
                    ? LogEventLevel.Information
                    : LogEventLevel.Warning;
                var address = context.Request.Url.ToString().Replace(baseUrl, "/").Replace("//", "/");
                var statusName = Enum.GetName(typeof(Constants.HttpStatusCode), context.Response.HttpStatusCode);
                Log.Write(level, "{address} => {HttpStatusCode} {statusName}", address, context.Response.HttpStatusCode, statusName);
            });
            nano.GlobalEventHandler.UnhandledExceptionHandlers.Add((exception, context) =>
            {
                var address = context.Request.Url.ToString().Replace(baseUrl, "/").Replace("//", "/");
                Log.Error(exception, "{address} => Exception: {Message}", address, exception.Message);
            });

            // pulse
            var startTime = DateTime.Now;
            nano.AddBackgroundTask("Uptime", (int)TimeSpan.FromMinutes(1).TotalMilliseconds, () =>
            {
                var uptime = DateTime.Now - startTime;
                Log.Information("Uptime {uptime}", uptime);
                return uptime;
            });

            // hosting
            HttpHost.Init(nano, Config.WebRoot);
            ApiHost.Init(nano, Config.WebRoot);
            nano.DisableCorrelationId();
            nano.EnableCors();

            // start server
            using (var server = HttpListenerNanoServer.Start(nano, baseUrl))
            {
                Log.Information("Listening on {url}", baseUrl);
                Log.Information("Press Ctrl+C to exit.");
                Process.Start(File.Exists($"{Environment.CurrentDirectory}\\index.html") ? baseUrl : $"{baseUrl}/ApiExplorer");
                exit.WaitOne();
            }
        }

        private static ManualResetEvent CreateExitEvent()
        {
            var exit = new ManualResetEvent(false);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Log.Information("Shutting down.");
                eventArgs.Cancel = true;
                exit.Set();
            };
            return exit;
        }

        private static void InitLogger()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.LiterateConsole()
                .CreateLogger();
        }

        private static void OutputTitle()
        {
            Console.Clear();
            var startColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(@"   __  ____      _ _      __    __   ____
  /  |/  (_)__  (_) | /| / /__ / /  / __/__ _____  _____ ____
 / /|_/ / / _ \/ /| |/ |/ / -_) _ \_\ \/ -_) __/ |/ / -_) __/
/_/  /_/_/_//_/_/ |__/|__/\__/_.__/___/\__/_/  |___/\__/_/
");
            Console.ForegroundColor = startColor;
        }
    }
}