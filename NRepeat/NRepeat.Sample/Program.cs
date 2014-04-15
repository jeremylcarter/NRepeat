using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NRepeat.Sample
{
    class Program
    {
        public static TcpProxy Proxy;
        static void Main(string[] args)
        {

            var definition = new ProxyDefinition() { ServerAddress = IPAddress.Any };

            // Use localhost if the debugger is attached
            if (Debugger.IsAttached)
            {
                definition.ServerAddress = IPAddress.Any;
                definition.ServerPort = 4501;
            }
            else
            {
                Console.WriteLine("Enter server port:");
                var port = Console.ReadLine();
                if (!String.IsNullOrEmpty(port))
                {
                    definition.ServerPort = Convert.ToInt16(port);
                }

                Console.WriteLine("Enter client/destination address:");
                var address = Console.ReadLine();
                if (!String.IsNullOrEmpty(port))
                {
                    definition.ClientAddress = IPAddress.Parse(address);
                }

                Console.WriteLine("Enter client/destination port:");
                var clientPort = Console.ReadLine();

                if (!String.IsNullOrEmpty(port))
                {
                    definition.ClientPort = Convert.ToInt16(clientPort);
                }
            }
            

            Proxy = new TcpProxy(definition);
            Proxy.Start();

            Console.WriteLine("Proxy started between {0}:{1} and {2}:{3}",
                definition.ServerAddress, definition.ServerPort,
                definition.ClientAddress, definition.ClientPort);

            Proxy.BytesTransfered += Proxy_BytesTransfered;
            Proxy.ServerDataSentToClient += Proxy_ServerDataSentToClient;
            Proxy.ClientDataSentToServer += Proxy_ClientDataSentToServer;

            Console.WriteLine("Press any key to stop proxy");
            Console.ReadLine();
            Proxy.Stop();
            Console.WriteLine("Press any key to exit");
            Console.ReadLine();

        }

        static void Proxy_BytesTransfered(object sender, ProxyByteDataEventArgs e)
        {
            Console.WriteLine("{0} : {1} sent {2}", DateTime.Now, e.Source, Encoding.ASCII.GetString(e.Bytes));
        }

        static void Proxy_ClientDataSentToServer(object sender, ProxyDataEventArgs e)
        {
            Console.WriteLine("{0} : Client sent {1} bytes to Server", DateTime.Now, e.Bytes);
        }

        static void Proxy_ServerDataSentToClient(object sender, ProxyDataEventArgs e)
        {
            Console.WriteLine("{0} : Server sent {1} bytes to Client", DateTime.Now, e.Bytes);
        }
    }
}
