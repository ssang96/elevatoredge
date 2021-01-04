using elevatoredgemodule.MODEL;
using elevatoredgemodule.UTIL;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Timers;

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
        /// T TOWER - 150.3.2.250
        /// </summary>
        public string targetIPAddress { get; set; } = "127.0.0.1";

        /// <summary>
        /// 엘리베이터 서버 PORT
        /// </summary>
        public int targetPort { get; set; } =  8000;

        /// <summary>
        /// 건물 아이디 
        /// T TOWER - 0001
        /// </summary>
        public string buildingID { get; set; } = "0001";

        /// <summary>
        /// Azure Web App의 주소
        /// </summary>
        public string azureWebAppURL { get; set; } = "https://skt-stg-kc-ev-app.azurewebsites.net/event/elevator/status";

        /// <summary>
        /// 디바이스 아이디 및 엣지 모듈 아이디
        /// </summary>
        public string deviceID { get; set; } = "iotedge01/elevatoriotedgemodule";

        /// <summary>
        /// 호기 정보를 관리하는 객체
        /// </summary>
        private UnitDataController unitDataController = null;

        /// <summary>
        /// IBS와 엣지간의 통신 상태를 체크하는 Timer
        /// </summary>
        private Timer comCheckTimer = null;

        /// <summary>
        /// 생성자
        /// 초기화
        /// </summary>
        public Controller()
        {
            protocol = new Protocol();
            protocol.AddProtocolItem(Marshal.SizeOf(typeof(StatusNotification)), true, new CheckFunction(StatusNotificationCheck), new CatchFunction(StatusNotificationCatch));

            unitDataController = new UnitDataController();
            unitDataController.webappUrl    = azureWebAppURL;
            unitDataController.buildingid   = buildingID;
            
            var moduleId    = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");
            string deviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");

            unitDataController.deviceid = $"{deviceId}/{moduleId}";

            comCheckTimer = new Timer();
            comCheckTimer.Interval = 30000;
            
            comCheckTimer.Elapsed += new ElapsedEventHandler(CommCheck);
            comCheckTimer.Enabled = true;        

            StartAsyncSocket(targetIPAddress, targetPort);
        }

        /// <summary>
        /// 생성자
        /// 초기화
        /// </summary>
        public Controller(String elevatorIP, int elevatorPort, String webappAddress, String buildingID, String timeInterval)
        {
            protocol = new Protocol();
            protocol.AddProtocolItem(Marshal.SizeOf(typeof(StatusNotification)), true, new CheckFunction(StatusNotificationCheck), new CatchFunction(StatusNotificationCatch));

            this.targetIPAddress    = elevatorIP;
            this.targetPort         = elevatorPort;
            this.buildingID         = buildingID;
            this.azureWebAppURL     = webappAddress;
            this.deviceID           = deviceID;

            unitDataController = new UnitDataController();

            unitDataController.webappUrl    = azureWebAppURL;
            unitDataController.buildingid   = buildingID;

            var moduleId    = Environment.GetEnvironmentVariable("IOTEDGE_MODULEID");
            string deviceId = Environment.GetEnvironmentVariable("IOTEDGE_DEVICEID");

            deviceID = $"{deviceId}/{moduleId}";
            unitDataController.deviceid = deviceID;

            comCheckTimer = new Timer();
            comCheckTimer.Interval = int.Parse(timeInterval) * 1000;

            comCheckTimer.Elapsed += new ElapsedEventHandler(CommCheck);
            comCheckTimer.Enabled = true;

            Console.WriteLine($"Device ID : {deviceId} Module ID : {moduleId}");

            StartAsyncSocket(targetIPAddress, targetPort);
        }

        /// <summary>
        /// IBS와 엣지간의 통신 상태를 체크하는 Timer 메소드
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CommCheck(object sender, ElapsedEventArgs e)
        {
            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [Controller : CommCheck] Check Commmunication between IBS and Edge");

            if (ClientSocket != null)
            {
                ComHttpPacket comHttpPacket = new ComHttpPacket();

                try
                {
                    var dataType = "";

                    comHttpPacket.building_id   = this.buildingID;                    
                    comHttpPacket.inspection_datetime    = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                    if (ClientSocket.IsConnected)
                    {
                        dataType = "general";
                        comHttpPacket.inspection_result_val = "connected";
                        comHttpPacket.inspection_result_cd  = "0";

                        Task<string> task = Task.Run<string>(async () => await HttpClientTransfer.PostWebAPI(azureWebAppURL, comHttpPacket, this.buildingID, this.deviceID, dataType));
                    }
                    else
                    {
                        dataType = "emergency";
                        comHttpPacket.inspection_result_val = "disconnected";
                        comHttpPacket.inspection_result_cd = "1";

                        Task<string> task = Task.Run<string>(async () => await HttpClientTransfer.PostWebAPI(azureWebAppURL, comHttpPacket, this.buildingID, this.deviceID, dataType));
                    }
                }
                catch(Exception ex)
                {
                    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [Controller : CommCheck] {ex.Message}");
                }
            }
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
                this.targetIPAddress = remoteIPAddress;
                this.targetPort = port;

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

            StatusNotification statusNoti = new StatusNotification();

            this.unitDataController.ReceiveStatus(statusNoti.SetByte(Data));
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

            if (this.comCheckTimer != null && this.comCheckTimer.Enabled)
            {
                this.comCheckTimer.Enabled = false;
                this.comCheckTimer.Dispose();
            }
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

        /// <summary>
        /// 소켓 클라이언트 정리하는 메소드
        /// </summary>
        public void Dispose()
        {
            ClientSocket?.CloseAsynchSocket();

            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
    }
}
