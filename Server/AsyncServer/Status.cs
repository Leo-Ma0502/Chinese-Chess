using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
namespace AsyncServer
{
    public class Status
    {
        public int row { get; set; }
        public int col { get; set; }
        public string? faction { get; set; }
        public string? division { get; set; }
        // public Status(int row, int col, string faction, string division)
        // {
        //     this.row = row;
        //     this.col = col;
        //     this.faction = faction;
        //     this.division = division;
        // }
    }
}
