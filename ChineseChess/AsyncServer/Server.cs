using System.Net;
using System.Net.Sockets;
using System.Text;
namespace AsyncServer
{
    public class Server
    {
        private readonly IPAddress ip;
        private readonly int port;
        public Server(IPAddress ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }

        //the main method to receive and send data
        //source: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/socket-services#create-a-socket-server
        public async Task Serve()
        {
            IPEndPoint ipEndPoint = new(ip, port);

            using Socket listener = new(
                ipEndPoint.AddressFamily,
                SocketType.Stream,
                ProtocolType.Tcp);

            listener.Bind(ipEndPoint);
            listener.Listen(100);
            Console.WriteLine("Listening at {0}:{1}", ip, port);

            var handler = await listener.AcceptAsync();
            Thread thread = new(async () =>
            {
                while (true)
                {
                    // Receive message.
                    var buffer = new byte[1_024];
                    var received = await handler.ReceiveAsync(buffer, SocketFlags.None);
                    var response = Encoding.UTF8.GetString(buffer, 0, received);

                    var eom = "<|EOM|>";
                    if (response.IndexOf(eom) > -1 /* is end of message */)
                    {
                        await RespondClient(response, eom, handler);

                    }
                }
            });
            thread.Start();
        }

        public static async Task<int> RespondClient(string response, string eom, Socket handler)
        {
            Console.WriteLine(
                        $"Socket server received message: \"{response.Replace(eom, "")}\"");

            var ackMessage = "<|ACK|>";
            var echoBytes = Encoding.UTF8.GetBytes(ackMessage);
            await handler.SendAsync(echoBytes, 0);
            Console.WriteLine(
                $"Socket server sent acknowledgment: \"{ackMessage}\"");
            if (response.Contains("over"))
            {
                Console.WriteLine("over");
                return 1;
            }
            else
            {
                Console.WriteLine("continue");
                return 0;
            }
        }
    }
}
