using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HashServer;

namespace Balancer
{
    class Program
    {
        private static string[] topology; 
        private static Random random = new Random();

        static void Main(string[] args)
        {
            var startFile = args.Length == 0 ? "HashServer.exe" : args[0];
            topology = File.ReadAllLines("topology.conf");
            var port = 6002;
            foreach (var line in topology)
                Process.Start(startFile, line.Split(':')[1]);
            var listener = new Listener(port, "method", OnContextAsync);
            listener.Start();
            new ManualResetEvent(false).WaitOne();
        }

        static async Task OnContextAsync(HttpListenerContext context)
        {
            var requestId = Guid.NewGuid();
            var query = context.Request.QueryString["query"];
            var remoteEndPoint = context.Request.RemoteEndPoint;
            Console.WriteLine("{0}: received {1} from {2}", requestId, query, remoteEndPoint);
            context.Request.InputStream.Close();
            var req = CreateRequest("http://" + topology[random.Next(topology.Length)] + "/" + context.Request.RawUrl);
            var resp = await req.GetResponseAsync();
            var s = await new StreamReader(resp.GetResponseStream()).ReadToEndAsync();
            await context.Response.OutputStream.WriteAsync(s.Select(Convert.ToByte).ToArray(), 0, s.Length);
            context.Response.OutputStream.Close();
        }

        static HttpWebRequest CreateRequest(string uri, int timeout = 30 * 1000)
        {
            var request = WebRequest.CreateHttp(uri);
            request.Timeout = timeout;
            request.Proxy = null;
            request.KeepAlive = true;
            request.ServicePoint.UseNagleAlgorithm = false;
            request.ServicePoint.ConnectionLimit = 1 << 10;
            return request;
        }
    }
}
