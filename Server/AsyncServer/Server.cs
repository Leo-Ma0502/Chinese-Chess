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

            var current_conn = 0; // current connected clients
            while (true)
            {
                var handler = await listener.AcceptAsync();
                current_conn++;
                Console.WriteLine("{0} connected, current connection: {1}", handler.RemoteEndPoint, current_conn);
                new Thread(async () =>
                {
                    // Receive message.
                    while (true)
                    {
                        var buffer = new byte[1_024];
                        var received = await handler.ReceiveAsync(buffer, SocketFlags.None);
                        var response = Encoding.UTF8.GetString(buffer, 0, received);

                        var eom = "<|EOM|>";

                        if (response.Trim().Equals("bye") || received == 0)
                        {
                            current_conn--;
                            Console.WriteLine("{0} went off line, current connection: {1}", handler.RemoteEndPoint, current_conn);
                            break;
                        }
                        else
                        {
                            await RespondClient(response, eom, handler);
                        }
                    }
                }).Start();
            }
        }
        public static async Task RespondClient(string response, string eom, Socket handler)
        {
            Console.WriteLine("Socket server received message on thread {0}: {1}", Environment.CurrentManagedThreadId, response);

            var ackMessage = "<|ACK|>";
            var responseHEAD = $"HTTP/1.1 200 OK\r\nAccess-Control-Allow-Origin: *\r\nContent-Type: text/plain\r\nContent-Length: {ackMessage.Length}\r\n\r\n{ackMessage}";
            // var echoBytes = Encoding.UTF8.GetBytes(ackMessage);
            var echoBytes = Encoding.UTF8.GetBytes(responseHEAD);
            await handler.SendAsync(echoBytes, SocketFlags.None);
            Console.WriteLine("Socket server sent message on thread {0}: {1}", Environment.CurrentManagedThreadId, ackMessage);
        }
    }
}
