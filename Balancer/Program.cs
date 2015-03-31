using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Remoting;
using System.Threading;
using System.Threading.Tasks;
using HashServer;

namespace Balancer
{
    class Program
    {
        private static int timeout = 2000;
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

        private static async Task OnContextAsync(HttpListenerContext context)
        {
            var requestId = Guid.NewGuid();
            var query = context.Request.QueryString["query"];
            var remoteEndPoint = context.Request.RemoteEndPoint;
            Console.WriteLine("{0}: received {1} from {2}", requestId, query, remoteEndPoint);
            context.Request.InputStream.Close();
            var upServers = topology.ToList();
            Stream st = null;
            while (st == null)
            {
                if (!upServers.Any())
                {
                    Console.WriteLine("Looks like all servers are down...");
                    context.Response.StatusCode = 500;
                    context.Response.Close();
                    return;
                }

                var server = upServers[random.Next(upServers.Count)];
                try
                {
                    var tasks = new Task<Stream>[2];
                    tasks[0] =
                        DownloadPageAsync("http://" + server + "/" + context.Request.RawUrl);
                    tasks[1] = Task.Run(async () =>
                    {
                        await Task.Delay(timeout);
                        return (Stream) null;
                    });

                    var task = await Task.WhenAny(tasks);
                    st = task.Result;
                }
                catch (AggregateException e)
                {
                    upServers.Remove(server);
                    Console.WriteLine("Something bad just happened at server {0}:\r\n\t{1}", server, e.InnerException.Message);
                    continue;
                }
                if (st == null)
                {
                    upServers.Remove(server);
                    Console.WriteLine("Server {0} timed out.", server);
                }
                else Console.WriteLine("Redirected to server {0}.", server);
            }
            var s = await new StreamReader(st).ReadToEndAsync();
            await context.Response.OutputStream.WriteAsync(s.Select(Convert.ToByte).ToArray(), 0, s.Length);
            context.Response.OutputStream.Close();
        }

        static async Task<Stream> DownloadPageAsync(string uri)
        {
            var request = CreateRequest(uri);
            var reply = await request.GetResponseAsync();
            return reply.GetResponseStream();
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
