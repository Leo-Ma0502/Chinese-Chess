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
        private List<string> waitingQueue = new();
        private List<GameRecord> gameRecords = new();
        public Server(IPAddress ip, int port, object[,] status)
        {
            this.ip = ip;
            this.port = port;
            this.status = getStatus(); // global status of combat
            // gameRecords.Add(new GameRecord(0, "invalid", "invalid", "invalid"));
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
                // each connection occupies 1 thread
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
                            RespondClient(response, handler, current_conn);
                        }

                    }
                }).Start();
            }
        }
        // parse params from request like /A?B=x&C=y...
        private string getParams(string[] parsedReq, string paramName)
        {
            if (parsedReq.Contains(paramName))
            {
                int index = Array.IndexOf(parsedReq, paramName);
                string paramValue = parsedReq[index + 1];
                return paramValue;
            }
            else
            {
                return "null";
            }
        }
        private void RespondClient(string response, Socket handler, int current_conn)
        {
            new Thread(async () =>
            {
                // parse request
                var reqString = response.Split(new char[] { ' ', '?', '=', '&' });
                int startingIndex = reqString[0].Equals("GET") || reqString[0].Equals("POST") || reqString[0].Equals(" ") ? 1 : 0;

                // /register: assign user name
                if (reqString[startingIndex].Trim().Equals("/register"))
                {
                    string res = GetRandomName();
                    string responseHEAD = $"HTTP/1.1 200 OK\r\nAccess-Control-Allow-Origin: *\r\nContent-Type: text/plain\r\nContent-Length: {res.Length}\r\n\r\n{res}";
                    var echoBytes = Encoding.UTF8.GetBytes(responseHEAD);
                    await handler.SendAsync(echoBytes, SocketFlags.None);
                    Console.WriteLine("Socket server sent message on thread {0}: {1}", Environment.CurrentManagedThreadId, res);

                }
                // /pair
                else if (reqString[startingIndex].Trim().Equals("/pair"))
                {
                    string res;
                    try
                    {
                        string player = getParams(reqString, "player");
                        if (player != null && !player.Equals("null"))
                        {
                            GameRecord? existing = gameRecords.Find(re => re.player1.Trim().Equals(player.Trim()) || re.player2.Trim().Equals(player.Trim()));
                            if (existing != null)
                            {
                                if (existing.status.Trim().Equals("progress"))
                                {
                                    res = "You cannot request for pairing while in a progressing game";
                                }
                                else
                                {
                                    res = PairUp(player);
                                }
                            }
                            else
                            {
                                res = PairUp(player);
                            }
                        }
                        else
                        {
                            res = "invalid request, username must be provided";
                        }
                    }
                    catch
                    {
                        res = "invalid request";
                    }
                    PrintRecord(gameRecords);
                    string responseHEAD = $"HTTP/1.1 200 OK\r\nAccess-Control-Allow-Origin: *\r\nContent-Type: text/plain\r\nContent-Length: {res.Length}\r\n\r\n{res}";
                    var echoBytes = Encoding.UTF8.GetBytes(responseHEAD);
                    await handler.SendAsync(echoBytes, SocketFlags.None);
                }
                // /quit
                else if (reqString[startingIndex].Trim().Equals("/quit"))
                {
                    string res = "Bye";
                    string responseHEAD = $"HTTP/1.1 200 OK\r\nAccess-Control-Allow-Origin: *\r\nContent-Type: text/plain\r\nContent-Length: {res.Length}\r\n\r\n{res}";
                    var echoBytes = Encoding.UTF8.GetBytes(responseHEAD);
                    await handler.SendAsync(echoBytes, SocketFlags.None);
                    current_conn--;
                    Console.WriteLine("{0} went off line, current connection: {1}", handler.RemoteEndPoint, current_conn);
                }
                // /initialstatus
                else if (reqString[startingIndex].Trim().Equals("/initialstatus"))
                {
                    string res;
                    string[] temp = new string[90];
                    try
                    {
                        string? player = getParams(reqString, "player");
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
                // 404
                else
                {
                    string res = "unsupported request";
                    string responseHEAD = $"HTTP/1.1 404 NOT FOUND\r\nAccess-Control-Allow-Origin: *\r\nContent-Type: text/plain\r\nContent-Length: {res.Length}\r\n\r\n{res}";
                    var echoBytes = Encoding.UTF8.GetBytes(responseHEAD);
                    await handler.SendAsync(echoBytes, SocketFlags.None);
                }
            }).Start();
        }
        private string GetRandomName()
        {
            string charPool = "QWERTYUIOPASDFGHJKLZXCVBNMqwertyuiopasdfghjklzxcvbnm1234567890";
            int lengthOfName = new Random().Next(0, charPool.Length);
            string name = "";
            for (int i = 0; i < lengthOfName; i++)
            {
                name += charPool[new Random().Next(0, charPool.Length)];
            }
            name += new Random().Next(100, 1000);
            return name;
        }
        private string PairUp(string player)
        {
            string res;
            if (!waitingQueue.Contains(player))
            {
                waitingQueue.Add(player);
                Console.WriteLine("A new player {0} started waiting in pool", player);
            }
            else
            {
                Console.WriteLine("{0} is still waiting in pool", player);
            }
            if (waitingQueue.Count != 1)
            {
                // pair this player with another waiting player and delete both of them from waiting pool
                foreach (var item in gameRecords)
                {
                    if (item.player1 == waitingQueue[0])
                    {
                        item.player2 = player;
                        item.status = "progress";
                    }
                }
                res = string.Format("You have been paired with {0}, good luck!", waitingQueue[0]);
                waitingQueue.Remove(player);
                waitingQueue.Remove(waitingQueue[0]);
            }
            else
            {
                // no other players waiting
                GameRecord waiting = new GameRecord(gameRecords.Count, "wait", player, "null");
                gameRecords.Add(waiting);

                if (gameRecords[gameRecords.IndexOf(waiting)].status.Equals("wait"))
                {
                    res = "You have been added to the waiting queue, searching for another player...";
                }
                else
                {
                    res = string.Format("You have been paired with {0}, good luck!", gameRecords[gameRecords.IndexOf(waiting)].player2);
                }
            }
            return res;
        }
        private void PrintRecord(List<GameRecord> list)
        {
            string output = "";
            foreach (var item in list)
            {
                output += string.Format("\nID:{0}, status:{1}, player1:{2}, player2:{3}", item.gameID, item.status, item.player1, item.player2);
            }
            Console.WriteLine("Game Records: {0}", output);
        }
    }
}
