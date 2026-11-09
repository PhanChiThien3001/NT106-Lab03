using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Bai05
{
    // Giu nguyen file ClientForm.Designer.cs (rong). Giao dien duoc dung bang code trong BuildUi().
    public partial class ClientForm : Form
    {
        private readonly TextBox txtHost = new TextBox();
        private readonly TextBox txtPort = new TextBox();
        private readonly TextBox txtName = new TextBox();
        private readonly Button btnConnect = new Button();
        private readonly Button btnDisconnect = new Button();

        private readonly TextBox txtFood = new TextBox();
        private readonly Button btnAdd = new Button();
        private readonly ComboBox cboScope = new ComboBox();
        private readonly Button btnDelete = new Button();
        private readonly ListBox lstFood = new ListBox();
        private readonly Button btnRandom = new Button();
        private readonly Label lblResult = new Label();
        private readonly Label lblStatus = new Label();

        private FoodClient client;
        private string myName;
        private List<FoodItem> currentItems = new List<FoodItem>();

        public ClientForm()
        {
            InitializeComponent();
            BuildUi();
            SetConnectedState(false);
        }

        // ---------------------------------------------------------------- giao dien

        private void BuildUi()
        {
            Text = "Hôm nay ăn gì? - Client";
            ClientSize = new Size(600, 440);
            MinimumSize = new Size(620, 400);

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
            txtName.Size = new Size(100, 23);

            btnConnect.Text = "Connect";
            btnConnect.Location = new Point(450, 12);
            btnConnect.Size = new Size(65, 25);
            btnConnect.Click += BtnConnect_Click;

            btnDisconnect.Text = "Ngắt";
            btnDisconnect.Location = new Point(521, 12);
            btnDisconnect.Size = new Size(65, 25);
            btnDisconnect.Click += (s, e) => DisconnectClient();

            Label lblFood = new Label { Text = "Thêm món:", Location = new Point(12, 56), AutoSize = true };
            txtFood.Location = new Point(80, 53);
            txtFood.Size = new Size(420, 23);
            txtFood.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtFood.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; AddFood(); }
            };

            btnAdd.Text = "Thêm";
            btnAdd.Location = new Point(506, 52);
            btnAdd.Size = new Size(80, 25);
            btnAdd.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAdd.Click += (s, e) => AddFood();

            Label lblScope = new Label { Text = "Xem:", Location = new Point(12, 94), AutoSize = true };
            cboScope.Location = new Point(80, 91);
            cboScope.Size = new Size(160, 23);
            cboScope.DropDownStyle = ComboBoxStyle.DropDownList;
            cboScope.Items.AddRange(new object[] { "Món của tôi", "Cộng đồng" });
            cboScope.SelectedIndex = 0;
            cboScope.SelectedIndexChanged += (s, e) => RequestList();

            btnDelete.Text = "Xóa món đã chọn";
            btnDelete.Location = new Point(250, 90);
            btnDelete.Size = new Size(130, 25);
            btnDelete.Click += (s, e) => DeleteSelected();

            lstFood.Location = new Point(12, 124);
            lstFood.Size = new Size(574, 220);
            lstFood.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            btnRandom.Text = "Chọn ngẫu nhiên";
            btnRandom.Location = new Point(12, 358);
            btnRandom.Size = new Size(160, 38);
            btnRandom.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnRandom.Click += (s, e) => Send(FoodProtocol.Build(FoodProtocol.Random, CurrentScope()));

            lblResult.Location = new Point(185, 358);
            lblResult.Size = new Size(401, 38);
            lblResult.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
            lblResult.TextAlign = ContentAlignment.MiddleLeft;
            lblResult.ForeColor = Color.DarkGreen;
            lblResult.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

            lblStatus.Location = new Point(12, 410);
            lblStatus.Size = new Size(574, 20);
            lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            lblStatus.ForeColor = Color.Gray;

            Controls.AddRange(new Control[]
            {
                lblHost, txtHost, lblPort, txtPort, lblName, txtName, btnConnect, btnDisconnect,
                lblFood, txtFood, btnAdd, lblScope, cboScope, btnDelete, lstFood,
                btnRandom, lblResult, lblStatus
            });
        }

        private void SetConnectedState(bool connected)
        {
            txtHost.Enabled = !connected;
            txtPort.Enabled = !connected;
            txtName.Enabled = !connected;
            btnConnect.Enabled = !connected;
            btnDisconnect.Enabled = connected;

            txtFood.Enabled = connected;
            btnAdd.Enabled = connected;
            cboScope.Enabled = connected;
            btnDelete.Enabled = connected;
            lstFood.Enabled = connected;
            btnRandom.Enabled = connected;

            if (!connected)
            {
                lstFood.Items.Clear();
                currentItems.Clear();
                lblResult.Text = "";
            }
        }

        // ---------------------------------------------------------------- ket noi

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            string name = FoodProtocol.Clean(txtName.Text);
            int port;
            if (!int.TryParse(txtPort.Text, out port))
            {
                MessageBox.Show("Port không hợp lệ");
                return;
            }
            if (name.Length == 0)
            {
                MessageBox.Show("Hãy nhập tên của bạn");
                return;
            }

            FoodClient c = new FoodClient();
            // Cac su kien duoc goi tu thread nhan du lieu => chuyen ve thread giao dien
            c.LineReceived += line => RunOnUi(() => { if (c == client) HandleLine(line); });
            c.Disconnected += () => RunOnUi(() =>
            {
                if (c != client) return;
                client = null;
                SetConnectedState(false);
                SetStatus("Mất kết nối với server", true);
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
            btnConnect.Enabled = false; // cho server tra JOINOK
            Send(FoodProtocol.Build(FoodProtocol.Join, name));
        }

        private void DisconnectClient()
        {
            FoodClient c = client;
            client = null;
            if (c != null) c.Disconnect();
            SetConnectedState(false);
            SetStatus("Đã ngắt kết nối", false);
        }

        private void Send(string line)
        {
            if (client == null) return;
            try { client.Send(line); }
            catch (Exception ex) { SetStatus("Gửi thất bại: " + ex.Message, true); }
        }

        // ---------------------------------------------------------------- thao tac

        private string CurrentScope()
        {
            return cboScope.SelectedIndex == 1 ? FoodProtocol.All : FoodProtocol.Me;
        }

        private void RequestList()
        {
            Send(FoodProtocol.Build(FoodProtocol.List, CurrentScope()));
        }

        private void AddFood()
        {
            string food = FoodProtocol.Clean(txtFood.Text);
            if (food.Length == 0 || client == null) return;
            Send(FoodProtocol.Build(FoodProtocol.Add, food));
            txtFood.Clear();
            txtFood.Focus();
        }

        private void DeleteSelected()
        {
            int index = lstFood.SelectedIndex;
            if (index < 0 || index >= currentItems.Count)
            {
                MessageBox.Show("Hãy chọn một món trong danh sách");
                return;
            }

            FoodItem item = currentItems[index];
            if (item.Owner != myName)
            {
                MessageBox.Show("Bạn chỉ xóa được món do chính mình thêm");
                return;
            }
            Send(FoodProtocol.Build(FoodProtocol.Delete, item.Id.ToString()));
        }

        // ---------------------------------------------------------------- xu ly tin tu server

        private void HandleLine(string line)
        {
            string[] head = FoodProtocol.Split(line, 2);
            string rest = head.Length > 1 ? head[1] : "";

            switch (head[0])
            {
                case FoodProtocol.JoinOk:
                    SetConnectedState(true);
                    SetStatus("Đã kết nối với tên " + myName, false);
                    RequestList();
                    break;

                case FoodProtocol.Ok:
                    SetStatus(rest, false);
                    RequestList(); // lam moi danh sach sau khi them/xoa
                    break;

                case FoodProtocol.Err:
                    SetStatus(rest, true);
                    if (client != null && !btnDisconnect.Enabled) DisconnectClient(); // loi luc JOIN
                    break;

                case FoodProtocol.List:
                    {
                        string[] p = FoodProtocol.Split(rest, 2); // [scope, payload]
                        if (p.Length < 2 || p[0] != CurrentScope()) break; // bo qua ket qua cu
                        ShowList(p[0], p[1]);
                        break;
                    }

                case FoodProtocol.Random:
                    {
                        string[] p = FoodProtocol.Split(rest, 3); // [scope, owner, ten mon]
                        if (p.Length < 3) break;
                        lblResult.Text = p[2];
                        SetStatus("Gợi ý từ " + (p[0] == FoodProtocol.All ? "cộng đồng" : "danh sách của bạn")
                                  + " (món của " + p[1] + ")", false);
                        break;
                    }
            }
        }

        private void ShowList(string scope, string payload)
        {
            currentItems = new List<FoodItem>();
            lstFood.Items.Clear();

            if (payload.Length > 0)
            {
                foreach (string raw in payload.Split(FoodProtocol.ItemSeparator))
                {
                    string[] f = raw.Split(new[] { ':' }, 3); // id:owner:ten
                    long id;
                    if (f.Length < 3 || !long.TryParse(f[0], out id)) continue;

                    FoodItem item = new FoodItem { Id = id, Owner = f[1], Name = f[2] };
                    currentItems.Add(item);
                    lstFood.Items.Add(scope == FoodProtocol.All ? item.Name + "   (" + item.Owner + ")" : item.Name);
                }
            }
        }

        // ---------------------------------------------------------------- tien ich

        private void SetStatus(string text, bool isError)
        {
            lblStatus.ForeColor = isError ? Color.Red : Color.Gray;
            lblStatus.Text = text;
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
            FoodClient c = client;
            client = null;
            if (c != null) c.Disconnect();
            base.OnFormClosing(e);
        }
    }
}
