using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
namespace AsyncServer
{
    public class Server
    {
        private readonly IPAddress ip;
        private readonly int port;
        private string[,] status;
        public Server(IPAddress ip, int port, object[,] status)
        {
            this.ip = ip;
            this.port = port;
            this.status = getStatus(); // global status of combat
        }
        private static string[,] getStatus()
        {
            var temp = new string[10, 9];
            string fac = "null", div = "null";
            // i: row
            for (int i = 0; i < 10; i++)
            {
                // j: col
                for (int j = 0; j < 9; j++)
                {
                    if (i < 4)
                    {
                        fac = "black";
                        if (i == 2)
                        {
                            if (j == 1 || j == 7)
                            {
                                div = "炮";
                            }
                            else
                            {
                                fac = "null"; div = "null";
                            }
                        }
                        else if (i == 3)
                        {
                            if (j == 0 || j == 2 || j == 4 || j == 6 || j == 8)
                            {
                                div = "卒";
                            }
                            else
                            {
                                fac = "null"; div = "null";
                            }
                        }
                        else if (i == 0)
                        {
                            if (j == 0 || j == 8)
                            {
                                div = "車";
                            }
                            else if (j == 1 || j == 7)
                            {
                                div = "馬";
                            }
                            else if (j == 2 || j == 6)
                            {
                                div = "象";
                                //temp[i, j] = JsonSerializer.Serialize(new Status { row = i, col = j, faction = fac, division = div });
                            }
                            else if (j == 3 || j == 5)
                            {
                                div = "士";
                                //temp[i, j] = JsonSerializer.Serialize(new Status { row = i, col = j, faction = fac, division = div });
                            }
                            else if (j == 4)
                            {
                                div = "將";
                                //temp[i, j] = JsonSerializer.Serialize(new Status { row = i, col = j, faction = fac, division = div });
                            }
                            else
                            {
                                fac = "null"; div = "null";
                            }
                        }
                        else
                        {
                            fac = "null"; div = "null";
                        }
                    }
                    else if (i > 5)
                    {
                        fac = "red";
                        if (i == 7)
                        {
                            if (j == 1 || j == 7)
                            {
                                div = "砲";
                            }
                            else
                            {
                                fac = "null"; div = "null";
                            }
                        }
                        else if (i == 6)
                        {
                            if (j == 0 || j == 2 || j == 4 || j == 6 || j == 8)
                            {
                                div = "兵";
                            }
                            else
                            {
                                fac = "null"; div = "null";
                            }
                        }
                        else if (i == 9)
                        {
                            if (j == 0 || j == 8)
                            {
                                div = "車";
                                //temp[i, j] = JsonSerializer.Serialize(new Status { row = i, col = j, faction = fac, division = div });
                            }
                            else if (j == 1 || j == 7)
                            {
                                div = "馬";
                                //temp[i, j] = JsonSerializer.Serialize(new Status { row = i, col = j, faction = fac, division = div });
                            }
                            else if (j == 2 || j == 6)
                            {
                                div = "相";
                                // temp[i, j] = JsonSerializer.Serialize(new Status { row = i, col = j, faction = fac, division = div });
                            }
                            else if (j == 3 || j == 5)
                            {
                                div = "仕";
                            }
                            else if (j == 4)
                            {
                                div = "帥";
                            }
                            else
                            {
                                fac = "null"; div = "null";
                            }
                        }
                        else
                        {
                            fac = "null"; div = "null";
                        }
                    }
                    else
                    {
                        fac = "null"; div = "null";
                    }
                    temp[i, j] = JsonSerializer.Serialize(new Status { row = i, col = j, faction = fac, division = div });
                }
            }
            return temp;
        }
        //the main method to receive and send data
        //source: https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/sockets/socket-services#create-a-socket-server
        public async Task Serve()
        {
            IPEndPoint ipEndPoint = new(ip, port);

            var waitingQueue = new List<string?>();

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
                waitingQueue.Add(handler.RemoteEndPoint?.ToString());
                for (int i = 0; i < waitingQueue.Count; i++)
                {
                    Console.WriteLine(waitingQueue[i]);
                }
                Console.WriteLine("{0} connected, current connection: {1}", handler.RemoteEndPoint, current_conn);
                new Thread(async () =>
                {
                    // Receive message.
                    while (true)
                    {
                        var buffer = new byte[1_024];
                        var received = await handler.ReceiveAsync(buffer, SocketFlags.None);
                        var response = Encoding.UTF8.GetString(buffer, 0, received);
                        if (received == 0)
                        {
                            current_conn--;
                            Console.WriteLine("{0} went off line, current connection: {1}", handler.RemoteEndPoint, current_conn);
                            break;
                        }
                        else
                        {
                            await RespondClient(response, handler, current_conn);
                        }

                    }
                }).Start();
            }
        }
        // parse params from request like /A?B=x&C=y...
        private string? getParams(string[] parsedReq, string paramName)
        {
            if (parsedReq.Contains(paramName))
            {
                int index = Array.IndexOf(parsedReq, paramName);
                string? paramValue = parsedReq[index + 1];
                return paramValue;
            }
            else
            {
                return "null";
            }
        }
        private async Task RespondClient(string response, Socket handler, int current_conn)
        {
            var parsedReq = response.Contains(" ") ? response.Split(" ") : new string[1] { response };
            if (parsedReq.Length == 1)
            {
                Console.WriteLine(parsedReq[0]);
            }
            else
            {
                // /register: assign user name
                string reqString = parsedReq[1];
                var parsedRqeString = reqString.Split(new char[] { '?', '=', '&' });
                if (reqString.StartsWith("/register"))
                {
                    string res;
                    if (current_conn % 2 != 0)
                    {
                        res = "Han";
                    }
                    else
                    {
                        res = "Chu";
                    }
                    string responseHEAD = $"HTTP/1.1 200 OK\r\nAccess-Control-Allow-Origin: *\r\nContent-Type: text/plain\r\nContent-Length: {res.Length}\r\n\r\n{res}";
                    var echoBytes = Encoding.UTF8.GetBytes(responseHEAD);
                    await handler.SendAsync(echoBytes, SocketFlags.None);
                    Console.WriteLine("Socket server sent message on thread {0}: {1}", Environment.CurrentManagedThreadId, res);

                }
                // /quit
                else if (reqString.StartsWith("/quit"))
                {
                    string res = "Bye";
                    string responseHEAD = $"HTTP/1.1 200 OK\r\nAccess-Control-Allow-Origin: *\r\nContent-Type: text/plain\r\nContent-Length: {res.Length}\r\n\r\n{res}";
                    var echoBytes = Encoding.UTF8.GetBytes(responseHEAD);
                    await handler.SendAsync(echoBytes, SocketFlags.None);
                    current_conn--;
                    Console.WriteLine("{0} went off line, current connection: {1}", handler.RemoteEndPoint, current_conn);
                }
                // /initialstatus
                else if (reqString.StartsWith("/initialstatus"))
                {
                    string res;
                    string[] temp = new string[90];
                    try
                    {
                        string? player = getParams(parsedRqeString, "player");
                        if (player != null && (player.Equals("Han") || player.Equals("Chu")))
                        {
                            int k = 0;
                            for (int i = 0; i < 10; i++)
                            {
                                for (int j = 0; j < 9; j++)
                                {
                                    temp[k] = this.status[i, j];
                                    k++;
                                }
                            }
                            res = string.Join(",", temp);
                        }
                        else
                        {
                            res = "invalid request";
                        }
                    }
                    catch
                    {
                        res = "invalid request";
                    }
                    string responseHEAD = $"HTTP/1.1 200 OK\r\nAccess-Control-Allow-Origin: *\r\nContent-Type: text/plain\r\nContent-Length: {res.Length}\r\n\r\n{res}";
                    var echoBytes = Encoding.UTF8.GetBytes(responseHEAD);
                    await handler.SendAsync(echoBytes, SocketFlags.None);
                }
            }
        }
    }
}
