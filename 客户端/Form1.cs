using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 客户端
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Socket client;
        private async void button1_Click(object sender, EventArgs e)
        {
            //开始连接
            try
            {
                client = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse("192.168.227.1"), 63323);
                await client.ConnectAsync(endpoint);
                label1.Text = "连接成功";
                button1.Enabled = false;
                button2.Enabled =true;
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.ConnectionRefused)
                    MessageBox.Show("服务器未启动/端口未开放");
                else if (ex.SocketErrorCode == SocketError.HostNotFound)
                    MessageBox.Show("IP地址不存在，检查服务端IP");
                else
                    MessageBox.Show($"网络异常：{ex.Message}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"未知错误：{ex.Message}");
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (client == null||!client.Connected)
            {
                MessageBox.Show("还没连接到服务器，起先连接服务器");
                return;
            }
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("发送数据不能为空");
                return;
            }
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(textBox1.Text);
                client.Send(data);
                label1.Text = "发送成功";
            }
            catch (Exception)
            {
                label1.Text = "连接失败";
                if (client!=null)
                {
                    if (client.Connected) client.Shutdown(SocketShutdown.Both);
                    client.Close();
                }
                button1.Enabled = true;
                button2.Enabled = false;
            }
        }
    }
}
