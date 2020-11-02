using elevatoredgemodule.MODEL;
using elevatoredgemodule.UTIL;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace elevatoredgemodule.CONTROL
{
    /// <summary>
    /// IBS와 통신 관리 및 수신 프로토콜 관리
    /// IBS에서 수신한 데이터는 HTTP CLIENT를 이용해서 Azure Connectivity 웹앱으로 전송
    /// </summary>
    public class Controller
    {
        /// <summary>
        /// 비동기 소켓 클라이언트 클래스
        /// </summary>
        private AsynchronousSocket ClientSocket;

        /// <summary>
        /// 데이터 받아서 처리하는 프로토콜 객체
        /// </summary>
        private Protocol protocol = null;

        /// <summary>
        /// 엘리베이터 서버 IP
        /// </summary>
        public string TargetIPAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// 엘리베이터 서버 PORT
        /// </summary>
        public int TargetPort { get; set; } =  20000;

        /// <summary>
        /// 건물 아이디 
        /// </summary>
        public string BuildingID { get; set; } = "build_id_001";

        /// <summary>
        /// Azure Web App의 주소
        /// </summary>
        public string TargetServerURL { get; set; } = "https://adt-dev-kc-connectivity-tsop-web.azurewebsites.net/event/elevator/status";

        /// <summary>
        /// 디바이스 아이디 및 엣지 모듈 아이디
        /// </summary>
        public string DeviceID { get; set; } = "iotedge01/elevatoriotedgemodule";

        /// <summary>
        /// 생성자
        /// 초기화
        /// </summary>
        public Controller()
        {
            protocol = new Protocol();
            protocol.AddProtocolItem(Marshal.SizeOf(typeof(StatusNotification)), true, new CheckFunction(StatusNotificationCheck), new CatchFunction(StatusNotificationCatch));

            StartAsyncSocket(TargetIPAddress, TargetPort);
        }

        /// <summary>
        /// 생성자
        /// 초기화
        /// </summary>
        public Controller(String elevatorIP, int elevatorPort, String webappAddress, String buildingID, String deviceID)
        {
            protocol = new Protocol();
            protocol.AddProtocolItem(Marshal.SizeOf(typeof(StatusNotification)), true, new CheckFunction(StatusNotificationCheck), new CatchFunction(StatusNotificationCatch));

            this.TargetIPAddress    = elevatorIP;
            this.TargetPort         = elevatorPort;
            this.BuildingID         = buildingID;
            this.TargetServerURL    = webappAddress;
            this.DeviceID           = deviceID;

            StartAsyncSocket(elevatorIP, elevatorPort);
        }

        /// <summary>
        /// 엘리베이터 통신 서버의 IP, PORT로 접속 클래스 생성하는 함수
        /// </summary>
        /// <param name="remoteIPAddress"></param>
        /// <param name="port"></param>
        public void StartAsyncSocket(string remoteIPAddress, int port)
        {
            try
            {
                this.TargetIPAddress = remoteIPAddress;
                this.TargetPort = port;

                ClientSocket = new AsynchronousSocket(remoteIPAddress, port);
                ClientSocket.Received += new ReceiveDelegate(OnReceived);
            }
            catch (Exception E)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [Controller Controller Error] {E.Message}");            
            }
        }

        /// <summary>
        /// 엘리베이터 감시반에서 통합 서버로 보내는 정보 프로토콜 STX, ETX 체크
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="queue"></param>
        /// <returns></returns>
        private bool StatusNotificationCheck(object sender, CircleQueue queue)
        {
            //Header Check
            if (queue.Buffer[(queue.Sp + 0 + queue.Size) % queue.Size] != (byte)'S' && queue.Buffer[(queue.Sp + 0 + queue.Size) % queue.Size] != (byte)'E')
                return false;		// Header Check
   
            //Tail Check
            if (queue.Buffer[(queue.Sp - 1 + queue.Size) % queue.Size] != (byte)'E')
                return false;
            return true;
        }       

        /// <summary>
        /// 엘리베이터 감시반에서 통합 서버로 보내는 정보 프로토콜 수신
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        private bool StatusNotificationCatch(object sender, byte[] Data)
        {
            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [Controller : StatusNotificationCatch] {System.Text.Encoding.ASCII.GetString(Data)} Status Received.");
            HttpClientTransfer HttpTransfer = new HttpClientTransfer();

            Task<string> task = Task.Run<string>(async() => await HttpTransfer.PostWebAPI(TargetServerURL, System.Text.Encoding.ASCII.GetString(Data), BuildingID, DeviceID));
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnReceived(object sender, ReceivedEventArgs e)
        {
            for (int i = 0; i < e.Data.Length; i++)
            {
                this.protocol.ProtocolProcessing(e.Data[i]);
            }
        }

        /// <summary>
        /// Direct Method로 엘리베이터의 IP, PORT 변경 시, Async 소켓 종료 후, 재 생성
        /// </summary>
        public void CloseAsyncSocket()
        {
            if (this.ClientSocket != null)
                this.ClientSocket.CloseAsynchSocket();           
        }

        /// <summary>
        /// 엘리베이터 서버로 제어 전송하는 함수      
        /// </summary>
        /// <param name="sendData"></param>
        /// <param name="sendCommand"></param>
        public void CommandSendToServer(byte[] sendData)
        {
            if(this.ClientSocket != null)
            {
                byte[] command = new byte[sendData.Length + 2];

                command[0] = 0x02;
                command[sendData.Length + 2 - 1] = 0x03;

                for(int i = 1; i < sendData.Length; i++)
                {
                    command[i] = sendData[i - 1];
                }

                this.ClientSocket.Send(command);
            }
        }

        public void Dispose()
        {
            ClientSocket?.CloseAsynchSocket();

            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
    }
}
