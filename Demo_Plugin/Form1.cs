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
using System.Data.SQLite;
using System.IO.Ports;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using RestSharp;
using Newtonsoft.Json;
using System.Runtime.InteropServices;

namespace Demo_Plugin
{
    public partial class Form1 : Form
    {      
        string globalName, globalSize, globalNumber, globalNumber1, globalOd, globalCode, globalCount;
        
        private SQLiteConnection sqlCon;
        private SQLiteCommand sqlCmd;
        private string slqliteconnection = "Data Source=dataALP.db;Version=3";

        private int count = 0;
        private int preVal = 0;
        private bool outloop = false;
        public Form1()
        {
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        #region SQLite
        private void SetConnection()
        {

            sqlCon = new SQLiteConnection(slqliteconnection);

        }

        private void ExecuteQuery(string txtQuery)
        {
            try
            {
                SetConnection();
                sqlCon.Open();


                sqlCmd = sqlCon.CreateCommand();
                sqlCmd.CommandText = txtQuery;

                sqlCmd.ExecuteNonQuery();
                sqlCon.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error while execute query:  " + ex.ToString());
                return;
            }
        }

        private void saveData(string name, string size, string number, string number1, string od)
        {
            ExecuteQuery("select * from tableMark");
            ExecuteQuery("insert into tableMark values ('" + name + "','" + Convert.ToInt16(size) + "','" + Convert.ToInt16(number) + "','" + Convert.ToInt16(number1) + "', '" + od + "')");

            this.Invoke((MethodInvoker)delegate
            {
                load_Datagridview1();
            });
        }

        private void addData2()
        {

            string date = DateTime.Now.ToString("dd/MM/yyyy");
            string time = DateTime.Now.ToString("HH:mm:ss");
            string query = "INSERT INTO markHistory (infor, day, time, number2) VALUES (@infor, @date, @time, @number2)";

            using (SQLiteCommand sqlCommand = new SQLiteCommand(query, sqlCon))
            {
                // Thay thế các tham số bằng giá trị thực tế
                sqlCommand.Parameters.AddWithValue("@infor", globalOd);
                sqlCommand.Parameters.AddWithValue("@date", date);
                sqlCommand.Parameters.AddWithValue("@time", time);
                sqlCommand.Parameters.AddWithValue("@number2", globalCount);

                sqlCon.Open();
                sqlCommand.ExecuteNonQuery();
                sqlCon.Close();
            }

            load_Datagridview2();
        }


        private void load_Datagridview1()
        {
            SetConnection();
            int rowNum = 1;
            sqlCon.Open();
            SQLiteCommand comm = new SQLiteCommand("Select * From tableMark", sqlCon);
            using (SQLiteDataReader read = comm.ExecuteReader())
            {
                dataGridView1.Rows.Clear();
                while (read.Read())
                {
                    dataGridView1.Rows.Add(new object[] {
                        rowNum,
                        read.GetValue(read.GetOrdinal("name")),  // U can use column index
                        read.GetValue(read.GetOrdinal("size")),  // Or column name like this
                        read.GetValue(read.GetOrdinal("number")),
                        read.GetValue(read.GetOrdinal("number1")),
                        read.GetValue(read.GetOrdinal("od"))

                    });
                    rowNum++;
                }
            }
            sqlCon.Close();
        }


        private void load_Datagridview2()
        {

            int row2 = 1;
            dataGridView2.Rows.Clear();
            SetConnection();
            sqlCon.Open();
            SQLiteCommand comm = new SQLiteCommand("Select * From markHistory", sqlCon);
            using (SQLiteDataReader read = comm.ExecuteReader())
            {
                while (read.Read())
                {
                    dataGridView2.Rows.Add(new object[] {
                        row2,
                        read.GetValue(read.GetOrdinal("infor")),  // U can use column index
                        read.GetValue(read.GetOrdinal("day")),
                        read.GetValue(read.GetOrdinal("time")),               
                        read.GetValue(read.GetOrdinal("number2")),
                    });
                    row2++;
                }
            }
            sqlCon.Close();
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            try
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

                    request.Headers.Authorization = new AuthenticationHeaderValue("Basic", "YWxwLml0MDM6MTIzNDU2Nzg5MA==");
                    var response = await client.SendAsync(request, System.Net.Http.HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    var orderProviders = JsonConvert.DeserializeObject<List<OrderProvider>>(responseBody);
                    var orderGroups = orderProviders.GroupBy(x => x.MATERIAL).ToList();

                    ExecuteQuery("DELETE FROM tableMark");

                    sqlCon.Open();
                    string insertQuery = "INSERT INTO tableMark (name, size, number, number1, od) " +
                                            "VALUES (@TenHang, @KichThuoc, @SoLuongCay, @SoLuongBo, @OD)";

                    foreach (var orderGroup in orderGroups)
                    {
                        var orderProvider = orderGroup.FirstOrDefault();
                        SQLiteCommand insertCommand = new SQLiteCommand(insertQuery, sqlCon);
                        insertCommand.Parameters.AddWithValue("@TenHang", orderProvider.MATERIAL);
                        insertCommand.Parameters.AddWithValue("@KichThuoc", orderProvider.SIZE);
                        insertCommand.Parameters.AddWithValue("@SoLuongCay", orderProvider.QUANTITY_CAY);
                        insertCommand.Parameters.AddWithValue("@SoLuongBo", orderProvider.QUANTIY_BO);
                        insertCommand.Parameters.AddWithValue("@OD", orderProvider.PRODUCTION_ORDER);
                        insertCommand.ExecuteNonQuery();
                    }

                    sqlCon.Close();

                }

                this.Invoke((MethodInvoker)delegate
                {
                    load_Datagridview1();
                });
            }
            catch
            { }

        }
        private void deleteData1()
        {
            ExecuteQuery("DELETE FROM tableMark");
            this.Invoke((MethodInvoker)delegate
            {
                load_Datagridview1();
            });
        }

