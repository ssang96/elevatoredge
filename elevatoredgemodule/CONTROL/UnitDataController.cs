using elevatoredgemodule.MODEL;
using elevatoredgemodule.UTIL;
using System;
using System.Collections.Concurrent;
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
        public void ReceiveStatus(string status)
        {
            UnitData unitData = null;

            //호기 추출
            var unitName = status.Substring(1, 2);
            
            //호기 정보 조회
            unitDataCollection.TryGetValue(unitName, out unitData);

            //기존에 호기 정보가 있다면, status 비교
            if (unitData != null)       
            {
                if (unitData.status.Substring(6) == status.Substring(6))
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
        private void SendStatusData(string status, DateTime date)
        {
            if (status.Substring(9, 2) == "00")
            {
                var dataType = "general";
                Task<string> task = Task.Run<string>(async () => await HttpClientTransfer.PostWebAPI(webappUrl, status, buildingid, deviceid, date, dataType));
            }
            else //긴급
            {
                var dataType = "emergency";
                Task<string> task = Task.Run<string>(async () => await HttpClientTransfer.PostWebAPI(webappUrl, status, buildingid, deviceid, date, dataType));
            }
        }
    }
}
