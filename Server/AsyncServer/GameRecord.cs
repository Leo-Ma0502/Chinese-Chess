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
        public string epPlayer1 { get; set; }
        public string epPlayer2 { get; set; }
        public string[,] initialStatus { get; set; }
        public Move? lastMovePlayer1 { get; set; }
        public Move? lastMovePlayer2 { get; set; }
        public string? whoseTurn { get; set; }
        public GameRecord(int gameID, string status, string player1, string player2, string epPlayer1, string epPlayer2, string[,] initialStatus, Move? lastMovePlayer1, Move? lastMovePlayer2, string? whoseTurn)
        {
            this.gameID = gameID;
            this.status = status;
            this.player1 = player1;
            this.player2 = player2;
            this.epPlayer1 = epPlayer1;
            this.epPlayer2 = epPlayer2;
            this.initialStatus = initialStatus;
            this.lastMovePlayer1 = lastMovePlayer1;
            this.lastMovePlayer1 = lastMovePlayer2;
            this.whoseTurn = whoseTurn;
        }
    }
}