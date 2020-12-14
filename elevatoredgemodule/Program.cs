namespace elevatoredgemodule
{
    using elevatoredgemodule.CONTROL;
    using elevatoredgemodule.MODEL;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;
    using Microsoft.Azure.Devices.Shared;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    class Program
    {

        private static ModuleClient ioTHubModuleClient;

        /// <summary>
        /// 엘리베이터 데이터 수신 및 WebApp으로 데이터 전송하는 클래스 
        /// </summary>
        private static Controller controller;

        /// <summary>
        /// 엘리베이터 서버 아이피
        /// </summary>
        private static string elevatorServerIP;

        /// <summary>
        /// 엘리베이터 서버 포트
        /// </summary>
        private static string elevatorServerPort;

        /// <summary>
        /// 이벤트 데이터를 전송 할 Azure Web App의 주소
        /// </summary>
        private static string azureWebAppAddress;

        /// <summary>
        /// 엘리베이터가 설치된 빌딩의 아이디
        /// </summary>
        private static string buildingID;

        /// <summary>
        /// 모듈의 아이디 
        /// </summary>
        private static string deviceID;

        static void Main(string[] args)
        {
            Init().Wait();          

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }      

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} IoT Hub module client initialized.");

            // Read the TemperatureThreshold value from the module twin's desired properties
            var moduleTwin = await ioTHubModuleClient.GetTwinAsync();
            await OnDesiredPropertiesUpdate(moduleTwin.Properties.Desired, ioTHubModuleClient);

            // Attach a callback for updates to the module twin's desired properties.
            await ioTHubModuleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredPropertiesUpdate, null);

            // Register callback to be called when a direct method is received by IoT Hub
            await ioTHubModuleClient.SetMethodHandlerAsync("control", MethodCallback, ioTHubModuleClient);                   
        }

        static Task OnDesiredPropertiesUpdate(TwinCollection desiredProperties, object userContext)
        {
            try
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Desired property change:");
                Console.WriteLine(JsonConvert.SerializeObject(desiredProperties));
               
                var reportedProperties = new TwinCollection();
                                
                if (desiredProperties["ElevatorServerIP"] != null)
                {
                    elevatorServerIP = desiredProperties["ElevatorServerIP"];
                    reportedProperties["ElevatorServerIP"] = elevatorServerIP;

                    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Elevator Server IP : {elevatorServerIP}");
                }

                if (desiredProperties["ElevatorServerPort"] != null)
                {
                    elevatorServerPort = desiredProperties["ElevatorServerPort"];
                    reportedProperties["ElevatorServerPort"] = elevatorServerPort;

                    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Elevator Server Port : {elevatorServerPort}");
                }

                if (desiredProperties["AzureWebAppAddress"] != null)
                {
                    azureWebAppAddress = desiredProperties["AzureWebAppAddress"];
                    reportedProperties["AzureWebAppAddress"] = azureWebAppAddress;

                    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Azure Web App Address : {azureWebAppAddress}");
                }

                if (desiredProperties["BuildingID"] != null)
                {
                    buildingID = desiredProperties["BuildingID"];
                    reportedProperties["BuildingID"] = buildingID;

                    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Building ID : {buildingID}");
                }

                if (desiredProperties["DeviceID"] != null)
                {
                    deviceID = desiredProperties["DeviceID"];
                    reportedProperties["DeviceID"] = deviceID;

                    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Device ID : {deviceID}");
                }

                if (reportedProperties.Count > 0)
                {
                    ioTHubModuleClient.UpdateReportedPropertiesAsync(reportedProperties).ConfigureAwait(false);
                }

                if (controller != null)
                {
                    controller.Dispose();
                    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Controller Class Disposed");
                    controller = null;
                }
                
                controller = new Controller(elevatorServerIP, int.Parse(elevatorServerPort), azureWebAppAddress, buildingID, deviceID);
            }
            catch (AggregateException ex)
            {
                foreach (Exception exception in ex.InnerExceptions)
                {
                    Console.WriteLine();
                    Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Error when receiving desired property: {0}", exception);
                }
            }            
          
            return Task.CompletedTask;
        }

        /// <summary>
        /// IoTHub에서 Direct Method를 수신하면 발생하는 Callback 함수
        /// </summary>
        /// <param name="methodRequest"></param>
        /// <param name="userContext"></param>
        /// <returns></returns>
        static Task<MethodResponse> MethodCallback(MethodRequest methodRequest, object userContext)
        {   
            string commandType = string.Empty;
            MethodResponse response = null; 

            Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} Received direct method call from IoT Hub {methodRequest.Name}, {Encoding.UTF8.GetString(methodRequest.Data)}");

            var command = JsonConvert.DeserializeObject<Dictionary<string, string>>(Encoding.UTF8.GetString(methodRequest.Data));

            if (command["commandType"] == null)
            {
                response = new MethodResponse(400);
                return Task.FromResult(response);
            }

            commandType = command["commandType"];

            //제어 메세지
            //1. 엘리베이터 서버 접속 IP, PORT 변경
            //2. Azure Web App 접속 URL 변경
            //3. 건물 아이디 변경
            switch(commandType) 
            {               
                case "command":
                    {
                        if (controller != null)
                        {
                            if (command["command"] != null)
                            {
                                //엘리베이터 서버와 통신을 하기 위한 최초 통신 시작 command 전송
                                if(command["command"].Length == (Marshal.SizeOf(typeof(ComCommand))) - 2)
                                {
                                    controller.CommandSendToServer(Encoding.UTF8.GetBytes(command["command"].ToString()));
                                }
                                else if(command["command"].Length == (Marshal.SizeOf(typeof(Command))) - 2)
                                {
                                    controller.CommandSendToServer(Encoding.UTF8.GetBytes(command["command"].ToString()));
                                }
                            }
                        }
                    }
                    break;                    
            }

            response = new MethodResponse(200);         
            return Task.FromResult(response);
        }
    }
}
