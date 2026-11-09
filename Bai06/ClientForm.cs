using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Bai06
{
    // Giu nguyen file ClientForm.Designer.cs (rong). Giao dien duoc dung bang code trong BuildUi().
    public partial class ClientForm : Form
    {
        private const string AllLabel = "[Tất cả]";
        private const int MaxFileBytes = 3 * 1024 * 1024; // gioi han 3 MB cho file/anh

        private readonly TextBox txtHost = new TextBox();
        private readonly TextBox txtPort = new TextBox();
        private readonly TextBox txtName = new TextBox();
        private readonly Button btnConnect = new Button();
        private readonly Button btnDisconnect = new Button();
        private readonly RichTextBox rtbChat = new RichTextBox();
        private readonly ListBox lstUsers = new ListBox();
        private readonly Label lblTarget = new Label();
        private readonly TextBox txtMessage = new TextBox();
        private readonly Button btnFile = new Button();
        private readonly Button btnSend = new Button();

        private ChatClient client;
        private string myName;

        public ClientForm()
        {
            InitializeComponent();
            BuildUi();
            SetConnectedState(false);
        }

        // ---------------------------------------------------------------- giao dien

        private void BuildUi()
        {
            Text = "Chat Client";
            ClientSize = new Size(760, 520);
            MinimumSize = new Size(700, 420);

            Label lblHost = new Label { Text = "IP Server:", Location = new Point(12, 16), AutoSize = true };
            txtHost.Location = new Point(80, 13);
            txtHost.Size = new Size(115, 23);
            txtHost.Text = "127.0.0.1";

            Label lblPort = new Label { Text = "Port:", Location = new Point(205, 16), AutoSize = true };
            txtPort.Location = new Point(240, 13);
            txtPort.Size = new Size(55, 23);
            txtPort.Text = "8080";

            Label lblName = new Label { Text = "Tên:", Location = new Point(305, 16), AutoSize = true };
            txtName.Location = new Point(340, 13);
            txtName.Size = new Size(110, 23);

            btnConnect.Text = "Connect";
            btnConnect.Location = new Point(460, 12);
            btnConnect.Size = new Size(80, 25);
            btnConnect.Click += BtnConnect_Click;

            btnDisconnect.Text = "Disconnect";
            btnDisconnect.Location = new Point(546, 12);
            btnDisconnect.Size = new Size(90, 25);
            btnDisconnect.Click += (s, e) => DisconnectClient();

            rtbChat.Location = new Point(12, 48);
            rtbChat.Size = new Size(560, 390);
            rtbChat.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            rtbChat.ReadOnly = true;
            rtbChat.BackColor = Color.White;

            Label lblUsers = new Label { Text = "Người tham gia", Location = new Point(584, 50), AutoSize = true };
            lblUsers.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lstUsers.Location = new Point(584, 70);
            lstUsers.Size = new Size(164, 368);
            lstUsers.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Right;
            lstUsers.SelectedIndexChanged += (s, e) => UpdateTargetLabel();

            lblTarget.Location = new Point(12, 450);
            lblTarget.AutoSize = true;
            lblTarget.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

            txtMessage.Location = new Point(12, 478);
            txtMessage.Size = new Size(430, 23);
            txtMessage.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            txtMessage.KeyDown += TxtMessage_KeyDown;

            btnFile.Text = "...";
            btnFile.Location = new Point(448, 477);
            btnFile.Size = new Size(40, 25);
            btnFile.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnFile.Click += BtnFile_Click;

            btnSend.Text = "Send";
            btnSend.Location = new Point(494, 477);
            btnSend.Size = new Size(78, 25);
            btnSend.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnSend.Click += (s, e) => SendMessage();

            Controls.AddRange(new Control[]
            {
                lblHost, txtHost, lblPort, txtPort, lblName, txtName, btnConnect, btnDisconnect,
                rtbChat, lblUsers, lstUsers, lblTarget, txtMessage, btnFile, btnSend
            });
        }

        private void SetConnectedState(bool connected)
        {
            txtHost.Enabled = !connected;
            txtPort.Enabled = !connected;
            txtName.Enabled = !connected;
            btnConnect.Enabled = !connected;
            btnDisconnect.Enabled = connected;

            txtMessage.Enabled = connected;
            btnSend.Enabled = connected;
            btnFile.Enabled = connected;
            lstUsers.Enabled = connected;

            if (!connected) lstUsers.Items.Clear();
            UpdateTargetLabel();
        }

        // ---------------------------------------------------------------- ket noi

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            int port;
            if (!int.TryParse(txtPort.Text, out port))
            {
                MessageBox.Show("Port không hợp lệ");
                return;
            }
            if (name.Length == 0 || name.Contains("|") || name.Contains(","))
            {
                MessageBox.Show("Hãy nhập tên (không chứa ký tự '|' hoặc ',')");
                return;
            }

            ChatClient c = new ChatClient();
            // Cac su kien duoc goi tu thread nhan du lieu => chuyen ve thread giao dien bang RunOnUi
            c.LineReceived += line => RunOnUi(() => { if (c == client) HandleLine(line); });
            c.Disconnected += () => RunOnUi(() =>
            {
                if (c != client) return;
                client = null;
                SetConnectedState(false);
                AppendChat("Mất kết nối với server", Color.Red);
            });

            try
            {
                c.Connect(txtHost.Text.Trim(), port);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không kết nối được: " + ex.Message);
                return;
            }

            client = c;
            myName = name;
            btnConnect.Enabled = false; // cho server tra JOINOK / JOINFAIL
            Send(Protocol.Build(Protocol.Join, name));
        }

        private void DisconnectClient()
        {
            ChatClient c = client;
            client = null;
            if (c != null) c.Disconnect();
            SetConnectedState(false);
        }

        private void Send(string line)
        {
            if (client == null) return;
            try { client.Send(line); }
            catch (Exception ex)
            {
                AppendChat("Gửi thất bại: " + ex.Message, Color.Red);
            }
        }

        // ---------------------------------------------------------------- xu ly tin tu server

        private void HandleLine(string line)
        {
            string[] head = Protocol.Split(line, 2);
            string rest = head.Length > 1 ? head[1] : "";

            switch (head[0])
            {
                case Protocol.JoinOk:
                    SetConnectedState(true);
                    AppendChat("Đã vào phòng với tên " + myName, Color.Gray);
                    break;

                case Protocol.JoinFail:
                    MessageBox.Show(rest, "Không vào được phòng");
                    DisconnectClient();
                    break;

                case Protocol.Msg:
                    {
                        string[] p = Protocol.Split(rest, 2); // [nguoi gui, noi dung]
                        if (p.Length < 2) break;
                        if (p[0] == myName) AppendChat("Tôi: " + p[1], Color.Blue);
                        else AppendChat(p[0] + ": " + p[1], Color.Black);
                        break;
                    }

                case Protocol.Pm:
                    {
                        string[] p = Protocol.Split(rest, 2);
                        if (p.Length < 2) break;
                        AppendChat("[Riêng] " + p[0] + ": " + p[1], Color.Purple);
                        break;
                    }

                case Protocol.Sys:
                    AppendChat(rest, Color.Gray);
                    break;

                case Protocol.Users:
                    UpdateUsers(rest);
                    break;

                case Protocol.FileMsg:
                    {
                        string[] p = Protocol.Split(rest, 3); // [nguoi gui, ten file, base64]
                        if (p.Length < 3) break;
                        ReceiveFile(p[0], p[1], p[2]);
                        break;
                    }
            }
        }

        private void UpdateUsers(string csv)
        {
            string selected = SelectedTarget();

            lstUsers.Items.Clear();
            lstUsers.Items.Add(AllLabel);
            foreach (string name in csv.Split(','))
            {
                if (name.Length > 0 && name != myName) lstUsers.Items.Add(name);
            }

            int index = selected == null ? 0 : lstUsers.Items.IndexOf(selected);
            lstUsers.SelectedIndex = index >= 0 ? index : 0;
        }

        // null = gui cho tat ca
        private string SelectedTarget()
        {
            if (lstUsers.SelectedIndex <= 0) return null;
            return (string)lstUsers.SelectedItem;
        }

        private void UpdateTargetLabel()
        {
            string target = SelectedTarget();
            lblTarget.Text = "Gửi tới: " + (target ?? AllLabel) + "   (chọn tên bên phải để nhắn riêng)";
        }

        // ---------------------------------------------------------------- gui tin / file

        private void TxtMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                SendMessage();
            }
        }

        private void SendMessage()
        {
            string text = txtMessage.Text.Trim();
            if (text.Length == 0 || client == null) return;

            string target = SelectedTarget();
            if (target == null) Send(Protocol.Build(Protocol.Msg, text));
            else Send(Protocol.Build(Protocol.Pm, target, text));

            txtMessage.Clear();
            txtMessage.Focus();
        }

        private void BtnFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Ảnh và văn bản|*.jpg;*.jpeg;*.png;*.txt";
                if (dialog.ShowDialog() != DialogResult.OK) return;

                byte[] data = File.ReadAllBytes(dialog.FileName);
                if (data.Length > MaxFileBytes)
                {
                    MessageBox.Show("File quá lớn (tối đa 3 MB)");
                    return;
                }

                string target = SelectedTarget() ?? Protocol.All;
                string fileName = Path.GetFileName(dialog.FileName);
                Send(Protocol.Build(Protocol.FileMsg, target, fileName, Convert.ToBase64String(data)));
                AppendChat("Bạn đã gửi file " + fileName + " tới " + (target == Protocol.All ? AllLabel : target), Color.DarkGreen);
            }
        }

        private void ReceiveFile(string from, string fileName, string base64)
        {
            byte[] data;
            try { data = Convert.FromBase64String(base64); }
            catch (FormatException) { return; }

            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            AppendChat(from + " gửi file " + fileName + " (" + (data.Length / 1024) + " KB)", Color.DarkGreen);

            if (ext == ".jpg" || ext == ".jpeg" || ext == ".png")
            {
                ShowImage(from, fileName, data);
            }
            else if (ext == ".txt")
            {
                DialogResult answer = MessageBox.Show(from + " gửi file " + fileName + ". Lưu về máy?",
                    "Nhận file", MessageBoxButtons.YesNo);
                if (answer == DialogResult.Yes) SaveBytes(fileName, data);
            }
        }

        private void ShowImage(string from, string fileName, byte[] data)
        {
            Form preview = new Form();
            preview.Text = fileName + " - từ " + from;
            preview.ClientSize = new Size(500, 420);

            PictureBox box = new PictureBox();
            box.Dock = DockStyle.Fill;
            box.SizeMode = PictureBoxSizeMode.Zoom;
            box.Image = Image.FromStream(new MemoryStream(data)); // stream phai mo suot doi cua anh

            Button btnSave = new Button();
            btnSave.Text = "Lưu ảnh";
            btnSave.Dock = DockStyle.Bottom;
            btnSave.Click += (s, e) => SaveBytes(fileName, data);

            preview.Controls.Add(box);
            preview.Controls.Add(btnSave);
            preview.Show(this);
        }

        private void SaveBytes(string fileName, byte[] data)
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.FileName = Path.GetFileName(fileName);
                if (dialog.ShowDialog() == DialogResult.OK) File.WriteAllBytes(dialog.FileName, data);
            }
        }

        // ---------------------------------------------------------------- tien ich

        private void AppendChat(string text, Color color)
        {
            rtbChat.SelectionStart = rtbChat.TextLength;
            rtbChat.SelectionLength = 0;
            rtbChat.SelectionColor = color;
            rtbChat.AppendText(text + Environment.NewLine);
            rtbChat.ScrollToCaret();
        }

        // Chuyen thao tac ve thread giao dien (thread-safe call)
        private void RunOnUi(Action action)
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { Invoke(action); }
                catch (ObjectDisposedException) { }
                catch (InvalidOperationException) { }
            }
            else action();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            ChatClient c = client;
            client = null;
            if (c != null) c.Disconnect();
            base.OnFormClosing(e);
        }
        private void ClientForm_Load(object sender, EventArgs e) { }
    }
}
