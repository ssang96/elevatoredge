using elevatoredgemodule.MODEL;
using SimpleTcp;
using System;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace elevatoredgemodule.UTIL
{
    /// <summary>
    /// 데이터 수신시 이벤트 발생 델리게이트 
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void ReceiveDelegate(object sender, ReceivedEventArgs e);

   /// <summary>
   /// 메세지 수신하면 발생하는 이벤트 
   /// </summary>
    public class ReceivedEventArgs : EventArgs
    {
        private byte[] m_Buffer = null;

        public byte[] Data
        {
            get { return m_Buffer; }
            set { m_Buffer = value; }
        }

        public ReceivedEventArgs()
            : base()
        {
        }

        public ReceivedEventArgs(byte[] data)
            : base()
        {
            m_Buffer = data;
        }
    }

    /// <summary>
    /// 비동기 소켓 클래스
    /// </summary>
    public class AsynchronousSocket
    {
        /// <summary>
        /// 소켓 클래스 WatsonTcp 사용
        /// </summary>        
        private SimpleTcpClient TcpClientSocket;

        /// <summary>
        /// 수신 데이터 버퍼 사이즈 
        /// </summary>
        public const int BufferSize = 1024;		   

        /// <summary>
        /// 서버 접속 여부 플래그
        /// </summary>
        private bool IsConnected = false;

        /// <summary>
        /// 데이터 받았을때 이벤트
        /// </summary>
        public event ReceiveDelegate Received = null;      
        private IPEndPoint iep = null;
            
        /// <summary>
        /// 생성자
        /// 소켓 생성 및 세팅
        /// </summary>
        /// <param name="remoteEP"></param>
        public AsynchronousSocket(string ip, int port)
        {
            IPAddress IpAddress = IPAddress.Parse(ip);
            iep = new IPEndPoint(IpAddress, port);

            Task.Run(() => InitComm());            
        }

        /// <summary>
        /// 소켓 환경 설정 및 이벤트 등록하는 함수
        /// </summary>
        /// <returns></returns>
        private async Task InitCommSetting()
        {
            if (TcpClientSocket != null)
                TcpClientSocket.Dispose();

            TcpClientSocket = new SimpleTcpClient(iep.Address.ToString(), iep.Port, false, null, null);

            TcpClientSocket.Events.Connected    += ServerConnected;
            TcpClientSocket.Events.Disconnected += ServerDisconnected;
            TcpClientSocket.Events.DataReceived += MessageReceived;

            TcpClientSocket.Keepalive.EnableTcpKeepAlives       = true;
            TcpClientSocket.Keepalive.TcpKeepAliveInterval      = 5;
            TcpClientSocket.Keepalive.TcpKeepAliveTime          = 5;
            TcpClientSocket.Keepalive.TcpKeepAliveRetryCount    = 5;

            TcpClientSocket.Connect();
            await Task.Delay(1000);
        }

        /// <summary>
        /// 접속을 시도하는 함수
        /// </summary>
        private async void InitComm()
        {
            while(!IsConnected)
            {
                try
                {
                    await InitCommSetting();
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [AsynchronousSocket InitComm Error] {ex.Message}");                 
                }
                finally
                {
                    Thread.Sleep(5000);                    
                }
            }
        }

        /// <summary>
        /// 서버에 접속되면 발생하는 이벤트 함수 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerConnected(object sender, EventArgs e)
        {   
            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [AsynchronousSocket ServerDisconnected] Connected To Server");

            IsConnected = true;

            SendStartCommand();
        }

        /// <summary>
        /// 서버로부터 접속이 끊기면 발생하는 이벤트 함수 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ServerDisconnected(object sender, EventArgs e)
        {
            this.IsConnected = false;
            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [AsynchronousSocket ServerDisconnected] Disconnected From Server");
            Task.Run(() => InitComm());
        }

        /// <summary>
        /// 서버로부터 데이터 수신 시 발생하는 이벤트 함수
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void MessageReceived(object sender, DataReceivedFromServerEventArgs args)
        {  
            if (args.Data != null)
            {
                // 데이터 처리
                byte[] byReturn = new byte[args.Data.Length];
                Array.Copy(args.Data, byReturn, args.Data.Length);
                ReceivedEventArgs ea = new ReceivedEventArgs(byReturn);

                if (Received != null)
                    Received(this, ea);
            }
        }

        /// <summary>
        /// 접속 되면 최초 통신 하겠다는 메세지 전송 함수
        /// </summary>
        private void SendStartCommand()
        {
            string targetServerIP = iep.Address.ToString();

            string[] ipAddress = targetServerIP.Split('.');

            string serverIP = int.Parse(ipAddress[0]).ToString("D3")
                + "." + int.Parse(ipAddress[1]).ToString("D3")
                + "." + int.Parse(ipAddress[2]).ToString("D3")
                + "." + int.Parse(ipAddress[3]).ToString("D3");

            ComCommand StartCommand = new ComCommand();
            StartCommand.IpAddress = Encoding.ASCII.GetBytes(serverIP);
            StartCommand.PortNumber = Encoding.ASCII.GetBytes("8000");
            StartCommand.Status = (byte)'1';

            Send(StartCommand.GetByte());

            StartCommand.Status = (byte)'2';
            Send(StartCommand.GetByte());
        }

        /// <summary>
        /// 데이터를 보낼때 사용하는 함수
        /// </summary>
        /// <param name="data"></param>
        /// <param name="startCommand"></param>
        public void Send(byte[] data)
        {
            try
            {
                this.TcpClientSocket.Send(Encoding.Default.GetString(data));

                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [AsynchronousSocket Send] " + Encoding.Default.GetString(data) + " Send To Server");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [AsynchronousSocket Send Error] {ex.Message}");
            }
        }

        /// <summary>
        /// 현재 생성된 소켓 Dispose, 연결 시도 중이면, Flag를 빠꿔서 연결 시도 종료 하는 함수
        /// </summary>
        public void CloseAsynchSocket()
        {
            if (!this.IsConnected)
            {
                this.IsConnected = true;
                Thread.Sleep(1);
            }

            if (this.TcpClientSocket != null)
            {
                this.TcpClientSocket.Dispose();
                this.TcpClientSocket = null;
            }
        }
    }
}
