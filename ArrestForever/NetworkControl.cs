using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Documents;

namespace ArrestForever
{
    public class NetworkControl
    {
        /// <summary>
        /// 服务器/客户端公用
        /// </summary>
        public static IPAddress localIP;//本地IPv4地址
        public int PlayerCount;//玩家数目限制
        private int MessageLength = 256;
        /// <summary>
        /// 仅服务器端使用
        /// </summary>
        private BroadcastServer broadcastServer;//广播服务器
        private IPEndPoint GameServer_send;//广播服务器发送端地址
        private IPEndPoint GameServer_receive;//广播服务器接收端地址
        private List<IPEndPoint> GameClientsBroadcastList;//在线玩家广播地址表
        public List<UdpClient> Clients;//在线玩家连接表
        private UdpClient Server_receive;//游戏接收服务器
        private UdpClient Server_send;//游戏发送服务器
        public Thread CreatGame_thread;//游戏创建线程
        public Thread WaitClient_thread;//等待客户端线程
        private Thread RequestReceive_thread;//游戏请求接收线程
        public Queue<String> RequestList_receive;//游戏请求接收队列
        private Queue<String> CommandList_send;//游戏指令发送队列
        public delegate void NewRequestRecieve();//新的游戏请求
        public event NewRequestRecieve NewRequest;//新请求到达事件
        public delegate void NewClientConnection();//新的客户端连接
        public event NewClientConnection NewClient;//新的客户端
        /// <summary>
        /// 仅客户端使用
        /// </summary>
        public BroadcastClient broadcastClient;//广播客户端
        public Thread JoinGame_thread;//游戏加入线程
        private UdpClient Client_receive;//游戏接收客户端
        private UdpClient Client_send;//游戏发送客户端
        private Thread CommandReceive_thread;//游戏命令接收线程
        public Thread LinkServer_thread;//连接服务器线程
        private Queue<String> RequestList_send;//游戏请求发送队列
        public Queue<String> CommandList_receive;//游戏指令接收队列
        public delegate void NewCommandReceive();//新的游戏指令
        public event NewCommandReceive NewCommand;//新指令到达事件
        public delegate void NewServerConnected();//连接到服务器
        public event NewServerConnected NewServer;//新的服务器
        /// <summary>
        /// 构造函数，获取本地可用IPv4地址
        /// </summary>
        public NetworkControl()
        {
            PlayerCount = 1;
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
            //获取本地可用IPv4地址
            foreach (IPAddress ipa in ips)
            {
                if (ipa.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ipa;
                    break;
                }
            }
        }
        /// <summary>
        /// 创建游戏服务器
        /// </summary>
        public void CreatGameServer()
        {
            GameClientsBroadcastList = new List<IPEndPoint>();//客户端广播地址表
            Clients = new List<UdpClient>();//客户端Udp连接
            RequestList_receive = new Queue<String>();//游戏请求接收队列
            CommandList_send = new Queue<String>();//游戏指令发送队列
            broadcastServer = new BroadcastServer(ref Server_receive, ref Server_send);//游戏广播服务器
            GameServer_send = broadcastServer.LocalIEP_send;//游戏广播服务器发送地址
            GameServer_receive = broadcastServer.LocalIEP_receive;//游戏广播服务器接收地址
            CreatGame_thread = new Thread(() =>
            {
                while (true)
                {
                    broadcastServer.sendStartBroadcast();
                    Thread.Sleep(1000);
                    Console.WriteLine("sendbroadcast~");
                }
            });//服务器发送广播
            Thread receiveIPList_thread = new Thread(() =>
            {
                while (GameClientsBroadcastList.Count < PlayerCount)
                {
                    IPEndPoint P_temp = broadcastServer.receiveResponse();
                    Console.WriteLine("a new broadcastclient");
                    if (!isIPExist(P_temp))
                    {
                        GameClientsBroadcastList.Add(P_temp);
                    }
                }
                CreatGame_thread.Abort();
            });//服务器接收响应
            CreatGame_thread.IsBackground = true;
            receiveIPList_thread.IsBackground = true;
            CreatGame_thread.Start();//服务器开始游戏广播
            receiveIPList_thread.Start();//服务器开始接收加入游戏响应
        }
        /// <summary>
        /// 等待客户端连接
        /// </summary>
        public void WaitClient()
        {
            WaitClient_thread = new Thread(() =>
            {
                while (Clients.Count < PlayerCount)
                {
                    UdpClient client = broadcastServer.getClient();
                    Clients.Add(client);
                    if (NewClient != null)
                        NewClient();
                }
            });
            WaitClient_thread.IsBackground = true;
            WaitClient_thread.Start();
        }
        /// <summary>
        /// 客户端广播地址列表里是否存在相同IP
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        private bool isIPExist(IPEndPoint p)
        {
            for (int i = 0; i < GameClientsBroadcastList.Count; i++)
                if (GameClientsBroadcastList[i].Address.Equals(p.Address))
                    return true;
            return false;
        }
        /// <summary>
        /// 创建游戏广播接收器
        /// </summary>
        public void CreatGameBroadcastLinker()
        {
            broadcastClient = new BroadcastClient(ref Client_receive, ref Client_send);//游戏广播客户端
            RequestList_send = new Queue<String>();
            CommandList_receive = new Queue<String>();
            JoinGame_thread = new Thread(() =>
            {
                broadcastClient.receiveBroadcast();
                broadcastClient.ConnectServer();
                broadcastClient.sendResponse();
                if (NewServer!=null)
                {
                    NewServer();
                }
            });//加入游戏线程
            JoinGame_thread.IsBackground = true;
            JoinGame_thread.Start();//开始接收服务器广播
        }
        /// <summary>
        /// 客户端开始处理游戏消息队列
        /// </summary>
        public void StartMessageQueue_client()
        {
            //RequestSend_thread = new Thread(() =>
            //{
            //    while (true)
            //    {
            //        if (RequestList_send.Count > 0)
            //        {
            //            String str;
            //            lock (RequestList_send)
            //            {
            //                str = RequestList_send.Dequeue();
            //            }
            //            SendGameCommandRequest(str);
            //        }
            //    }
            //});
            CommandReceive_thread = new Thread(() =>
            {
                while (true)
                {
                    String str = ReceiveGameCommand();
                    lock (CommandList_receive)
                    {
                        CommandList_receive.Enqueue(str);
                    }
                    if (NewCommand != null)
                        NewCommand();
                }
            });
            CommandReceive_thread.IsBackground = true;
            //RequestSend_thread.Start();
            CommandReceive_thread.Start();
        }
        /// <summary>
        /// 将游戏请求队列里的请求全部发出
        /// </summary>
		public void RequestSend_fun()
		{
			lock (RequestList_send)
				while (RequestList_send.Count > 0)
				{
					String str;
					str = RequestList_send.Dequeue();
					SendGameCommandRequest(str);
				}
		}
        /// <summary>
        /// 向请求队列中添加请求并强制异步处理
        /// </summary>
        /// <param name="str"></param>
        public void AddGameRequest(String str)
        {
            lock (RequestList_send)
            {
                RequestList_send.Enqueue(str);
            }
			ThreadPool.QueueUserWorkItem(new WaitCallback((object state) =>
				{
					RequestSend_fun();
				}), null);
        }
        /// <summary>
        /// 服务器开始处理游戏消息队列
        /// </summary>
        public void StartMessageQueue_server()
        {
            RequestReceive_thread = new Thread(() =>
            {
                while (true)
                {
                    String str = ReceiveGameRequest();
                    lock (RequestList_receive)
                    {
                        RequestList_receive.Enqueue(str);
                    }
                    if (NewRequest != null)
                        NewRequest();
                }
            });
            //CommandSend_thread = new Thread(() =>
            //{
            //    while (CommandList_send.Count > 0)
            //    {
            //        String str;
            //        lock (CommandList_send)
            //        {
            //            str = CommandList_send.Dequeue();
            //        }
            //        SendGameCommand(str);
            //    }
            //});
            RequestReceive_thread.IsBackground = true;
            RequestReceive_thread.Start();
            //CommandSend_thread.Start();
        }
        /// <summary>
        /// 将游戏指令队列里的命令全部发出
        /// </summary>
		public void CommandSend_fun()
		{
			lock (CommandList_send)
				while (CommandList_send.Count > 0)
				{
					String str;
					str = CommandList_send.Dequeue();
					SendGameCommand(str);
				}
		}
        /// <summary>
        /// 向消息队列中添加新的命令并强制异步执行
        /// </summary>
        /// <param name="str"></param>
        public void AddGameCommand(String str)
        {
            lock (CommandList_send)
            {
                CommandList_send.Enqueue(str);
            }
			ThreadPool.QueueUserWorkItem(new WaitCallback((object state) =>
				{
					CommandSend_fun();
				}), null);
        }
        /// <summary>
        /// 向服务器发送游戏操作请求
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public void SendGameCommandRequest(String command)
        {
            byte[] buff_send = Encoding.Unicode.GetBytes(command);
            Client_send.Send(buff_send, buff_send.Length);
        }
        /// <summary>
        /// 接收游戏指令
        /// </summary>
        public String ReceiveGameCommand()
        {
            IPEndPoint remoteIEP = null;
            byte[] buff_receive = Client_receive.Receive(ref remoteIEP);
            return Encoding.Unicode.GetString(buff_receive);
        }
        /// <summary>
        /// 向每个客户端发送游戏指令
        /// </summary>
        /// <param name="command"></param>
        public void SendGameCommand(String command)
        {
            byte[] buff_send = Encoding.Unicode.GetBytes(command);
            foreach (UdpClient client in Clients)
                client.Send(buff_send, buff_send.Length);
        }
        /// <summary>
        /// 接收指定客户端的请求
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public String ReceiveGameRequest()
        {
            IPEndPoint remoteIEP = null;
            byte[] buff_receive = Server_receive.Receive(ref remoteIEP);
            return Encoding.Unicode.GetString(buff_receive);
        }
        /// <summary>
        /// 获取第一个可用端口号
        /// </summary>
        /// <returns></returns>
        #region
        public static int GetFirstAvailablePort()
        {
            int MAX_PORT = 8000; //系统tcp/udp端口数最大是65535            
            int BEGIN_PORT = 5000;//从这个端口开始检测
            for (int i = BEGIN_PORT; i < MAX_PORT; i++)
            {
                if (PortIsAvailable(i)) return i;
            }
            return -1;
        }
        /// <summary>
        /// 返回被占用的端口号
        /// </summary>
        /// <returns></returns>
        public static List<int> PortIsUsed()
        {
            //获取本地计算机的网络连接和通信统计数据的信息
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            //返回本地计算机上的所有Tcp监听程序
            IPEndPoint[] ipsTCP = ipGlobalProperties.GetActiveTcpListeners();
            //返回本地计算机上的所有UDP监听程序
            IPEndPoint[] ipsUDP = ipGlobalProperties.GetActiveUdpListeners();
            //返回本地计算机上的Internet协议版本4(IPV4 传输控制协议(TCP)连接的信息。
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            List<int> allPorts = new List<int>();
            foreach (IPEndPoint ep in ipsTCP) allPorts.Add(ep.Port);
            foreach (IPEndPoint ep in ipsUDP) allPorts.Add(ep.Port);
            foreach (TcpConnectionInformation conn in tcpConnInfoArray) allPorts.Add(conn.LocalEndPoint.Port);
            return allPorts;
        }
        /// <summary>
        /// 检查指定端口是否可用
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public static bool PortIsAvailable(int port)
        {
            bool isAvailable = true;
            List<int> portUsed = PortIsUsed();
            foreach (int p in portUsed)
            {
                if (p == port)
                {
                    isAvailable = false;
                    break;
                }
            }
            return isAvailable;
        }
        #endregion
        public class BroadcastServer
        {
            private UdpClient GameServerBroadcast;//游戏开始广播服务器
            private IPEndPoint DeclareBroadcastAreaIEP;//广播地址段
            public IPEndPoint LocalIEP_send;//本机发送IP节点

