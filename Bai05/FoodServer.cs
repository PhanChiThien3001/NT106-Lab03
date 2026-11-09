using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Bai05
{
    public class FoodSession
    {
        public TcpClient Tcp { get; private set; }
        public StreamWriter Writer { get; private set; }
        public string Name { get; set; } // null cho den khi JOIN

        public FoodSession(TcpClient tcp, StreamWriter writer)
        {
            Tcp = tcp;
            Writer = writer;
        }

        public void Send(string line)
        {
            lock (Writer)
            {
                Writer.WriteLine(line);
            }
        }
    }

    public class FoodServer
    {
        private readonly FoodRepository repository;
        private TcpListener listener;
        private bool isRunning;

        private readonly List<FoodSession> sessions = new List<FoodSession>();
        private readonly object sessionsLock = new object();

        public event Action<string> LogReceived;

        public FoodServer(FoodRepository repository)
        {
            this.repository = repository;
        }

        private void Log(string text)
        {
            Action<string> handler = LogReceived;
            if (handler != null) handler(text);
        }

        public void Start(int port)
        {
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            isRunning = true;

            Thread acceptThread = new Thread(AcceptLoop);
            acceptThread.IsBackground = true;
            acceptThread.Start();

            Log("Server started on port " + port);
        }

        public void Stop()
        {
            isRunning = false;
            if (listener != null) listener.Stop();

            lock (sessionsLock)
            {
                foreach (FoodSession s in sessions)
                {
                    try { s.Tcp.Close(); } catch { }
                }
                sessions.Clear();
            }
        }

        private void AcceptLoop()
        {
            while (isRunning)
            {
                try
                {
                    TcpClient tcp = listener.AcceptTcpClient();
                    Log("New connection from " + tcp.Client.RemoteEndPoint);

                    Thread clientThread = new Thread(() => HandleClient(tcp));
                    clientThread.IsBackground = true;
                    clientThread.Start();
                }
                catch (SocketException) { break; }
                catch (ObjectDisposedException) { break; }
                catch (InvalidOperationException) { break; }
            }
        }

        private void HandleClient(TcpClient tcp)
        {
            FoodSession session = null;
            try
            {
                NetworkStream stream = tcp.GetStream();
                StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false));
                writer.AutoFlush = true;
                session = new FoodSession(tcp, writer);
                lock (sessionsLock) { sessions.Add(session); }

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    try
                    {
                        ProcessLine(session, line);
                    }
                    catch (IOException) { throw; } // client rot mang => thoat vong lap
                    catch (ObjectDisposedException) { throw; }
                    catch (Exception ex)
                    {
                        Log("Lỗi xử lý lệnh '" + line + "': " + ex.Message);
                        session.Send(FoodProtocol.Build(FoodProtocol.Err, "Lỗi server: " + ex.Message));
                    }
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            finally
            {
                lock (sessionsLock) { sessions.Remove(session); }
                if (session != null && session.Name != null) Log(session.Name + " disconnected");
                try { tcp.Close(); } catch { }
            }
        }

        private void ProcessLine(FoodSession session, string line)
        {
            string[] head = FoodProtocol.Split(line, 2);
            string command = head[0];
            string rest = head.Length > 1 ? head[1] : "";

            if (command == FoodProtocol.Join)
            {
                string name = FoodProtocol.Clean(rest);
                if (name.Length == 0)
                {
                    session.Send(FoodProtocol.Build(FoodProtocol.Err, "Tên không hợp lệ"));
                    return;
                }
                session.Name = name;
                Log(name + " joined");
                session.Send(FoodProtocol.Build(FoodProtocol.JoinOk, name));
                return;
            }

            if (session.Name == null)
            {
                session.Send(FoodProtocol.Build(FoodProtocol.Err, "Bạn cần JOIN trước"));
                return;
            }

            switch (command)
            {
                case FoodProtocol.Add:
                    {
                        string food = FoodProtocol.Clean(rest);
                        if (food.Length == 0)
                        {
                            session.Send(FoodProtocol.Build(FoodProtocol.Err, "Tên món không được để trống"));
                            return;
                        }
                        repository.Add(food, session.Name);
                        Log(session.Name + " thêm món: " + food);
                        session.Send(FoodProtocol.Build(FoodProtocol.Ok, "Đã thêm món \"" + food + "\""));
                        break;
                    }

                case FoodProtocol.List:
                    SendList(session, NormalizeScope(rest));
                    break;

                case FoodProtocol.Random:
                    HandleRandom(session, NormalizeScope(rest));
                    break;

                case FoodProtocol.Delete:
                    {
                        long id;
                        if (long.TryParse(rest, out id) && repository.Delete(id, session.Name))
                        {
                            Log(session.Name + " xóa món #" + id);
                            session.Send(FoodProtocol.Build(FoodProtocol.Ok, "Đã xóa món"));
                        }
                        else
                        {
                            session.Send(FoodProtocol.Build(FoodProtocol.Err,
                                "Không xóa được (món không tồn tại hoặc không phải của bạn)"));
                        }
                        break;
                    }
            }
        }

        private static string NormalizeScope(string text)
        {
            return text.Trim().ToUpperInvariant() == FoodProtocol.All ? FoodProtocol.All : FoodProtocol.Me;
        }

        private void SendList(FoodSession session, string scope)
        {
            List<FoodItem> items = repository.GetAll(scope == FoodProtocol.All ? null : session.Name);
            string payload = string.Join(FoodProtocol.ItemSeparator.ToString(),
                items.Select(i => i.Id + ":" + i.Owner + ":" + i.Name));
            session.Send(FoodProtocol.Build(FoodProtocol.List, scope, payload));
        }

        private void HandleRandom(FoodSession session, string scope)
        {
            FoodItem item = repository.GetRandom(scope == FoodProtocol.All ? null : session.Name);
            if (item == null)
            {
                session.Send(FoodProtocol.Build(FoodProtocol.Err, "Chưa có món nào trong danh sách"));
                return;
            }
            Log(session.Name + " random (" + scope + ") => " + item.Name);
            session.Send(FoodProtocol.Build(FoodProtocol.Random, scope, item.Owner, item.Name));
        }
    }
}
