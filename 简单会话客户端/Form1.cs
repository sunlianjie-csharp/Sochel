using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace 简单会话客户端
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        //连接服务器
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (button1.Text== "连接服务器")
                {
                    //连接服务器
                    ConnetServer();
                    //接收数据
                    Accept();
                }
                else
                {
                    //断开连接
                    DisConnetServer();
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(ex.Message);
            }
        }
        Socket socketClient;
        public void ConnetServer()
        {
             socketClient = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(textBox1.Text), int.Parse(textBox2.Text));
            socketClient.Connect(endPoint);
            socketClient.Send(Encoding.UTF8.GetBytes("连接成功"));
            button1.Text = "断开服务器";
        }
        //接收数据
        CancellationTokenSource cts;
        public void Accept()
        {
            cts = new CancellationTokenSource();
            Task.Run(() =>
            {
                // 【修复1：固定缓冲区，抛弃Available】
                byte[] buffer = new byte[1024];
                while (!cts.IsCancellationRequested)
                {
                    int lenght = 0;
                    try
                    {
                        lenght = socketClient.Receive(buffer);
                    }
                    catch (Exception)
                    {
                        // 网络断开直接跳出循环
                        break;
                    }

                    if (lenght > 0)
                    {
                        // 【修复2：只读取有效字节+清除空白，防止n1匹配失败】
                        string message = Encoding.UTF8.GetString(buffer, 0, lenght).Trim();

                        // 【修复3：下线判断放在Invoke外部，优先处理断开逻辑】
                        if (message == "n1")
                        {
                            // 服务器下发下线指令，客户端主动断开
                            Invoke(new Action(DisConnetServer));
                            break;
                        }

                        // 仅UI更新放进委托
                        Invoke(new Action(() =>
                        {
                            richTextBox1.Text += message + "\r\n";
                            richTextBox1.SelectionStart = richTextBox1.TextLength;
                            richTextBox1.ScrollToCaret();
                        }));
                    }
                }
            }, cts.Token);
        }
        public void DisConnetServer()
        {
            if (socketClient != null)
            {
                socketClient.Send(Encoding.UTF8.GetBytes("n1"));
                socketClient.Disconnect(false);
                socketClient.Close();
                socketClient = null;
                cts.Cancel();
                button1.Text = "连接服务器";
            }
        }
        //发送
        private void button2_Click(object sender, EventArgs e)
        {
            // 1. 基础校验：Socket存在 + 发送内容非空
            if (socketClient == null)
            {
                MessageBox.Show("未连接服务器！");
                return;
            }
            string sendMsg = textBox3.Text.Trim();
            if (string.IsNullOrWhiteSpace(sendMsg))
            {
                MessageBox.Show("发送内容不能为空！");
                return;
            }

            try
            {
                // 2. 发送字节
                byte[] sendBytes = Encoding.UTF8.GetBytes(sendMsg);
                socketClient.Send(sendBytes);

                // 3. 聊天框追加发送记录（明文展示，换行自动滚动到底）
                Invoke(new Action(() =>
                {
                    richTextBox1.Text += $"{textBox1.Text}:{sendMsg}\r\n";
                    richTextBox1.SelectionStart = richTextBox1.TextLength;
                    richTextBox1.ScrollToCaret();
                    
                    textBox3.Clear();
                }));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"发送失败：{ex.Message}，连接已断开");
                DisConnetServer();
            }
        }
    }
}
