using System.Net;
using System.Net.Sockets;
namespace AsyncServer
{
    public class Status
    {
        public int row { get; set; }
        public int col { get; set; }
        public string? faction { get; set; }
        public string? division { get; set; }
    }
}
