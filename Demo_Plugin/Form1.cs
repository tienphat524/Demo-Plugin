using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Data.SqlClient;
using System.IO.Ports;
using System.Globalization;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net.Http.Headers;

namespace Demo_Plugin
{
    public partial class Form1 : Form
    {
        //server=MUNESH-PC;database=windowapp;UID=sa;password=123
        string strCon = @"Data Source=NHATTAN;Initial Catalog=databaseMark;Integrated Security=True";
        string globalName, globalSize, globalNumber, globalNumber1, globalOd, globalCode, globalCount;
        SqlConnection sqlCon = null;
        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }
        private TcpListener server;
        private Thread listenerThread;
        private List<Thread> clientThreads = new List<Thread>();
        private void StartServer()
        {
            try
            {
                int port = Convert.ToInt32(textBox2.Text);
                IPAddress ipAddress = IPAddress.Parse(textBox1.Text);

                server = new TcpListener(ipAddress, port);
                server.Start();

                listenerThread = new Thread(ListenForClients);
                listenerThread.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Start ServerError : " + ex.Message);
            }
        }

        private void ListenForClients()
        {
            try
            {
                while (true)
                {
                    TcpClient client = server.AcceptTcpClient();
                    Thread clientThread = new Thread(HandleClient);
                    clientThread.Start(client);
                }
            }
            catch /*(Exception ex)*/
            {
                //MessageBox.Show("Listen Client Error: " + ex.Message);
            }
        }
        private void HandleClient(object clientObj)
        {
            TcpClient client = (TcpClient)clientObj;

            NetworkStream stream = client.GetStream();
            byte[] data = new byte[1024];
            int bytesRead;

            while (true)
            {
                bytesRead = stream.Read(data, 0, data.Length);
                if (bytesRead == 0)
                {
                    break;
                }

                string message = Encoding.ASCII.GetString(data, 0, bytesRead);
                if (message.Length == 32)
                {
                    int str1 = "PTX-DT2290312-600".Length;
                    int str2 = 1; 
                    int str3 = 2; 
                    int str4 = 2; 
                    int str5 = 10;

                    if (message.Length == str1 + str2 + str3 + str4 + str5)
                    {
                        string name = message.Substring(0, str1);  // "PTX-DT2290312-600"
                        string size = message.Substring(str1, str2);  // "6"
                        string number = message.Substring(str1 + str2, str3);  // "32"
                        string number1 = message.Substring(str1 + str2 + str3, str4);  // "16"
                        string od = message.Substring(str1 + str2 + str3 + str4, str5);  // "1100066391"

                        addData1(name, size, number, number1, od);
                        updateData1();
                    }

                }
                else
                {
                    MessageBox.Show("Error Message");
                }

            }

            client.Close();
            lock (clientThreads)
            {
                clientThreads.Remove(Thread.CurrentThread);
            }
        }

