using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Client
{
    public partial class Client : Form
    {
        public Client()
        {
            InitializeComponent();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Player2 player2 = new();
            Thread thread = new(player2.StartClient);
            thread.Start();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Player1 player1 = new();
            Thread thread = new(player1.StartClient);
            thread.Start();
        }
    }
}