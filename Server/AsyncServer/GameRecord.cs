using System.Net;
using System.Net.Sockets;
namespace AsyncServer
{
    public class GameRecord
    {
        public int gameID { get; set; }
        public string status { get; set; }
        public string player1 { get; set; }
        public string player2 { get; set; }
        public Socket? epPlayer1 { get; set; }
        public Socket? epPlayer2 { get; set; }
        public string[,] initialStatus { get; set; }
        public GameRecord(int gameID, string status, string player1, string player2, Socket? epPlayer1, Socket? epPlayer2, string[,] initialStatus)
        {
            this.gameID = gameID;
            this.status = status;
            this.player1 = player1;
            this.player2 = player2;
            this.epPlayer1 = epPlayer1;
            this.epPlayer2 = epPlayer2;
            this.initialStatus = initialStatus;
        }
    }
}