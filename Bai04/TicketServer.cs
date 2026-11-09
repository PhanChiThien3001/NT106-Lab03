using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Bai04
{
    /*
     * GIAO THỨC (mỗi lệnh là 1 dòng UTF-8, các trường ngăn cách bởi '|'):
     *
     * Client -> Server
     *   GET_DATA
     *   BOOK|<họ tên>|<tên phim>|<phòng>|<ghế1,ghế2,...>
     *
     * Server -> Client
     *   DATA_BEGIN
     *   MOVIE|<tên phim>|<giá chuẩn>|<phòng1,phòng2,...>
     *   SOLD|<tên phim>|<phòng>|<ghế đã bán,...>
     *   DATA_END
     *   BOOK_OK|<họ tên>|<tên phim>|<phòng>|<ghế,...>|<tổng tiền>
     *   BOOK_FAIL|<lý do>
     *   SOLD_UPDATE|<tên phim>|<phòng>|<ghế vừa bán,...>     (broadcast cho TẤT CẢ client)
     *   ERROR|<thông báo>
     */

    internal class ClientConnection
    {
        private readonly object _writeLock = new object();

        public TcpClient Tcp { get; }
        public StreamReader Reader { get; }
        public StreamWriter Writer { get; }
        public string Endpoint { get; }

        public ClientConnection(TcpClient tcp)
        {
            Tcp = tcp;
            NetworkStream stream = tcp.GetStream();
            var utf8 = new UTF8Encoding(false);
            Reader = new StreamReader(stream, utf8);
            Writer = new StreamWriter(stream, utf8) { AutoFlush = true, NewLine = "\n" };
            Endpoint = tcp.Client.RemoteEndPoint?.ToString() ?? "unknown";
        }

        public void Send(string line)
        {
            lock (_writeLock) // nhiều thread có thể cùng gửi (xử lý lệnh + broadcast)
            {
                try { Writer.WriteLine(line); }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
            }
        }

        public void Close()
        {
            try { Tcp.Close(); } catch { }
        }
    }

    public class TicketServer
    {
        private readonly TicketStore _store;
        private readonly List<ClientConnection> _clients = new List<ClientConnection>();
        private readonly object _clientsLock = new object();
        private TcpListener _listener;
        private Thread _acceptThread;
        private volatile bool _running;

        public event Action<string> LogMessage;

        public TicketServer(TicketStore store)
        {
            _store = store;
        }

        public void Start(int port)
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _running = true;

            _acceptThread = new Thread(AcceptLoop) { IsBackground = true };
            _acceptThread.Start();
            Log("Server started on port " + port + "!");
        }

        public void Stop()
        {
            _running = false;
            try { _listener?.Stop(); } catch { }

            lock (_clientsLock)
            {
                foreach (ClientConnection c in _clients) c.Close();
                _clients.Clear();
            }
        }

        private void AcceptLoop()
        {
            while (_running)
            {
                TcpClient tcp;
                try { tcp = _listener.AcceptTcpClient(); }
                catch (SocketException) { break; }
                catch (ObjectDisposedException) { break; }

                var conn = new ClientConnection(tcp);
                lock (_clientsLock) { _clients.Add(conn); }
                Log("Connection accepted from " + conn.Endpoint);

                var thread = new Thread(() => HandleClient(conn)) { IsBackground = true };
                thread.Start();
            }
        }

        private void HandleClient(ClientConnection conn)
        {
            try
            {
                string line;
                while (_running && (line = conn.Reader.ReadLine()) != null)
                    ProcessCommand(conn, line);
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            finally
            {
                lock (_clientsLock) { _clients.Remove(conn); }
                conn.Close();
                Log("Client " + conn.Endpoint + " disconnected");
            }
        }

        private void ProcessCommand(ClientConnection conn, string line)
        {
            string[] p = line.Split('|');
            switch (p[0])
            {
                case "GET_DATA":
                    SendAllData(conn);
                    break;
                case "BOOK":
                    HandleBook(conn, p);
                    break;
                default:
                    conn.Send("ERROR|Lệnh không hợp lệ: " + p[0]);
                    break;
            }
        }

        private void SendAllData(ClientConnection conn)
        {
            conn.Send("DATA_BEGIN");
            foreach (Movie m in _store.Movies)
                conn.Send("MOVIE|" + m.Name + "|" + m.BasePrice + "|" + string.Join(",", m.Rooms));
            foreach (var entry in _store.SnapshotSold())
                conn.Send("SOLD|" + entry.Movie + "|" + entry.Room + "|" + string.Join(",", entry.Seats));
            conn.Send("DATA_END");
        }

        private void HandleBook(ClientConnection conn, string[] p)
        {
            if (p.Length != 5 || !int.TryParse(p[3], out int room))
            {
                conn.Send("BOOK_FAIL|Yêu cầu đặt vé sai định dạng.");
                return;
            }

            string movie = p[2];
            string seatsCsv = p[4];
            BookingResult result = _store.TryBook(p[1], movie, room, seatsCsv.Split(','));

            if (!result.Success)
            {
                conn.Send("BOOK_FAIL|" + result.Message);
                Log(conn.Endpoint + " đặt vé thất bại: " + result.Message);
                return;
            }

            string soldSeats = string.Join(",", result.Seats);
            conn.Send("BOOK_OK|" + result.Customer + "|" + movie + "|" + room + "|" + soldSeats + "|" + result.Total);
            Broadcast("SOLD_UPDATE|" + movie + "|" + room + "|" + soldSeats); // đồng bộ cho mọi quầy
            Log(result.Customer + " (" + conn.Endpoint + ") đặt " + soldSeats + " - " + movie +
                " - phòng " + room + " - " + SeatCatalog.FormatMoney(result.Total));
        }

        private void Broadcast(string line)
        {
            List<ClientConnection> snapshot;
            lock (_clientsLock) { snapshot = _clients.ToList(); }
            foreach (ClientConnection c in snapshot) c.Send(line);
        }

        private void Log(string message)
        {
            LogMessage?.Invoke(message);
        }
    }
}