            private UdpClient GameServerBroadcastRecieve;//游戏广播响应接收服务器
            private IPEndPoint ReceiveBroadcastAreaIEP;//接收地址段
            public IPEndPoint LocalIEP_receive;//本机接收IP节点

            public IPEndPoint GameServerIEP_receive;//游戏服务器接收节点
            public IPEndPoint GameServerIEP_send;//游戏服务器发送节点

            public UdpClient GameServer_receive;
            public UdpClient GameServer_send;

            private String GameServerBeginRunDeclare = "This is the game \"Bomb-man\" server";//游戏服务器验证声明
            private String Response = "This is a game client";//客户端验证声明
            /// <summary>
            /// 构造函数，创建广播发送和接收服务器，以及游戏服务器监听器
            /// </summary>
            /// <param name="tcpl"></param>
            public BroadcastServer(ref UdpClient udp1, ref UdpClient udp2)
            {
                LocalIEP_send = new IPEndPoint(NetworkControl.localIP, NetworkControl.GetFirstAvailablePort());//选取开始广播服务器地址
                GameServerBroadcast = new UdpClient(LocalIEP_send);//创建开始广播服务器
                LocalIEP_receive = new IPEndPoint(NetworkControl.localIP, NetworkControl.GetFirstAvailablePort());//选取响应接收服务器地址
                GameServerBroadcastRecieve = new UdpClient(LocalIEP_receive);//创建响应接收服务器
                DeclareBroadcastAreaIEP = new IPEndPoint(IPAddress.Broadcast, 23333);
                //广播发送范围为默认广播地址，端口为23333
                ReceiveBroadcastAreaIEP = new IPEndPoint(NetworkControl.localIP, NetworkControl.GetFirstAvailablePort());//创建接收地址段

                GameServerIEP_receive = new IPEndPoint(NetworkControl.localIP, NetworkControl.GetFirstAvailablePort());//选取游戏服务器地址和端口
                GameServer_receive = udp1 = new UdpClient(GameServerIEP_receive);
                GameServerIEP_send = new IPEndPoint(NetworkControl.localIP, NetworkControl.GetFirstAvailablePort());//选取游戏服务器地址和端口
                GameServer_send = udp2 = new UdpClient(GameServerIEP_send);
                //Console.WriteLine(LocalIEP_send.ToString());
                //Console.WriteLine(LocalIEP_receive.ToString());
                //Console.WriteLine(GameServerListener.ToString());
            }
            /// <summary>
            /// 发送游戏开始广播
            /// </summary>
            public void sendStartBroadcast()
            {
                byte[] buff = Encoding.Unicode.GetBytes(GameServerBeginRunDeclare
                    + ";My BroadcastIPEndPoint is;" + LocalIEP_receive.Address + ";" + LocalIEP_receive.Port
                    + ";My GameServerIPEndPoint is;" + GameServerIEP_receive.Address
                    + ";" + GameServerIEP_receive.Port);
                //创建广播内容，包括广播服务器接收地址和游戏服务器节点地址
                GameServerBroadcast.Send(buff, buff.Length, DeclareBroadcastAreaIEP);//向指定范围发送广播
            }
            /// <summary>
            /// 广播服务器接收回应
            /// </summary>
            /// <returns></returns>
            public IPEndPoint receiveResponse()
            {
                while (true)
                {
                    byte[] buff = GameServerBroadcastRecieve.Receive(ref ReceiveBroadcastAreaIEP);//接收响应数据
                    String message = Encoding.Unicode.GetString(buff);//字节流->字符串
                    String[] chips = message.Split(';');//拆分数据
                    Console.WriteLine("BroadResponse: " + message);
                    if (chips[0].Equals(Response))
                    {
                        return new IPEndPoint(IPAddress.Parse(chips[2]), Int32.Parse(chips[3]));//返回读出的客户机地址
                    }
                }
            }