        private void ConnectSQL()
        {
            try
            {
                if (sqlCon == null)
                    sqlCon = new SqlConnection(strCon);

                if (sqlCon.State == ConnectionState.Closed)
                {
                    sqlCon.Open();
                    //MessageBox.Show("Connect database succeed");
                }
                sqlCon.Close();


            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void DisconnectSQL()
        {
            if (sqlCon != null && sqlCon.State == ConnectionState.Open)
            {
                sqlCon.Close();
            }
            else { }
        }

        private void updateData1()
        {
            SqlCommand sqlCmd = new SqlCommand();
            sqlCmd.CommandType = CommandType.Text;
            sqlCmd.CommandText = "select * from tableMark" ;
            sqlCmd.Connection = sqlCon;
            int rowNum = 1;
            sqlCon.Open();
            SqlDataReader reader = sqlCmd.ExecuteReader();
            dataGridView1.Rows.Clear();

            DataTable dataMark = new DataTable();
            while (reader.Read())
            {
                dataGridView1.Rows.Add(new object[] {
                    rowNum,
                    reader["Ten hang"],
                    reader["Kich thuoc"],
                    reader["so luong cay"],
                    reader["so luong bo"],
                    reader["OD"],
                    });
                rowNum++;
            }
            sqlCon.Close();
        }


        private void addData1(string name, string size, string number, string number1, string od)
        {
            SqlCommand sqlCmd = new SqlCommand();
            sqlCmd.CommandType = CommandType.Text;
            sqlCmd.CommandText = "select * from tableMark";

            sqlCmd.Connection = sqlCon;

            string query = "insert tableMark Values(N'" + name + "'," + Convert.ToInt16(size) + "," + Convert.ToInt16(number) + "," + Convert.ToInt16(number1) + ",N'" + od + "')";

            SqlCommand sqlCommand = new SqlCommand(query, sqlCon);
            sqlCon.Open();
            sqlCommand.ExecuteNonQuery();
            sqlCon.Close();
        }

        private void InforMark()
        {
            textBox4.Text = globalName;
            textBox5.Text = globalSize;
            textBox6.Text = (Convert.ToInt16(globalNumber) * Convert.ToInt16(globalNumber1)).ToString();
            textBox7.Text = globalOd;
            textBox12.Text = "ALP " + globalOd;
            globalCount = textBox6.Text;
        }

        private void deleteRow1()
        {
            SqlCommand sqlCmd = new SqlCommand();
            sqlCmd.CommandType = CommandType.Text;
            sqlCmd.CommandText = "select * from tableMark";

            sqlCmd.Connection = sqlCon;
            //string name = globalName;
            string query = "delete from tableMark where [Ten hang] = N'" + globalName + "'";
            SqlCommand sqlCommand = new SqlCommand(query, sqlCon);
            sqlCon.Open();
            sqlCommand.ExecuteNonQuery();
            sqlCon.Close();
            updateData1();
        }

        private void deleteRow2()
        {
            SqlCommand sqlCmd = new SqlCommand();
            sqlCmd.CommandType = CommandType.Text;
            sqlCmd.CommandText = "select * from MarkHistory";

            sqlCmd.Connection = sqlCon;
            string query = "delete from MarkHistory where [Mark Code] = N'" + globalCode + "'";
            SqlCommand sqlCommand = new SqlCommand(query, sqlCon);
            sqlCon.Open();
            sqlCommand.ExecuteNonQuery();
            sqlCon.Close();
        }

        private void clearGlobalvar()
        {
            globalName = "";
            globalSize = "";
            globalNumber = "";
            globalNumber1 = "";
            globalOd = "";
        }

        private void updateData2()
        {
            SqlCommand sqlCmd = new SqlCommand();
            sqlCmd.CommandType = CommandType.Text;
            sqlCmd.CommandText = "select * from MarkHistory order by [Mark Time] desc";

            sqlCmd.Connection = sqlCon;
            int rowNum = 1;
            sqlCon.Open();
            SqlDataReader reader = sqlCmd.ExecuteReader();
            dataGridView2.Rows.Clear();
        
            while (reader.Read())
            {
                dataGridView2.Rows.Add(new object[] {
                    rowNum,
                    reader["Mark Code"],
                    Convert.ToDateTime(reader["Mark Time"]).ToString("dd/M/yyyy", CultureInfo.InvariantCulture), 
                    Convert.ToDateTime(reader["Mark Time"]).ToString("HH:mm:ss", CultureInfo.InvariantCulture), 
                    reader["Mark Count"],
                    });
                rowNum++;
            }
            sqlCon.Close();
        }

        private void addData2()
        {
            //SqlCommand sqlCmd = new SqlCommand();
            //sqlCmd.CommandType = CommandType.Text;
            //sqlCmd.CommandText = "select * from tableHistory";
            //sqlCmd.Connection = sqlCon;

            string query = "insert into MarkHistory Values(N'" + globalOd + "', getdate(), "+globalCount+")";

            SqlCommand sqlCommand = new SqlCommand(query, sqlCon);
            sqlCon.Open();
            sqlCommand.ExecuteNonQuery();
            sqlCon.Close();
            updateData2();
        }

        private void updateComplete()
        {
            deleteRow1();
            addData2();
            textBox4.Text = "";
            textBox5.Text = "";
            textBox6.Text = "";
            textBox7.Text = "";
            textBox12.Text = "";           
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            try
            {
                string asciiText = textBox3.Text;
                byte[] myByes = System.Text.Encoding.ASCII.GetBytes(asciiText);
                serialPort1.Write(myByes, 0, myByes.Length);
            }
            catch
            {

            }
            
        }

        private void btnDeldata_Click(object sender, EventArgs e)
        {
            deleteRow1();
            updateData1();
            clearGlobalvar();
        }

        private void btnListen_Click(object sender, EventArgs e)
        {
            StartServer();
            label3.Text = "Connect";
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                if (server != null)
                {
                    server.Stop();
                    foreach (Thread clientThread in clientThreads)
                    {
                        clientThread.Abort();
                    }
                    clientThreads.Clear();

                    server = null;
                    listenerThread = null;

                    label3.Text = "Disconnect";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void btnChoose_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = MessageBox.Show("Bạn có chắc chắn chọn mã hàng này?", "Information", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    InforMark();
                    string asciiText = "<LPhat_R0><DCLEAR>" + "<DNEW,TEXT," + textBox7.Text + ">" + "<D" + textBox7.Text + "," + textBox12.Text + ">" + "<DSPEED,4000><DPOWER,30><DFREQ,150><DMARK_MODE,2><DMARK_START_DIST_DELAY,15>" + "<D" + textBox7.Text + ",HEIGHT,3.0>" + "<D" + textBox7.Text + ",WIDTH,40.0>" + "<D" + textBox7.Text + ",X,0.0>" + "<D" + textBox7.Text + ",Y,0.0>";
                    byte[] myByes = System.Text.Encoding.ASCII.GetBytes(asciiText);
                    serialPort1.Write(myByes, 0, myByes.Length);
                }
                else
                {
                    return;
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
           
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(comboBox1.Text))
                {
                    return;
                }
                Properties.Settings.Default.Port1 = comboBox1.Text;
                serialPort1.PortName = Properties.Settings.Default.Port1;
                Properties.Settings.Default.Save();
                serialPort1.Open();
                serialPort1.RtsEnable = true;
                label14.Text = "Connected!";
            }
            catch
            {

            }
        }

        private void btnDis_Click(object sender, EventArgs e)
        {
            serialPort1.Close();
            label14.Text = "Disconnected!";
        }

        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = serialPort1.ReadExisting();
            textBox8.Text = data;
            if(textBox8.Text == "<XT><XE>")
            {
                label23.Text = "ĐÃ ĐỦ SỐ LẦN KHẮC";
                label23.ForeColor = Color.Black;
                updateComplete();
            }
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            string asciiText = "<DPLANCOUNT," + textBox6.Text +">" + "<DMARKCOUNT,0>" + "<LPhat_R0>" + "<X>";
            byte[] myByes = System.Text.Encoding.ASCII.GetBytes(asciiText);
            serialPort1.Write(myByes, 0, myByes.Length);
            label23.Text = "ĐANG CHẠY";
            label23.ForeColor = Color.Blue;
        }

        private void btneStop_Click(object sender, EventArgs e)
        {
            string asciiText = "<P>";
            byte[] myByes = System.Text.Encoding.ASCII.GetBytes(asciiText);
            serialPort1.Write(myByes, 0, myByes.Length);
        }

        private void btnDelhis_Click(object sender, EventArgs e)
        {
            deleteRow2();
            updateData2();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = MessageBox.Show("Bạn có chắc chắn chọn mã hàng này?", "Information", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    InforMark();
                    string asciiText = "<LPhat_R9><DCLEAR>" + "<DNEW,TEXT," + textBox7.Text + ">" + "<D" + textBox7.Text + "," + textBox12.Text + ">" + "<DSPEED,4000><DPOWER,30><DFREQ,150><DMARK_MODE,2><DMARK_START_DIST_DELAY,15>" + "<D" + textBox7.Text + ",HEIGHT,3.0>" + "<D" + textBox7.Text + ",WIDTH,40.0>" + "<D" + textBox7.Text + ",X,0.0>" + "<D" + textBox7.Text + ",Y,0.0>";
                    byte[] myByes = System.Text.Encoding.ASCII.GetBytes(asciiText);
                    serialPort1.Write(myByes, 0, myByes.Length);

                }
                else
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            using (HttpClient client = new HttpClient())
            {
                var requestData = new { processDate = DateTime.UtcNow.ToString("dd/MM/yyyy") };
                var request = new HttpRequestMessage();
                var data = JsonConvert.SerializeObject(requestData);
                var content = new System.Net.Http.StringContent(data);
                content.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("application/json");
                request.Content = content;
                request.Method = new System.Net.Http.HttpMethod("POST");
                var urlBuilder = new StringBuilder("http://alpdev.anlapphat.com:8002/zalp_get_pr_ord?sap-client=300");
                var url = urlBuilder.ToString();
                request.RequestUri = new System.Uri(url, System.UriKind.RelativeOrAbsolute);

                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", "QUxQLklUMDM6MTIzNDU2Nzg=");
                var response = await client.SendAsync(request, System.Net.Http.HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                var responseBody = await response.Content.ReadAsStringAsync();

                var orderProviders = JsonConvert.DeserializeObject<List<OrderProvider>>(responseBody);
                var orderGroups = orderProviders.GroupBy(x => x.MATERIAL).ToList();

                string deleteQuery = "DELETE FROM tableMark";
                SqlCommand sqlCommandDelete = new SqlCommand(deleteQuery, sqlCon);
                sqlCon.Open();
                sqlCommandDelete.ExecuteNonQuery();

                string insertQuery = "INSERT INTO tableMark ([Ten Hang], [Kich Thuoc], [so luong cay], [so luong bo], [OD]) " +
                                        "VALUES (@TenHang, @KichThuoc, @SoLuongCay, @SoLuongBo, @OD)";

                foreach (var orderGroup in orderGroups)
                {
                    var orderProvider = orderGroup.FirstOrDefault();
                    SqlCommand insertCommand = new SqlCommand(insertQuery, sqlCon);
                    insertCommand.Parameters.AddWithValue("@TenHang", orderProvider.MATERIAL);
                    insertCommand.Parameters.AddWithValue("@KichThuoc", orderProvider.SIZE);
                    insertCommand.Parameters.AddWithValue("@SoLuongCay", orderProvider.QUANTITY_CAY);
                    insertCommand.Parameters.AddWithValue("@SoLuongBo", orderProvider.QUANTIY_BO);
                    insertCommand.Parameters.AddWithValue("@OD", orderProvider.PRODUCTION_ORDER);

                    insertCommand.ExecuteNonQuery();
                }
                sqlCon.Close();
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {

            updateData2();
        }

        private void dataGridView2_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < dataGridView2.Rows.Count)
            {
                DataGridViewRow selectedRow = dataGridView2.Rows[e.RowIndex];

                string code = selectedRow.Cells["infor"].Value.ToString();               
                string count = selectedRow.Cells["number2"].Value.ToString();

                globalCode = code;
                //globalCount = count;              
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            ConnectSQL();
            updateData1();
            updateData2();
            comboBox1.Text = Properties.Settings.Default.Port1;
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {

                comboBox1.Items.Add(port);
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            DisconnectSQL();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            updateData1();
        }

        private void dataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex >= 0 && e.RowIndex < dataGridView1.Rows.Count)
            {
                DataGridViewRow selectedRow = dataGridView1.Rows[e.RowIndex];

                string name = selectedRow.Cells["name"].Value.ToString();
                string size = selectedRow.Cells["size"].Value.ToString();
                string number = selectedRow.Cells["number"].Value.ToString();
                string number1 = selectedRow.Cells["number1"].Value.ToString();
                string od = selectedRow.Cells["OD"].Value.ToString();
                
                globalName = name;
                globalSize = size;
                globalNumber = number;
                globalNumber1 = number1;
                globalOd = od;
            }
           
        }

    }
}
