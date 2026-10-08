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

namespace 简介聊天室
{
    public partial class Form1 : Form
    {
        // 定义保存所有的客户端的字典
        // 接收到客户端对象之后往这个dic添加数据
        //键是客户端终端  
        //值是客户端对象
        Dictionary<EndPoint,Socket> clients = new Dictionary<EndPoint,Socket>();
        //下拉框的数据源
        List<Client> clients2 = new List<Client>();
        Socket socketServer=null;
        CancellationTokenSource cts1 = null;
        public Form1()
        {
            InitializeComponent();
        }
        public class Client
        {
            public EndPoint EndPoint { get; set; }// 终端对象 IP加端口
            public Socket Socket { get; set; }  //客户端对象 数据通道
        }
        //启动服务器
        private void button1_Click(object sender, EventArgs e)
        {
            //启动服务器
            try
            {
                if (button1.Text == "启动服务器")
                {
                    // 打开服务器
                    StartServer();

                    // 监听客户端是否连入数据读取
                    Accepte();
                    button1.Text = "关闭服务器";
                }
                else
                {
                    //关闭服务器
                    CloseServer();
                    button1.Text = "启动服务器";
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
        }
        //关闭服务器
        public void CloseServer()
        {
            if (socketServer != null)
            {
                foreach (var item in clients2)
                {
                    Socket s1 = item.Socket;
                    try
                    {
                        s1.Send(Encoding.UTF8.GetBytes("n1"));
                        s1.Disconnect(false);
                    }
                    catch
                    {
                        // 客户端离线，发送失败直接跳过，不中断关闭流程
                    }
                }
            }
            // 核心修复：判空后再操作cts1
            if (cts1 != null)
            {
                cts1.Cancel();
                cts1.Dispose();
                cts1 = null;
            }
            if (socketServer != null)
            {
                socketServer.Close();
                socketServer = null;
            }
            button1.Text = "启动服务器";
        }
        //打开服务器的方法
        public void StartServer()//数据通道绑定IP
        {
            socketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socketServer.Bind(new IPEndPoint( IPAddress.Parse(textBox1.Text),int.Parse(textBox2.Text)));
            socketServer.Listen(1000);
        }
        //连接客户端和读取客户端发来的数据
        public void Accepte()
        {
            cts1 = new CancellationTokenSource();
            Task.Run(() =>
            {
                while (!cts1.IsCancellationRequested)
                {
                    Socket socketClient = null;  // 代表客户端
                    //读取客户端连入请求
                    try
                    {
                        socketClient = socketServer.Accept();
                    }
                    catch (Exception)
                    {
                        break;
                    }
                    // 把读取的客户端对象存储在字典里面 
                    // 先判断字典是否存在 如果不存在就添加
                    //socketClient.RemoteEndPoint 客户端的终端属性
                    if (!clients.ContainsKey(socketClient.RemoteEndPoint))
                    {
                        clients.Add(socketClient.RemoteEndPoint, socketClient); //添加到字典里面 键是终端，socketClient值

                        //添加到下拉框的数据源中list
                        clients2.Add(new Client()
                        {
                            EndPoint = socketClient.RemoteEndPoint,
                            Socket = socketClient
                        });
                    }
                    Invoke(new Action(() =>
                    {
                        comboBox1.DataSource = null;
                        comboBox1.DataSource = clients2;
                        comboBox1.DisplayMember = "EndPoint";
                        comboBox1.ValueMember = "Socket";
                    }));

                    //读取客户端数据
                    var clientCts = new CancellationTokenSource();
                    Task.Run(() =>
                    {
                        // 2. 固定1024缓冲区，替换 socketClient.Available（收不到消息根源）
                        byte[] buffer = new byte[1024];
                        while (!clientCts.IsCancellationRequested)
                        {
                            int len = 0;
                            try
                            {
                                len = socketClient.Receive(buffer);
                            }
                            catch
                            {
                                break;
                            }

                            if (len > 0)
                            {
                                EndPoint end = socketClient.RemoteEndPoint;//远程客户端地址信息
                                // 3. 只读取有效字节，去除末尾空字符，解决n1判断失效
                                string message = Encoding.UTF8.GetString(buffer, 0, len).Trim();
                                // 1. 拼接发送方IP+端口前缀
                                IPEndPoint clientEp = end as IPEndPoint;
                                string prefix = "";
                                if (clientEp != null)
                                {
                                    prefix = $"{clientEp.Address}:{clientEp.Port} ";
                                }
                                // 组装带IP端口的完整消息
                                string newMsg = prefix + message;
                                byte[] sendData = Encoding.UTF8.GetBytes(newMsg);
                                foreach (var item in clients2)
                                {
                                    Socket s = item.Socket;
                                    if (s.RemoteEndPoint!= end)
                                    {
                                        try
                                        {
                                            s.Send(sendData);
                                        }
                                        catch (Exception)
                                        {

                                            
                                        }
                                        
                                    }
                                }

                                //事先和客户端商量好 ， 断开之后给服务器发一个断开的消息
                                // 不用默认断开的标识， 发送一个n1代表客户端断开，
                                if (message == "n1") //证明客户端要断开了
                                {
                                    //查找数组里面已经有一个和当前终端一样的
                                    Client c1 = clients2.Find(x => x.EndPoint == end);
                                    if (c1 != null)
                                    {
                                        clients2.Remove(c1);//移除客户端对象
                                        clients.Remove(end); //4. 补充删除字典，修复残留离线客户端
                                    }

                                    if (clients2.Count == 0)
                                    {
                                        //证明移除完客户端对象 数据源置位空
                                        Invoke(new Action(() =>
                                        {
                                            comboBox1.DataSource = null;
                                        }));
                                    }
                                    else  // ，没有移除完， 把数据源再重新赋值一下
                                    {
                                        Invoke(new Action(() =>
                                        {
                                            comboBox1.DataSource = null;
                                            comboBox1.DataSource = clients2;
                                            comboBox1.DisplayMember = "EndPoint";
                                            comboBox1.ValueMember = "Socket";
                                        }));
                                    }
                                    socketClient.Close();
                                    break;
                                }

                                Invoke(new Action(() =>
                                {
                                    richTextBox1.Text += $"{end}:{message}" + Environment.NewLine;
                                    richTextBox1.SelectionStart = richTextBox1.TextLength;//  把光标定到文本最后面
                                    richTextBox1.ScrollToCaret(); // 滚动到光标处
                                }));
                            }
                        }

                    }, clientCts.Token); // 使用局部令牌，不再共用全局cts1
                }

            }, cts1.Token);
        }
        //发送数据
        private void button2_Click(object sender, EventArgs e)
        {
            if (socketServer == null)
            {
                MessageBox.Show("请先打开服务器");
                return;
            }
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("发送的内容不能为空");
                return;
            }
            //字典和list 放的数据是一样的
            if (checkBox1.Checked) //选中广播 所有的客户端发消息
            {
                foreach (var item in clients2) //  遍历所有的客户端
                {
                    item.Socket.Send(Encoding.UTF8.GetBytes("服务器说：" + textBox1.Text)); //给所有的客户端
                }

            }
            else  //私发
            {
                Socket s1 = (Socket)comboBox1.SelectedValue;// 客户端对象
                if (s1 != null && s1.Connected)
                {
                    s1.Send(Encoding.UTF8.GetBytes("服务器说：" + textBox1.Text)); //给单独的客户端发消息
                }
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CloseServer();
        }
    }
}
