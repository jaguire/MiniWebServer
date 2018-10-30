using Nano.Web.Core;
using Serilog;

namespace MiniWebServer
{
    public static class HttpHost
    {
        public static void Init(NanoConfiguration nano, string webRoot)
        {
            Log.Information("wwwRoot: {webRoot}", webRoot);
            nano.AddDirectory("/", webRoot, returnHttp404WhenFileWasNotFound: true);
        }
    }
}