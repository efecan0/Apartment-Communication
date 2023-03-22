using System;
using System.Drawing;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Xml.Linq;
using Timer = System.Windows.Forms.Timer;
using Newtonsoft.Json.Linq;


namespace AsyncTcpClient
{
    public partial class Form1 : Form
    {

        private TextBox newText;
        private TextBox conStatus;
        private ListBox results;
        private Socket client;
        private byte[] data = new byte[1024];
        private int size = 1024;
        private Label label;
        private TextBox textName;
        private Label label3;



        public Form1()
        {
            Timer timer = new Timer();
            timer.Interval = 1000; // 1 second
            timer.Tick += new EventHandler(Timer_Tick);
            timer.Start();

            InitializeComponent();
            Text = " CinsApartment Client";
            Size = new Size(700, 500);


            Label label3 = new Label();
            label3.Parent = this;
            label3.Text = "";
            label3.AutoSize = true;
            label3.Location = new Point(10, 40);
            this.label3 = label3;

            Label label = new Label();
            label.Parent = this;
            label.Text = "";
            label.AutoSize = true;
            label.Location = new Point(10, 60);
            this.label = label;

            Label label1 = new Label();
            label1.Parent = this;
            label1.Text = "Enter text:";
            label1.AutoSize = true;
            label1.Location = new Point(10, 80);

            TextBox textName = new TextBox();
            textName.Parent = this;
            textName.Size = new Size(100, 2 * Font.Height);
            textName.Location = new Point((this.Width - textName.Width) / 2, 10);

            this.textName = textName;

            Label labelName = new Label();
            labelName.Parent = this;
            labelName.Text = "Name:";
            labelName.AutoSize = true;
            labelName.Location = new Point(textName.Left - labelName.Width - 10, textName.Top);

            newText = new TextBox();
            newText.Parent = this;
            newText.Size = new Size(200, 2 * Font.Height);
            newText.Location = new Point(10, 105);

            results = new ListBox();
            results.Parent = this;
            results.Location = new Point(10, 145);
            results.Size = new Size(360, 18 * Font.Height);

            Label label2 = new Label();
            label2.Parent = this;
            label2.Text = "Connection Status:";
            label2.AutoSize = true;
            label2.Location = new Point(10, 438);

            conStatus = new TextBox();
            conStatus.Parent = this;
            conStatus.Text = "Disconnected";
            conStatus.Size = new Size(200, 2 * Font.Height);
            conStatus.Location = new Point(140, 435);

            Button sendit = new Button();
            sendit.Parent = this;
            sendit.Text = "Send";
            sendit.BackColor = Color.Yellow;
            sendit.Location = new Point(220, 102);
            sendit.Size = new Size(5 * Font.Height, 2 * Font.Height);
            sendit.Click += new EventHandler(ButtonSendOnClick);

            Button connect = new Button();
            connect.Parent = this;
            connect.Text = "Connect";
            connect.BackColor = Color.Green;
            connect.Location = new Point(295, 70);
            connect.Size = new Size(6 * Font.Height, 2 * Font.Height);
            connect.Click += new EventHandler(ButtonConnectOnClick);

            Button discon = new Button();
            discon.Parent = this;
            discon.Text = "Disconnect";
            discon.BackColor = Color.Red;
            discon.Location = new Point(295, 102);
            discon.Size = new Size(6 * Font.Height, 2 * Font.Height);
            discon.Click += new EventHandler(ButtonDisconOnClick);
        }
        void ButtonConnectOnClick(object obj, EventArgs ea)
        {
            conStatus.Text = "Connecting...";
            Socket newsock = new Socket(AddressFamily.InterNetwork,
                                  SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint iep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 9040);
            newsock.BeginConnect(iep, new AsyncCallback(Connected), newsock);
        }

        void ButtonSendOnClick(object obj, EventArgs ea)
        {
            // Get the name and message from the text boxes
            string name = textName.Text;
            string message = newText.Text;

            // Clear the newText text box
            newText.Clear();

            // Send the message in the format "name: message"
            byte[] data = Encoding.ASCII.GetBytes(name + ": " + message);
            client.BeginSend(data, 0, data.Length, SocketFlags.None,
                             new AsyncCallback(SendData), client);
        }

        void ButtonDisconOnClick(object obj, EventArgs ea)
        {
            client.Close();
            conStatus.Text = "Disconnected";
        }

        void Connected(IAsyncResult iar)
        {
            client = (Socket)iar.AsyncState;
            try
            {
                client.EndConnect(iar);
                conStatus.Text = "Connected to: " + client.RemoteEndPoint.ToString();
                client.BeginReceive(data, 0, size, SocketFlags.None,
                              new AsyncCallback(ReceiveData), client);
            }
            catch (SocketException)
            {
                conStatus.Text = "Error connecting";
            }
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (client != null && client.Connected)
            {
                client.BeginReceive(data, 0, size, SocketFlags.None,
                              new AsyncCallback(ReceiveData), client);
            }
        }

        void ReceiveData(IAsyncResult iar)
        {
            try
            {
                int recv = client.EndReceive(iar);
                if (recv == 0)
                {
                    client.Close();
                    return;
                }
                string stringData = Encoding.ASCII.GetString(data, 0, recv);

                if (stringData.StartsWith("{\"Data"))
                {
                    // Parse the received data as a JSON object
                    JObject obj = JObject.Parse(stringData);

                    // Extract the values of the App_Temp and Description fields
                    int appTemp = (int)obj["Data"][0]["App_Temp"];
                    string description = (string)obj["Data"][0]["Weather"]["Description"];

                    // Display the values in the label
                    label.Text = $"Temprature ºC: {appTemp}, Weather Condition: {description}";

                }else if (stringData.StartsWith("{\"data"))
                {

                    JObject obj = JObject.Parse(stringData);
                    double dollarToTry = (double)obj["data"]["TRY"];

                    label3.Text = $"1 Dollar: {dollarToTry} TL";

                }else
                {
                    // Add the received data to the list box
                    results.Items.Add(stringData);
                }

                client.BeginReceive(data, 0, size, SocketFlags.None,
                                    new AsyncCallback(ReceiveData), client);
            }
            catch (ObjectDisposedException)
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "SGSclient",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        void SendData(IAsyncResult iar)
        {
            Socket remote = (Socket)iar.AsyncState;
            int sent = remote.EndSend(iar);
            remote.BeginReceive(data, 0, size, SocketFlags.None,
                          new AsyncCallback(ReceiveData), remote);
        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void backgroundWorker1_DoWork(object sender, System.ComponentModel.DoWorkEventArgs e)
        {

        }
    }
}