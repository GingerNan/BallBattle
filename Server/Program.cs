using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    internal class Program
    {
        public static Server server;
        
        static void Main(string[] args)
        {
            server = new Server();
            
            Console.ReadLine();
        }
    }
}
