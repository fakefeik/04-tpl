using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Remoting;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HashServer;

namespace Balancer
{
    class Program
    {
        private static int timeout = 3000;
        private static string[] topology; 
        private static Random random = new Random();

        static void Main(string[] args)
        {
            var startFile = args.Length == 0 ? "HashServer.exe" : args[0];
            var lines = File.ReadAllLines("topology.conf");
            topology = lines.Select(x => x.Split(' ')[0]).ToArray();
            var port = 6002;
            foreach (var line in lines)
                Process.Start(startFile, line.Split(new [] {':', ' '})[1] + " " + line.Split(new []{':', ' '})[2]);
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
            var deflate = (context.Request.Headers["Accept-Encoding"] ?? "").Contains("deflate");
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
                    //Console.WriteLine("werehere");
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
            
            if (deflate)
            {
                Console.WriteLine("Encoding with deflate...");
                context.Response.Headers.Add("Content-Encoding", "deflate");
                var bytes = Compress(st);
                await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            }
            else
            {
                var s = await new StreamReader(st).ReadToEndAsync();
                await context.Response.OutputStream.WriteAsync(s.Select(Convert.ToByte).ToArray(), 0, s.Length);
            }
            context.Response.OutputStream.Close();
        }

        static async Task<Stream> DownloadPageAsync(string uri)
        {
            var request = CreateRequest(uri);
            var reply = await request.GetResponseAsync();
            return reply.GetResponseStream();
        }

        static byte[] Compress(Stream input)
        {
            using (var compressStream = new MemoryStream())
            using (var compressor = new DeflateStream(compressStream, CompressionMode.Compress))
            {
                input.CopyTo(compressor);
                compressor.Close();
                return compressStream.ToArray();
            }
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