            public UdpClient getClient()
            {
                while (true)
                {
                    byte[] buff = GameServer_receive.Receive(ref ReceiveBroadcastAreaIEP);
                    String message = Encoding.Unicode.GetString(buff);//字节流->字符串
                    String[] chips = message.Split(';');//拆分数据
                    Console.WriteLine(message);
                    if (chips[0].Equals(Response))
                    {
                        UdpClient udpclient = new UdpClient(new IPEndPoint(NetworkControl.localIP, NetworkControl.GetFirstAvailablePort()));
                        udpclient.Connect(new IPEndPoint(IPAddress.Parse(chips[1]), Int32.Parse(chips[2])));
                        return udpclient;//返回客户端连接
                    }
                }
            }
        }
        /// <summary>
        /// 游戏广播客户端
        /// </summary>
        public class BroadcastClient
        {
            private UdpClient GameClientBroadcast_receive;//广播接收服务器
            private UdpClient GameClientBroadcast_send;//广播发送服务器
            public IPEndPoint LocalIEP_receive;//本机接收地址
            public IPEndPoint localIEP_send;//本机发送地址
            private IPEndPoint ReceiveBroadcastArea;//广播接收范围
            private String Response = "This is a game client";//客户端验证声明
            private String GameServerBeginRunDeclare = "This is the game \"Bomb-man\" server";
            //服务器验证声明
            public IPEndPoint BroadcastServerIEP;//解析出的广播服务器IP
            public IPEndPoint GameServerIEP;//解析出的游戏服务器IP

