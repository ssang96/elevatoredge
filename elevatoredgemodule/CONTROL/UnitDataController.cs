using elevatoredgemodule.MODEL;
using elevatoredgemodule.UTIL;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace elevatoredgemodule.CONTROL
{
    class UnitDataController
    {
        /// <summary>
        /// UnitData 객체를 관리하는 컬렉션 객체
        /// </summary>
        private Dictionary<string, UnitData> unitDataCollection = null;

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
            unitDataCollection = new Dictionary<string, UnitData>();      
        }

        /// <summary>
        /// 엘리베이터에서 수신한 데이터를 체크하여 변동 사항이 있는 데이터만 Web App으로 전송하는 메소드
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
                if (unitData.status == status)
                {
                    unitDataCollection[unitName].recevieDate = DateTime.Now;
                }
                else
                {
                    unitDataCollection[unitName].recevieDate = DateTime.Now;
                    unitDataCollection[unitName].status = status;

                    //변동 사항이 있으므로 Web App으로 전송
                    Task<string> task = Task.Run<string>(async() => await HttpClientTransfer.PostWebAPI(webappUrl, status, buildingid, deviceid, unitDataCollection[unitName].recevieDate));
                }
            }
            else
            {
                UnitData unit = new UnitData();
                unit.unitName = unitName;
                unit.recevieDate = DateTime.Now;
                unit.status = status;

                unitDataCollection.Add(unitName, unit);

                //신규 호기 데이터이므로 Web App으로 전송
                Task<string> task = Task.Run<string>(async () => await HttpClientTransfer.PostWebAPI(webappUrl, status, buildingid, deviceid, unitDataCollection[unitName].recevieDate));
            }
        }
    }
}
