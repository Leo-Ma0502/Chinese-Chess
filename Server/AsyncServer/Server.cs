using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
namespace AsyncServer
{
    public class Server
    {
        private readonly Object _lockForGameRecords = new();
        private readonly IPAddress ip;
        private readonly int port;
        private List<string> waitingQueue = new();
        private List<GameRecord> gameRecords = new();
        public Server(IPAddress ip, int port)
        {
            this.ip = ip;
            this.port = port;
        }
        // global status of combat
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
                            }
                            else if (j == 3 || j == 5)
                            {
                                div = "士";
                            }
                            else if (j == 4)
                            {
                                div = "將";
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
                            }
                            else if (j == 1 || j == 7)
                            {
                                div = "馬";
                            }
                            else if (j == 2 || j == 6)
                            {
                                div = "相";
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
                        try
                        {
                            var buffer = new byte[1_024];
                            var received = await handler.ReceiveAsync(buffer, SocketFlags.None);
                            var response = Encoding.UTF8.GetString(buffer, 0, received);
                            if (received == 0)
                            {
                                current_conn--;
                                string vanishedEndPoint = handler.RemoteEndPoint.ToString();
                                Console.WriteLine("{0} went off line, current connection: {1}", vanishedEndPoint, current_conn);
                                HandleOffline(vanishedEndPoint);
                                break;
                            }
                            else
                            {
                                lock (_lockForGameRecords)
                                {
                                    RespondClient(response, handler, current_conn);
                                }
                            }
                        }
                        catch
                        {
                            Console.WriteLine("connection lost for funciton Serve()");
                            break;
                        };
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
                if (paramValue.Trim().Length != 0)
                {
                    return paramValue.Trim();
                }
                else
                {
                    return "null";
                }
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
                    Console.WriteLine("Thread {0} responded {1} for {2}", Environment.CurrentManagedThreadId, handler.RemoteEndPoint, reqString[startingIndex].Trim());

                }
                //   /
                else if (reqString[startingIndex].Trim().Equals("/"))
                {
                    string res;
                    try
                    {
                        res = "Hello";
                    }
                    catch
                    {
                        res = "invalid request";
                    }
                    string responseHEAD = $"HTTP/1.1 200 OK\r\nAccess-Control-Allow-Origin: *\r\nContent-Type: text/plain\r\nContent-Length: {res.Length}\r\n\r\n{res}";
                    var echoBytes = Encoding.UTF8.GetBytes(responseHEAD);
                    await handler.SendAsync(echoBytes, SocketFlags.None);
                    Console.WriteLine("Thread {0} responded {1} for {2}", Environment.CurrentManagedThreadId, handler.RemoteEndPoint, reqString[startingIndex].Trim());
                }
                // /pair
                else if (reqString[startingIndex].Trim().Equals("/pair"))
                {
                    string res;
                    string player = getParams(reqString, "player").Trim();
                    try
                    {
                        if (player != null && !player.Equals("null"))
                        {
                            GameRecord? existing = gameRecords.Find(re => re.player1.Trim().Equals(player) || re.player2.Trim().Equals(player));
                            if (existing != null) // firstly, find if this player is in existing game record 
                            {
                                if (existing.status.Trim().Equals("progress")) // then, check if this game is progressing 
                                {
                                    // if the game is progressing, notify the player the game begins
                                    Response2Player resObj = new Response2Player
                                    {
                                        msg = string.Format("You have been paired with {0}, good luck!", existing.player1.Equals(player) ? existing.player2 : existing.player1),
                                        username = player,
                                        label = "1",
                                        gameID = existing.gameID.ToString()
                                    };
                                    res = JsonSerializer.Serialize(resObj);
                                }
                                else
                                {
                                    // if not, pass it on to pairup function
                                    res = PairUp(player, handler);
                                }
                            }
                            else // if the player is not in existing record, pass it on to pair up function
                            {
                                res = PairUp(player, handler);
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

                    string responseHEAD = $"HTTP/1.1 200 OK\r\nAccess-Control-Allow-Origin: *\r\nContent-Type: text/plain\r\nContent-Length: {res.Length}\r\n\r\n{res}";
                    var echoBytes = Encoding.UTF8.GetBytes(responseHEAD);
                    try
                    {
                        await handler.SendAsync(echoBytes, SocketFlags.None);
                        Console.WriteLine("Thread {0} responded {1} for {2}", Environment.CurrentManagedThreadId, handler.RemoteEndPoint, string.Format("{0}?player={1}", reqString[startingIndex].Trim(), player));
                    }
                    catch
                    {
                        Console.WriteLine("Connection lost for pairing");
                    }

                }
                // /quit
                else if (reqString[startingIndex].Trim().Equals("/quit"))
                {
                    string res;
                    string player = getParams(reqString, "player").Trim();
                    string gameID = getParams(reqString, "gameID").Trim();
                    try
                    {
                        if (player != null && !player.Equals("null") && gameID != null && !gameID.Equals("null"))
                        {
                            res = "bye";
                            var record = gameRecords.Find(record => record.gameID.ToString().Equals(gameID));
                            if (record.player1.Equals(player))
                            {
                                HandleOffline(record.epPlayer1);
                            }
                            else if (record.player2.Equals(player))
                            {
                                HandleOffline(record.epPlayer2);
                            }
                        }
                        else
                        {
                            res = "Invalid Request";
                        }
                    }
                    catch { res = "Invalid Request"; }
                    string responseHEAD = $"HTTP/1.1 200 OK\r\nAccess-Control-Allow-Origin: *\r\nContent-Type: text/plain\r\nContent-Length: {res.Length}\r\n\r\n{res}";
                    var echoBytes = Encoding.UTF8.GetBytes(responseHEAD);
                    try
                    {
                        await handler.SendAsync(echoBytes, SocketFlags.None);
                        Console.WriteLine("Thread {0} responded {1} for {2}", Environment.CurrentManagedThreadId, handler.RemoteEndPoint, string.Format("{0}?player={1}&gameID={2}", reqString[startingIndex].Trim(), player, gameID));
                    }
                    catch
                    {
                        Console.WriteLine("Connection lost for quit");
                    }

                }
                // /mymove
                else if (reqString[startingIndex].Trim().Equals("/mymove"))
                {
                    string res;
                    string[] temp = new string[90];
                    string player = getParams(reqString, "player").Trim();
                    string gameID = getParams(reqString, "gameID").Trim();
                    string orow = getParams(reqString, "orow").Trim();
                    string ocol = getParams(reqString, "ocol").Trim();
                    string nrow = getParams(reqString, "nrow").Trim();
                    string ncol = getParams(reqString, "ncol").Trim();
                    string fac = getParams(reqString, "fac").Trim();
                    string div = getParams(reqString, "div").Trim();
                    try
                    {
                        if (player != null && !player.Equals("null") && gameID != null && !gameID.Equals("null") && orow != null && !orow.Equals("null") && ocol != null && !ocol.Equals("null") && nrow != null && !nrow.Equals("null") && ncol != null && !ncol.Equals("null") && fac != null && !fac.Equals("null") && div != null && !div.Equals("null"))
                        {
                            var record = gameRecords.Find(record => record.gameID.ToString().Equals(gameID));
                            if (record.status.Equals("progress"))
                            {
                                if (record.player1.Equals(player))
                                {
                                    record.lastMovePlayer1 = new Move { nrow = int.Parse(nrow), ncol = int.Parse(ncol), orow = int.Parse(orow), ocol = int.Parse(ocol), fac = fac, div = div };
                                    record.whoseTurn = "player2";
                                    Console.WriteLine("In game {0}, player {1} moved the chess from [row {2} col {3}] to [row {4} col {5}]", record.gameID, record.player1, orow, ocol, nrow, ncol);
                                }
                                else if (record.player2.Equals(player))
                                {
                                    record.lastMovePlayer2 = new Move { nrow = int.Parse(nrow), ncol = int.Parse(ncol), orow = int.Parse(orow), ocol = int.Parse(ocol), fac = fac, div = div };
                                    record.whoseTurn = "player1";
                                    Console.WriteLine("In game {0}, player {1} moved the chess from [row {2} col {3}] to [row {4} col {5}]", record.gameID, record.player2, orow, ocol, nrow, ncol);
                                }
                                res = "your move has been submitted";
                            }
                            else
                            {
                                res = "This game has been terminated because the other player went offline";
                            }
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
                    Console.WriteLine("Thread {0} responded {1} for {2}", Environment.CurrentManagedThreadId, handler.RemoteEndPoint, string.Format("{0}?player={1}&gameID={2}&orow={3}&ocol={4}&nrow={5}&ncol={6}&fac={7}&div={8}", reqString[startingIndex].Trim(), player, gameID, orow, ocol, nrow, ncol, fac, div));
                }
                // /initialstatus
                else if (reqString[startingIndex].Trim().Equals("/initialstatus"))
                {
                    string res;
                    string[] temp = new string[90];
                    string player = getParams(reqString, "player").Trim();
                    string gameID = getParams(reqString, "gameID").Trim();
                    try
                    {
                        if (player != null && !player.Equals("null") && gameID != null && !gameID.Equals("null"))
                        {
                            int k = 0;
                            for (int i = 0; i < 10; i++)
                            {
                                for (int j = 0; j < 9; j++)
                                {
                                    // temp[k] = this.status[i, j];
                                    var record = gameRecords.Find(record => record.gameID.ToString().Equals(gameID));
                                    temp[k] = record.initialStatus[i, j];
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
                    Console.WriteLine("Thread {0} responded {1} for {2}", Environment.CurrentManagedThreadId, handler.RemoteEndPoint, string.Format("{0}?player={1}&gameID={2}", reqString[startingIndex].Trim(), player, gameID));
                }
                // /theirmove
                else if (reqString[startingIndex].Trim().Equals("/theirmove"))
                {
                    string res;
                    string[] temp = new string[90];
                    string player = getParams(reqString, "player").Trim();
                    string gameID = getParams(reqString, "gameID").Trim();
                    try
                    {
                        if (player != null && !player.Equals("null") && gameID != null && !gameID.Equals("null"))
                        {
                            var record = gameRecords.Find(record => record.gameID.ToString().Equals(gameID));
                            if (record.status.Equals("progress"))
                            {
                                if (record.player1.Equals(player))
                                {
                                    res = JsonSerializer.Serialize(record.lastMovePlayer2);
                                }
                                else
                                {
                                    res = JsonSerializer.Serialize(record.lastMovePlayer1);
                                }
                            }
                            else
                            {
                                res = "This game has been terminated because the other player went offline";
                            }
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
                    Console.WriteLine("Thread {0} responded {1} for {2}", Environment.CurrentManagedThreadId, handler.RemoteEndPoint, string.Format("{0}?player={1}&gameID={2}", reqString[startingIndex].Trim(), player, gameID));
                }
                // myturn
                else if (reqString[startingIndex].Trim().Equals("/myturn"))
                {
                    string res;
                    string[] temp = new string[90];
                    string player = getParams(reqString, "player").Trim();
                    string gameID = getParams(reqString, "gameID").Trim();
                    try
                    {
                        if (player != null && !player.Equals("null") && gameID != null && !gameID.Equals("null"))
                        {
                            var record = gameRecords.Find(record => record.gameID.ToString().Equals(gameID));
                            if (record.status.Equals("progress"))
                            {
                                if (record.player1.Equals(player))
                                {
                                    if (record.whoseTurn.Equals("player1"))
                                    {
                                        res = "true";
                                    }
                                    else
                                    {
                                        res = "false";
                                    }
                                }
                                else if (record.player2.Equals(player))
                                {
                                    if (record.whoseTurn.Equals("player2"))
                                    {
                                        res = "true";
                                    }
                                    else
                                    {
                                        res = "false";
                                    }
                                }
                                else
                                {
                                    res = "player not found";
                                }
                            }
                            else
                            {
                                res = "This game has been terminated because the other player went offline";
                            }
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
                    Console.WriteLine("Thread {0} responded {1} for {2}", Environment.CurrentManagedThreadId, handler.RemoteEndPoint, string.Format("{0}?player={1}&gameID={2}", reqString[startingIndex].Trim(), player, gameID));
                }
                // 404
                else
                {
                    string res = "unsupported request";
                    string responseHEAD = $"HTTP/1.1 404 NOT FOUND\r\nAccess-Control-Allow-Origin: *\r\nContent-Type: text/plain\r\nContent-Length: {res.Length}\r\n\r\n{res}";
                    var echoBytes = Encoding.UTF8.GetBytes(responseHEAD);
                    await handler.SendAsync(echoBytes, SocketFlags.None);
                    Console.WriteLine("Thread {0} rejected an invalid request from {1}", Environment.CurrentManagedThreadId, handler.RemoteEndPoint);
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
        // This function deals with two situations: the player is not in exisitng record and the player is in existing record but the game is not progressing
        private string PairUp(string player, Socket handler)
        {
            // firstly, if the player is not in the waiting pool, add it in
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
            if (waitingQueue.Count != 1) // there is another player waiting
            {
                int currGID;
                if (gameRecords.Find(record => record.player1.Equals(waitingQueue[0]) && record.status.Equals("wait")) == null)
                {
                    // add the waiting player to record, labeling it waiting, although this situation is highly unlikely
                    GameRecord waiting = new GameRecord(gameRecords.Count, "wait", player, "null", handler.RemoteEndPoint.ToString(), "", getStatus(), null, null, "player1");
                    gameRecords.Add(waiting);
                    currGID = waiting.gameID;
                }
                else // the waiting player is already in the record
                {
                    currGID = gameRecords.Find(record => record.player1.Equals(waitingQueue[0]) && record.status.Equals("wait")).gameID;
                }
                gameRecords[currGID].status = "progress";
                gameRecords[currGID].player2 = player;
                gameRecords[currGID].epPlayer2 = handler.RemoteEndPoint.ToString();
                Response2Player resObj = new Response2Player
                {
                    msg = string.Format("You have been paired with {0}, good luck!", waitingQueue[0]),
                    gameID = currGID.ToString(),
                    username = player,
                    label = "2"
                };
                res = JsonSerializer.Serialize(resObj);
                waitingQueue.Remove(player);
                waitingQueue.Remove(waitingQueue[0]);
                Console.WriteLine("Game {0} started", gameRecords[currGID].gameID);
            }
            else // there is no other player in waiting queue
            {
                int currGID;
                if (gameRecords.Find(record => record.player1.Equals(player) && record.status.Equals("wait")) == null) // the player has not been added to game record
                {
                    // add the player to record, labeling it waiting
                    GameRecord waiting = new GameRecord(gameRecords.Count, "wait", player, "null", handler.RemoteEndPoint.ToString(), "", getStatus(), null, null, "player1");
                    gameRecords.Add(waiting);
                    currGID = waiting.gameID;
                }
                else // the player is already in the record
                {
                    currGID = gameRecords.Find(record => record.player1.Equals(player) && record.status.Equals("wait")).gameID;
                }
                Response2Player resObj = new Response2Player
                {
                    msg = "You have been added to the waiting queue, searching for another player...",
                    gameID = gameRecords[currGID].gameID.ToString(),
                    username = player,
                    label = "1"
                };
                res = JsonSerializer.Serialize(resObj);
                string responseHEAD = $"HTTP/1.1 200 OK\r\nAccess-Control-Allow-Origin: *\r\nContent-Type: text/plain\r\nContent-Length: {res.Length}\r\n\r\n{res}";
                var echoBytes = Encoding.UTF8.GetBytes(responseHEAD);

                handler.Send(echoBytes, SocketFlags.None);
                Thread.Sleep(50);
            }
            return res;
        }
        private void PrintRecord(List<GameRecord> list)
        {
            string output = "";
            foreach (var item in list)
            {
                output += string.Format("\nID:{0}, status:{1}, player1:{2}, player2:{3}, epP1:{4}, epP2:{5}", item.gameID, item.status, item.player1, item.player2, item.epPlayer1, item.epPlayer2);
            }
            Console.WriteLine("Game Records: {0}", output);
        }
        private void HandleOffline(string vanishedEndPoint)
        {
            try
            {
                var relatedRecord = gameRecords.Find(item => (item.epPlayer1.Equals(vanishedEndPoint) || item.epPlayer2.Equals(vanishedEndPoint)) && !item.status.Equals("terminated"));
                if (relatedRecord != null)
                {
                    // change related game record
                    new Thread(() =>
                    {
                        relatedRecord.status = "terminated";
                        Console.WriteLine("Thread {0} closing connection with {1} and terminating...", Environment.CurrentManagedThreadId, vanishedEndPoint);
                        Console.WriteLine("Game {0} terminated", relatedRecord.gameID);
                    }).Start();
                    // listen on change of game record
                    new Thread(() =>
                    {
                        foreach (var item in gameRecords)
                        {
                            if (item.status.Equals("terminated"))
                            {
                                if (waitingQueue.Contains(item.player1))
                                {
                                    waitingQueue.Remove(item.player1);
                                }
                                if (waitingQueue.Contains(item.player2))
                                {
                                    waitingQueue.Remove(item.player2);
                                }
                            }
                        }

                    }).Start();
                }
            }
            catch
            {
                Console.WriteLine("Something went wrong...");
            }
        }

        // check connection on each socket 
        private Boolean CheckConnection(Socket handler)
        {
            var buffer = new byte[1_024];
            var received = handler.Receive(buffer, SocketFlags.None);
            if (received == 0)
            {
                return false;
            }
            return true;
        }
    }
}
