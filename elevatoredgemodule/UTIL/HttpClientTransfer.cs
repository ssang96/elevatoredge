using elevatoredgemodule.MODEL;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace elevatoredgemodule.UTIL
{
    /// <summary>
    /// HttpClient를 사용해서 Azure Portal의 Web App으로 데이터를 전송하는 클래스
    /// </summary>
    class HttpClientTransfer
    {
        private ServiceProvider serviceProvider = null;
        private IHttpClientFactory httpClientFactory = null;

        public HttpClientTransfer()
        {
            serviceProvider = new ServiceCollection().AddHttpClient().BuildServiceProvider();            
        }

        /// <summary>
        /// Azure에 구축된 Web App으로 데이터를 전송하는 함수
        /// </summary>
        /// <param name="webappURL"></param>
        /// <param name="recevieData"></param>
        /// <param name="buildingID"></param>
        /// <param name="deviceID"></param>
        /// <param name="dates"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<string> PostWebAPI(string webappURL, StatusNotification recevieData, string buildingID, string deviceID, DateTime dates, string type)
        {
            string result = string.Empty;
            var packet = new HttpPacket();
            packet.building_id = buildingID;
            packet.device_id = deviceID;
            packet.event_time = dates.ToString("yyyy-MM-dd HH:mm:ss");
            
            try
            {               
                packet.elevator_number  = Encoding.UTF8.GetString(recevieData.Unit);
                packet.type_cd          = Convert.ToChar(recevieData.STX).ToString();
                packet.floor_value      = Encoding.UTF8.GetString(recevieData.Location);
                packet.direction_value  = Convert.ToChar(recevieData.Direction).ToString();
                packet.door_status      = Convert.ToChar(recevieData.Door).ToString();
                packet.elevator_status  = Convert.ToChar(recevieData.Status).ToString();
                packet.alarm_cd         = Encoding.UTF8.GetString(recevieData.Alarm);
               
                string json = JsonConvert.SerializeObject(packet);              

                StringContent data = new StringContent(json, Encoding.UTF8, "application/json");
                httpClientFactory = serviceProvider.GetService<IHttpClientFactory>();
                
                var client = httpClientFactory.CreateClient();                
                client.DefaultRequestHeaders.Add("type", type);
                client.Timeout = TimeSpan.FromSeconds(60);

                var response = await client.PostAsync(new Uri(webappURL + "/event/elevator/status"), data);
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [HttpClientTransfer : status] {json} Send To WebApp ");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [status error] {ex.Message}");
                result = ex.Message;
            }

            return result;
        }

        /// <summary>
        /// Azure에 구축된 Web App으로 IBS와의 통신 상태 데이터를 전송하는 함수
        /// </summary>
        /// <param name="webappURL"></param>
        /// <param name="comStatus"></param>
        /// <param name="buildingID"></param>
        /// <param name="deviceID"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public async Task<string> PostWebAPI(string webappURL, ComHttpPacket comStatus, string buildingID, string deviceID, string type)
        {
            string result = string.Empty;

            try
            {               
                string json = JsonConvert.SerializeObject(comStatus);
                
                StringContent data = new StringContent(json, Encoding.UTF8, "application/json");

                var client = httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Add("type", type);    
               
                var response = await client.PostAsync(new Uri(webappURL + "/event/elevator/health"), data);
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [HttpClientTransfer : health] {json} Send To WebApp");               
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [health error] {ex.Message}");
                result = ex.Message;
            }

            return result;
        }
    }
}
