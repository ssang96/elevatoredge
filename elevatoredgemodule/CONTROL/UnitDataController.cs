using elevatoredgemodule.MODEL;
using elevatoredgemodule.UTIL;
using System;
using System.Collections.Concurrent;
using System.Text;
using System.Threading.Tasks;

namespace elevatoredgemodule.CONTROL
{
    /// <summary>
    /// 수신한 데이터의 변경 사항 체크 및 데이터 전송 클래스
    /// </summary>
    class UnitDataController
    {
        /// <summary>
        /// UnitData 객체를 관리하는 컬렉션 객체
        /// </summary>
        private ConcurrentDictionary<string, UnitData> unitDataCollection = null;

        /// <summary>
        /// 건물 아이디 
        /// </summary>
        public string buildingid = string.Empty;

        /// <summary>
        /// Azure Web App의 주소
        /// </summary>
        public string webappUrl = string.Empty;

        /// <summary>
        /// 디바이스 아이디 및 엣지 모듈 아이디
        /// </summary>
        public string deviceid = string.Empty;
        
        /// <summary>
        /// 생성자
        /// </summary>
        public UnitDataController()
        {
            unitDataCollection = new ConcurrentDictionary<string, UnitData>();      
        }

        /// <summary>
        /// 엘리베이터에서 수신한 데이터를 체크하여 변동 사항 체크 메소드
        /// </summary>
        /// <param name="status"></param>
        public void ReceiveStatus(StatusNotification status)
        {
            UnitData unitData = null;

            //호기 추출
            var unitName = Encoding.UTF8.GetString(status.Unit);

            //호기 정보 조회
            unitDataCollection.TryGetValue(unitName, out unitData);

            //기존에 호기 정보가 있다면, status 비교
            if (unitData != null)       
            {
                var recevieBytesData = Encoding.UTF8.GetString(status.GetByte());
                var previewsBytesData = Encoding.UTF8.GetString(unitData.status.GetByte());

                Console.WriteLine($"Previews : {previewsBytesData.Substring(6, 5)}, Current : {recevieBytesData.Substring(6, 5)}");

                if (previewsBytesData.Substring(6, 5) == recevieBytesData.Substring(6, 5))
                {
                    unitDataCollection[unitName].recevieDate = DateTime.Now;
                }
                else
                {
                    unitDataCollection[unitName].recevieDate = DateTime.Now;
                    unitDataCollection[unitName].status = status;

                    this.SendStatusData(status, unitDataCollection[unitName].recevieDate);
                }
            }
            else
            {
                UnitData unit = new UnitData();
                unit.unitName = unitName;
                unit.recevieDate = DateTime.Now;
                unit.status = status;

                unitDataCollection.TryAdd(unitName, unit);
                
                this.SendStatusData(status, unit.recevieDate);
            }
        }

        /// <summary>
        /// Web App으로 전송하는 메소드
        /// </summary>
        /// <param name="status"></param>
        /// <param name="date"></param>
        private void SendStatusData(StatusNotification status, DateTime date)
        {
            var dataType = "";

            if (Encoding.UTF8.GetString(status.Alarm) == "00")
            {
                dataType = "general";
                Task<string> task = Task.Run<string>(async () => await HttpClientTransfer.PostWebAPI(webappUrl, status, buildingid, deviceid, date, dataType));
            }
            else //긴급
            {
                dataType = "emergency";
                Task<string> emergencyTask = Task.Run<string>(async () => await HttpClientTransfer.PostWebAPI(webappUrl, status, buildingid, deviceid, date, dataType));

                dataType = "general";
                Task<string> generalTask = Task.Run<string>(async () => await HttpClientTransfer.PostWebAPI(webappUrl, status, buildingid, deviceid, date, dataType));
            }
        }
    }
}
