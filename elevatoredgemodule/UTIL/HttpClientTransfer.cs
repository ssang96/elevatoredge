using elevatoredgemodule.MODEL;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace elevatoredgemodule.UTIL
{
    /// <summary>
    /// HttpClient를 사용해서 Azure Portal의 Web App으로 데이터를 전송하는 클래스
    /// </summary>
    class HttpClientTransfer
    {   
        /// <summary>
        /// Azure에 구축된 Web App으로 데이터를 전송하는 함수
        /// </summary>
        /// <param name="ReceivedData"></param>
        /// <returns></returns>
        public static async Task<string> PostWebAPI(string webappURL, StatusNotification recevieData, string buildingID, string deviceID, DateTime dates, string type)
        {
            string result = string.Empty;
            var packet = new HttpPacket();

            packet.building_id  = buildingID;
            packet.device_id    = deviceID;
            packet.event_time   = dates.ToString("yyyy-MM-dd HH:mm:ss");

            HttpClient client = null;

            try
            {
                //StatusNotification 데이터를 SK C&C 요청으로 분류
                packet.elevator_number  = Encoding.UTF8.GetString(recevieData.Unit);
                packet.type_cd          = Convert.ToChar(recevieData.STX).ToString();
                packet.floor_value      = Encoding.UTF8.GetString(recevieData.Location);
                packet.direction_value  = Convert.ToChar(recevieData.Direction).ToString();
                packet.door_status      = Convert.ToChar(recevieData.Door).ToString();
                packet.elevator_status  = Convert.ToChar(recevieData.Status).ToString();
                packet.alarm_cd         = Encoding.UTF8.GetString(recevieData.Alarm);

                //Converting the object to a json string. NOTE: Make sure the object doesn't contain circular references.
                string json = JsonConvert.SerializeObject(packet);

                //Needed to setup the body of the request
                StringContent data = new StringContent(json, Encoding.UTF8, "application/json");

                if (webappURL.ToUpper().Contains("HTTPS"))
                {
                    var handler = new HttpClientHandler()
                    {
                        SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls
                    };

                    client = new HttpClient(handler);
                }
                else
                {
                    client = new HttpClient();
                }

                client.DefaultRequestHeaders.Add("type", type);

                //Pass in the full URL and the json string content
                var response = await client.PostAsync(new Uri(webappURL + "/event/elevator/status"), data);

                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [HttpClientTransfer : PostWebAPI] {json} Send To WebApp");

                result = response.StatusCode.ToString();
                
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [HttpClientTransfer : PostWebAPI] {response.StatusCode} Received From WebApp");

                client.Dispose();
                client = null;
            }
            catch(Exception ex)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [PostWebAPI Connected Error] {ex.Message}");
                result = ex.Message;
            }
            finally
            {
                if (client != null)
                    client.Dispose();
            }

            return result;
        }

        public static async Task<string> PostWebAPI(string webappURL, ComHttpPacket comStatus, string buildingID, string deviceID, string type)
        {
            string result = string.Empty;
                                   
            HttpClient client = null;

            try
            {               
                //Converting the object to a json string. NOTE: Make sure the object doesn't contain circular references.
                string json = JsonConvert.SerializeObject(comStatus);

                //Needed to setup the body of the request
                StringContent data = new StringContent(json, Encoding.UTF8, "application/json");

                if (webappURL.ToUpper().Contains("HTTPS"))
                {
                    var handler = new HttpClientHandler()
                    {
                        SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls
                    };

                    client = new HttpClient(handler);
                }
                else
                {
                    client = new HttpClient();
                }

                client.DefaultRequestHeaders.Add("type", type);

                //Pass in the full URL and the json string content
                var response = await client.PostAsync(new Uri(webappURL + "/event/elevator/health"), data);

                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [HttpClientTransfer : PostWebAPI] {json} Send To WebApp");

                result = response.StatusCode.ToString();

                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [HttpClientTransfer : PostWebAPI] {response.StatusCode} Received From WebApp");

                client.Dispose();
                client = null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [PostWebAPI Connected Error] {ex.Message}");
                result = ex.Message;
            }
            finally
            {
                if (client != null)
                    client.Dispose();
            }

            return result;
        }
    }
}
