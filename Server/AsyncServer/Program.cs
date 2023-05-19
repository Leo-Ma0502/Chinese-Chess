using System.Net;

namespace AsyncServer
{
    class Entry
    {
        static async Task Main(string[] args)
        {
            Server server = new(IPAddress.Loopback, 8081);

            await server.Serve();
        }
    }
}


