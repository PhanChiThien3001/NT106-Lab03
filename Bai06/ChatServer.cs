using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Bai06
{
    // Đại diện cho 1 client đang kết nối
    public class ClientSession
    {
        public TcpClient Tcp { get; }
        public StreamWriter Writer { get; }
        public string Name { get; set; }

        public ClientSession(TcpClient tcp, StreamWriter writer)
        {
            Tcp = tcp;
            Writer = writer;
        }

        // lock vì nhiều thread có thể cùng gửi cho 1 client
        public void Send(string line)
        {
            lock (Writer)
            {
                Writer.WriteLine(line);
            }
        }
    }

    public class ChatServer
    {
        private TcpListener listener;
        private bool isRunning;

        // Danh sách người online: tên -> phiên kết nối
        private readonly Dictionary<string, ClientSession> sessions =
            new Dictionary<string, ClientSession>();
        private readonly object sessionsLock = new object();

        // Form sẽ đăng ký sự kiện này để hiển thị log
        public event Action<string> LogReceived;

        private void Log(string text)
        {
            LogReceived?.Invoke(text);
        }

        public void Start(int port)
        {
            // IPAddress.Any: cho phép máy khác trong LAN kết nối tới
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            isRunning = true;

            Thread acceptThread = new Thread(AcceptLoop);
            acceptThread.IsBackground = true; // đóng form thì thread tự dừng
            acceptThread.Start();

            Log("Server started on port " + port);
        }

        public void Stop()
        {
            isRunning = false;
            listener?.Stop(); // làm AcceptTcpClient ném exception để thoát vòng lặp

            lock (sessionsLock)
            {
                foreach (ClientSession s in sessions.Values)
                    s.Tcp.Close();
                sessions.Clear();
            }
        }

        // Vòng lặp chờ client mới. Mỗi client được giao cho 1 thread riêng.
        private void AcceptLoop()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient tcp = listener.AcceptTcpClient(); // chờ tại đây
                    Log("New connection from " + tcp.Client.RemoteEndPoint);

                    Thread clientThread = new Thread(() => HandleClient(tcp));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
                catch (SocketException) { break; }
                catch (ObjectDisposedException) { break; }
            }
        }

        // Chạy trên thread riêng của từng client
        private void HandleClient(TcpClient tcp)
        {
            ClientSession session = null;
            try
            {
                NetworkStream stream = tcp.GetStream();
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false));
                writer.AutoFlush = true;
                session = new ClientSession(tcp, writer);

                string line;
                // ReadLine trả về null khi client ngắt kết nối
                while ((line = reader.ReadLine()) != null)
                {
                    ProcessLine(session, line);
                    if (line == Protocol.Leave) break;
                }
            }
            catch (IOException) { }          // client rớt mạng đột ngột
            catch (ObjectDisposedException) { }
            finally
            {
                RemoveSession(session);
                tcp.Close();
            }
        }

        // Phân tích 1 dòng lệnh và xử lý
        private void ProcessLine(ClientSession session, string line)
        {
            string[] head = Protocol.Split(line, 2);
            string command = head[0];
            string rest = head.Length > 1 ? head[1] : "";

            switch (command)
            {
                case Protocol.Join:
                    HandleJoin(session, rest.Trim());
                    break;

                case Protocol.Msg:
                    if (session.Name == null) return; // chưa JOIN thì bỏ qua
                    Log(session.Name + ": " + rest);
                    Broadcast(Protocol.Build(Protocol.Msg, session.Name, rest));
                    break;

                case Protocol.Pm:
                    if (session.Name == null) return;
                    HandlePrivateMessage(session, rest);
                    break;
            }
        }

        private void HandleJoin(ClientSession session, string name)
        {
            if (name.Length == 0)
            {
                session.Send(Protocol.Build(Protocol.JoinFail, "Tên không được để trống"));
                return;
            }

            lock (sessionsLock)
            {
                if (sessions.ContainsKey(name))
                {
                    session.Send(Protocol.Build(Protocol.JoinFail, "Tên đã có người dùng"));
                    return;
                }
                session.Name = name;
                sessions[name] = session;
            }

            Log(name + " joined");
            Broadcast(Protocol.Build(Protocol.Sys, name + " đã vào phòng"));
            BroadcastUserList();
        }

        private void HandlePrivateMessage(ClientSession from, string rest)
        {
            string[] parts = Protocol.Split(rest, 2); // [người nhận, nội dung]
            if (parts.Length < 2) return;

            ClientSession target;
            lock (sessionsLock)
            {
                sessions.TryGetValue(parts[0], out target);
            }

            if (target == null)
            {
                from.Send(Protocol.Build(Protocol.Sys, "Không tìm thấy người dùng " + parts[0]));
                return;
            }

            Log(from.Name + " -> " + parts[0] + " (riêng): " + parts[1]);
            string packet = Protocol.Build(Protocol.Pm, from.Name, parts[1]);
            target.Send(packet);
            // Báo lại cho người gửi để họ thấy tin của mình (kèm tên người nhận)
            from.Send(Protocol.Build(Protocol.Pm, "Bạn → " + parts[0], parts[1]));
        }

        // Gửi 1 dòng cho tất cả mọi người
        private void Broadcast(string line)
        {
            List<ClientSession> snapshot;
            lock (sessionsLock)
            {
                snapshot = sessions.Values.ToList(); // sao chép để không giữ lock lúc gửi
            }

            foreach (ClientSession s in snapshot)
            {
                try { s.Send(line); }
                catch (IOException) { } // người này vừa rớt, sẽ bị dọn ở HandleClient
            }
        }

        private void BroadcastUserList()
        {
            string names;
            lock (sessionsLock)
            {
                names = string.Join(",", sessions.Keys);
            }
            Broadcast(Protocol.Build(Protocol.Users, names));
        }

        private void RemoveSession(ClientSession session)
        {
            if (session == null || session.Name == null) return;

            lock (sessionsLock)
            {
                sessions.Remove(session.Name);
            }

            Log(session.Name + " left");
            Broadcast(Protocol.Build(Protocol.Sys, session.Name + " đã rời phòng"));
            BroadcastUserList();
        }
    }
}