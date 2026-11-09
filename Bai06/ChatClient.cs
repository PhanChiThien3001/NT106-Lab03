using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Bai06
{
    /// <summary>Phia client: ket noi, gui dong lenh, nhan dong tra loi tren 1 thread rieng.</summary>
    public class ChatClient
    {
        private TcpClient tcp;
        private StreamWriter writer;

        // Duoc goi tu thread nhan du lieu => Form phai Invoke de sua giao dien
        public event Action<string> LineReceived;
        public event Action Disconnected;

        public void Connect(string host, int port)
        {
            tcp = new TcpClient();
            tcp.Connect(host, port);

            NetworkStream stream = tcp.GetStream();
            writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.AutoFlush = true;
            StreamReader reader = new StreamReader(stream, Encoding.UTF8);

            Thread receiveThread = new Thread(() => ReceiveLoop(reader));
            receiveThread.IsBackground = true;
            receiveThread.Start();
        }

        public void Send(string line)
        {
            lock (writer)
            {
                writer.WriteLine(line);
            }
        }

        private void ReceiveLoop(StreamReader reader)
        {
            try
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Action<string> handler = LineReceived;
                    if (handler != null) handler(line);
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            finally
            {
                Action onDisconnected = Disconnected;
                if (onDisconnected != null) onDisconnected();
            }
        }

        public void Disconnect()
        {
            try { Send(Protocol.Leave); } catch { }
            try { tcp.Close(); } catch { }
        }
    }
}
