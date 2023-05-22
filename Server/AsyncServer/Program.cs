using System.Net;

namespace AsyncServer
{
    class Entry
    {
        static async Task Main(string[] args)
        {
            Server server = new(IPAddress.Loopback, 8081, new object[10, 9]);

            await server.Serve();
        }
    }
}


