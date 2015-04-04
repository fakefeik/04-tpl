using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using log4net.Config;

namespace HashServer
{
	class Program
	{
	    private static int timeout = 1000;
		static void Main(string[] args)
		{
			XmlConfigurator.Configure();
			try
			{
				var listener = new Listener(args.Length == 0 ? port : int.Parse(args[0]), "method", OnContextAsync);
			    timeout = args.Length > 1 ? int.Parse(args[1]) : timeout;
				listener.Start();

                Console.WriteLine("Timeout: " + timeout);
				//var listenerSync = new ListenerSync(port, "methodSync", OnContext);
				//listenerSync.Start();

				log.InfoFormat("Server started!");
				new ManualResetEvent(false).WaitOne();
			}
			catch(Exception e)
			{
				log.Fatal(e);
				throw;
			}
		}

		private static async Task OnContextAsync(HttpListenerContext context)
		{
			var requestId = Guid.NewGuid();
			var query = context.Request.QueryString["query"];
			var remoteEndPoint = context.Request.RemoteEndPoint;
			log.InfoFormat("{0}: received {1} from {2}", requestId, query, remoteEndPoint);
			context.Request.InputStream.Close();

			await Task.Delay(timeout);
//			Thread.Sleep(1000);

			var hash = Convert.ToBase64String(CalcHash(Encoding.UTF8.GetBytes(query)));
			var encryptedBytes = Encoding.UTF8.GetBytes(hash);

			await context.Response.OutputStream.WriteAsync(encryptedBytes, 0, encryptedBytes.Length);
			context.Response.OutputStream.Close();
			log.InfoFormat("{0}: {1} sent back to {2}", requestId, hash, remoteEndPoint);
		}

		private static void OnContext(HttpListenerContext context)
		{
			var requestId = Guid.NewGuid();
			var query = context.Request.QueryString["query"];
			var remoteEndPoint = context.Request.RemoteEndPoint;
			log.InfoFormat("{0}: received {1} from {2}", requestId, query, remoteEndPoint);
			context.Request.InputStream.Close();

			Thread.Sleep(1000);

			var hash = Convert.ToBase64String(CalcHash(Encoding.UTF8.GetBytes(query)));
			var encryptedBytes = Encoding.UTF8.GetBytes(hash);

			context.Response.OutputStream.WriteAsync(encryptedBytes, 0, encryptedBytes.Length);
			context.Response.OutputStream.Close();
			log.InfoFormat("{0}: {1} sent back to {2}", requestId, hash, remoteEndPoint);
		}

		private static byte[] CalcHash(byte[] data)
		{
			using(var hasher = new HMACMD5(Key))
				return hasher.ComputeHash(data);
		}

		private const int port = 20000;
		private static readonly byte[] Key = Encoding.UTF8.GetBytes("Контур.Шпора");
		private static readonly ILog log = LogManager.GetLogger(typeof(Program));
	}
}