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
        public static async Task<string> PostWebAPI(string webappURL, string recevieData, string buildingID, string deviceID, DateTime dates)
        {
            string result = string.Empty;
            var packet = new HttpPacket();

            packet.building_id  = buildingID;
            packet.device_id    = deviceID;
            packet.event_time   = dates.ToString("yyyy-MM-dd HH:mm:ss");
            packet.receive_data = recevieData;

            HttpClient client = null;

            try
            {
                //Converting the object to a json string. NOTE: Make sure the object doesn't contain circular references.
                string json = JsonConvert.SerializeObject(packet);

                //Needed to setup the body of the request
                StringContent data = new StringContent(json, Encoding.UTF8, "application/json");

                var handler = new HttpClientHandler()
                {
                    SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls
                };

                client = new HttpClient(handler);

                //Pass in the full URL and the json string content
                var response = await client.PostAsync(new Uri(webappURL), data);

                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [HttpClientTransfer : PostWebAPI] {json} Send To WebApp");

                result = response.StatusCode.ToString();
                
                Console.WriteLine($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [HttpClientTransfer : PostWebAPI] {response.StatusCode} Received From WebApp");                
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
    }
}
