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

namespace _1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        Socket server = null;
        private void Form1_Load(object sender, EventArgs e)
        {
            // 1实例化服务器对象
            //参数1 地址类型 InterNetwork  ipv4类型， InterNetworkV6 ipv6的地址类型
            //参数2 SocketType.Stream 指定数据传输的时候 使用数据流的方式进行传输
            //参数3 指定通信协议 ProtocolType.Tcp 采用的tcp通信
            server = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //2把服务器对象绑定给一个ip和端口号
            //ip地址字符串
            string ipStr = "";

            //端口号 建议不要写太小  端口范围0-65535
            int port = 63323;

            //获取当前电脑的ip信息
            // Dns.GetHostName() 获取计算机的主机名称
            //  Dns.GetHostEntry 通过主机名解析成对应的ip
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());

            // host.AddressList 地址列表
            foreach (IPAddress item in host.AddressList)
            {
                if (item.AddressFamily == AddressFamily.InterNetwork) // 查询判断是不是ipv4的地址
                {
                    ipStr = item.ToString();
                }
            }

            //地址类型
            IPAddress address = IPAddress.Parse(ipStr);// 把字符串转成地址类型

            // 终端对象: 包含ip和端口
            IPEndPoint endPoint = new IPEndPoint(address, port);
            server.Bind(endPoint); //绑定ip和端口号


            //3 开启服务器
            server.Listen(100);// 容纳客户端数量

            // 使用哪些技术
            //服务器可以接收多个客户端，使用线程
            //客户端需要不停的发消息， 需要while（true）for一直处于接收状态
            // 多个客户端需要使用 需要使用线程，每个客户端需要发消息，又得使用线程，需要俩层嵌套

            //4 接收客户端发来的请求
            //AcceptDataSigle();//接收单个客户端的方法
           AcceptDataMul();
        }
        CancellationTokenSource cts;
        CancellationTokenSource cts2;
        public void AcceptDataMul()
        {
            // 接收客户端需要写在线程
            cts = new CancellationTokenSource();
            Task.Run(() =>
            {
                while (!cts.IsCancellationRequested)
                {
                    // 接收多个客户端
                    Socket client = server.Accept();//
                    if (client == null)
                    {
                        Console.WriteLine("客户端未连接");
                        break;
                    }
                    if (client.Connected) //保证客户端正常连接
                    {
                        //读取数据 一直读取 需要一个线程
                        cts2 = new CancellationTokenSource();
                        Task.Run(() =>
                        {
                            while (!cts2.IsCancellationRequested)
                            {
                                try
                                {
                                    //读取数据
                                    byte[] buffer = new byte[client.Available];
                                    int length = client.Receive(buffer);

                                    Invoke(new Action(() =>
                                    {
                                        richTextBox1.AppendText(Encoding.UTF8.GetString(buffer));

                                    }));

                                }
                                catch (Exception)
                                {

                                    break;
                                }

                            }
                        }, cts2.Token);
                    }
                }
            }, cts.Token);
        }
        public void AcceptDataSigle()
        {
            cts = new CancellationTokenSource();
            Task.Run(() =>
            {
                //server 服务器
                //client 客户端
                Socket client = server.Accept();// 接收客户端

                // 处理客户端发来数据
                while (!cts.IsCancellationRequested)
                {
                    //定义一个字节数组 数组的长度为读取的可用数据的长度
                    byte[] buffer = new byte[client.Available];

                    //Receive() 接收客户端的数据
                    int length = client.Receive(buffer);

                    Invoke(new Action(() =>
                    {
                        richTextBox1.AppendText(Encoding.UTF8.GetString(buffer));
                    }));
                }
            }, cts.Token);
        }

        //1 创建服务器对象  new socket()
        //2  服务器对象绑定终端  bind(IPEndPoint)
        //3  服务器开启监听  .listen
        //4 接收客户端连接请求.accept()
        //5 接收发来的数据 .receive()
    }
}