            public UdpClient Client_receive;//游戏接收客户端
            private IPEndPoint ClientIEP_receive;
            public UdpClient Client_send;//游戏发送客户端
            private IPEndPoint ClientIEP_send;
            /// <summary>
            /// 构造函数，创建广播接收和发送服务器，以及游戏客户端TCP/IP连接
            /// </summary>
            /// <param name="udp1"></param>
            public BroadcastClient(ref UdpClient udp1, ref UdpClient udp2)
            {
                LocalIEP_receive = new IPEndPoint(NetworkControl.localIP, 23333);//选取本机地址，端口为23333
                GameClientBroadcast_receive = new UdpClient(LocalIEP_receive);//创建广播接收服务器
                ReceiveBroadcastArea = null;//接收范围为所有IP

                localIEP_send = new IPEndPoint(NetworkControl.localIP, NetworkControl.GetFirstAvailablePort());//创建广播发送服务器地址
                GameClientBroadcast_send = new UdpClient(localIEP_send);//创建广播发送服务器

                ClientIEP_receive = new IPEndPoint(NetworkControl.localIP, NetworkControl.GetFirstAvailablePort());
                Client_receive = udp1 = new UdpClient(ClientIEP_receive);//创建游戏接收客户端

                ClientIEP_send = new IPEndPoint(NetworkControl.localIP, NetworkControl.GetFirstAvailablePort());
                Client_send = udp2 = new UdpClient(ClientIEP_send);//创建游戏发送客户端
            }
            /// <summary>
            /// 接收游戏广播
            /// </summary>
            public void receiveBroadcast()
            {
                while (true)
                {
                    byte[] buff = GameClientBroadcast_receive.Receive(ref ReceiveBroadcastArea);//接收游戏服务器广播
                    Console.WriteLine("broadcast-receive");
                    String message = Encoding.Unicode.GetString(buff);//字节流->字符串
                    String[] chips = message.Split(';');//拆分数据
                    if (chips[0].Equals(GameServerBeginRunDeclare))
                    {
                        BroadcastServerIEP = new IPEndPoint(IPAddress.Parse(chips[2]), Int32.Parse(chips[3]));
                        //解析广播服务器IP地址
                        GameServerIEP = new IPEndPoint(IPAddress.Parse(chips[5]), Int32.Parse(chips[6]));
                        //解析游戏服务器IP地址
                        return;
                    }
                }
            }
            /// <summary>
            /// 连接游戏服务器
            /// </summary>
            public void ConnectServer()
            {
                Client_send.Connect(GameServerIEP);//连接解析出的游戏服务器地址
                //Console.WriteLine("服务器连接成功");
                byte[] buff = Encoding.Unicode.GetBytes(Response + ";" + ClientIEP_receive.Address + ";" + ClientIEP_receive.Port);
                Client_send.Send(buff, buff.Length);
            }
            /// <summary>
            /// 向广播服务器发送回应
            /// </summary>
            public void sendResponse()
            {
                GameClientBroadcast_send.Connect(BroadcastServerIEP);
                byte[] buff = Encoding.Unicode.GetBytes(Response + ";My IPEndPoint is;"
                    + LocalIEP_receive.Address + ";" + LocalIEP_receive.Port);//创建发送数据
                GameClientBroadcast_send.Send(buff, buff.Length);//向服务器发送响应信息和本机地址
            }
        }
    }
}