        private void deleteData2()
        {
            ExecuteQuery("DELETE FROM markHistory");
            this.Invoke((MethodInvoker)delegate
            {
                load_Datagridview2();
            });
        }

        private void deleteRow()
        {
            ExecuteQuery("select * from tableMark");
            string query = "delete from tableMark where name = '" + globalName + "'";
            SQLiteCommand sqlCommand = new SQLiteCommand(query, sqlCon);
            sqlCon.Open();
            sqlCommand.ExecuteNonQuery();
            sqlCon.Close();

            load_Datagridview1();
        }

        #endregion

        private void send(string a)
        {
            string asciiText = a;
            byte[] myByes = System.Text.Encoding.ASCII.GetBytes(asciiText);
            serialPort1.Write(myByes, 0, myByes.Length);
        }


        delegate void SetTextCallback(string text);

        private void SetText(string text)
        {
            if (this.textBox8.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.textBox8.Text = text;
            }
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
              
        private void clearGlobalvar()
        {
            globalName = "";
            globalSize = "";
            globalNumber = "";
            globalNumber1 = "";
            globalOd = "";
        }
        
        private void updateComplete()
        {
            deleteRow();
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
            deleteData1();
            clearGlobalvar();
        }

        private void btnChoose_Click(object sender, EventArgs e)
        {
            try
            {
                DialogResult result = MessageBox.Show("Bạn có chắc chắn chọn mã hàng này?", "Information", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                if (result == System.Windows.Forms.DialogResult.Yes)
                {
                    InforMark();
                    //send("<LPhat_R0><DCLEAR>" + "<DNEW,TEXT," + textBox7.Text + ">" + "<D" + textBox7.Text + "," + textBox12.Text + ">" + "<DSPEED,4000><DPOWER,30><DFREQ,150><DMARK_MODE,0><DMARK_START_DIST_DELAY,15>" + "<D" + textBox7.Text + ",HEIGHT,3.0>" + "<D" + textBox7.Text + ",WIDTH,40.0>" + "<D" + textBox7.Text + ",X,0.0>" + "<D" + textBox7.Text + ",Y,0.0>" + "<DMARKCOUNT,0>");
                    send("<LPhat_R0><DCLEAR>" + "<DNEW,TEXT," + textBox7.Text + ">" + "<D" + textBox7.Text + "," + textBox12.Text + ">" + "<DSPEED,4000><DPOWER,30><DFREQ,150><DMARK_MODE,0>" + "<D" + textBox7.Text + ",HEIGHT,3.0>" + "<D" + textBox7.Text + ",WIDTH,40.0>" + "<D" + textBox7.Text + ",X,0.0>" + "<D" + textBox7.Text + ",Y,0.0>" + "<DMARKCOUNT,0>");

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


        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            string data = serialPort1.ReadExisting();
            textBox8.Text = data;

            if (textBox8.Text == "<XT><XE>")
            {
                label23.Text = "ĐÃ ĐỦ SỐ LẦN KHẮC";
                label23.ForeColor = Color.Black;
                updateComplete();
            }

        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                //send("<DPLANCOUNT," + textBox6.Text + ">" + "<DMARKCOUNT,0>" + "<LPhat_R0>" + "<X>");                
                //label23.Text = "ĐANG CHẠY";
                //label23.ForeColor = Color.Blue;
                outloop = false;
                count = 0;
                label26.Text = count.ToString();

                Thread read = new Thread(readSignal);
                read.IsBackground = true;
                read.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void btneStop_Click(object sender, EventArgs e)
        {
            try
            {
                //send("<P>");
                outloop = true;
                string date = DateTime.Now.ToString("dd/MM/yyyy");
                string time = DateTime.Now.ToString("HH:mm:ss");
                string query = "INSERT INTO markHistory (infor, day, time, number2) VALUES (@infor, @date, @time, @number2)";

                deleteRow();
                using (SQLiteCommand sqlCommand = new SQLiteCommand(query, sqlCon))
                {
                    // Thay thế các tham số bằng giá trị thực tế
                    sqlCommand.Parameters.AddWithValue("@infor", globalOd);
                    sqlCommand.Parameters.AddWithValue("@date", date);
                    sqlCommand.Parameters.AddWithValue("@time", time);
                    sqlCommand.Parameters.AddWithValue("@number2", label26.Text);

                    sqlCon.Open();
                    sqlCommand.ExecuteNonQuery();
                    sqlCon.Close();
                }

                load_Datagridview2();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

        }

        private void btnDelhis_Click(object sender, EventArgs e)
        {
            deleteData2();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            updateComplete();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            deleteRow();
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

        private void readSignal()
        {
            while (true)
            {
                if(outloop == true)
                {
                    return;
                }
                int Val = int.Parse(label2.Text);
                label23.Text = "ĐANG CHẠY";
                label23.ForeColor = Color.Blue;
                if (Val == 1 && preVal == 0)
                {
                    send("<X>");
                    count++;
                    label26.Text = count.ToString();
                }
                if(count == Convert.ToInt16(textBox6.Text))
                {
                    label23.Text = "ĐÃ ĐỦ SỐ LẦN KHẮC";
                    label23.ForeColor = Color.Black;
                    updateComplete();
                    return;
                }

                preVal = Val;
            }
        }
        private void btnthread_Click(object sender, EventArgs e)
        {
            
        }

        private void button1_MouseDown(object sender, MouseEventArgs e)
        {
            label2.Text = "1";
        }

        private void button1_MouseUp(object sender, MouseEventArgs e)
        {
            label2.Text = "0";
        }

        private void button5_Click(object sender, EventArgs e)
        {
            load_Datagridview1();
        }

        private void button1_Click_1(object sender, EventArgs e)
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
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }


        private void dataGridView2_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.RowIndex < dataGridView2.Rows.Count)
                {
                    DataGridViewRow selectedRow = dataGridView2.Rows[e.RowIndex];

                    string code = selectedRow.Cells["infor"].Value.ToString();
                    string count = selectedRow.Cells["number2"].Value.ToString();

                    //globalCode = code;
                    //globalCount = count;              
                }
            }
            catch
            {
                return;
            }
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {

            load_Datagridview1();
            load_Datagridview2();
            comboBox1.Text = Properties.Settings.Default.Port1;
            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {

                comboBox1.Items.Add(port);
            }
        }
     
        private void dataGridView1_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            try
            {
                if (e.RowIndex >= 0 && e.RowIndex < dataGridView1.Rows.Count)
                {
                    DataGridViewRow selectedRow = dataGridView1.Rows[e.RowIndex];

                    string name = selectedRow.Cells["name"].Value.ToString();
                    string size = selectedRow.Cells["size"].Value.ToString();
                    string number = selectedRow.Cells["number"].Value.ToString();
                    string number1 = selectedRow.Cells["number1"].Value.ToString();
                    string od = selectedRow.Cells["od"].Value.ToString();

                    globalName = name;
                    globalSize = size;
                    globalNumber = number;
                    globalNumber1 = number1;
                    globalOd = od;
                }
            }
            catch
            {
                return;
            }
            
           
        }

    }
}
