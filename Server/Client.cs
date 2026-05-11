using System;
using System.Net.Sockets;
using System.Text;

namespace Server
{
    public class Client
    {
        private Socket Socket;
        public string PlayerId { get; private set; }
        
        public Client(Socket socket)
        {
            Socket = socket;
            // 唯一标识
            PlayerId = Guid.NewGuid().ToString();
            StartReceiving();
        }
        
        // 持续接收消息
        private void StartReceiving()
        {
            try
            {
                byte[] buffer = new byte[1024];
                Socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, ReceiveCallback, buffer.Length);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                int bytesRead = Socket.EndReceive(ar);
                if (bytesRead > 0)
                {
                    byte[] buffer = ar.AsyncState as byte[];
                    
                    string msg = Encoding.UTF8.GetString(buffer);
                    Console.WriteLine(msg);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}