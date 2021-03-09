using elevatoredgemodule.MODEL;
using System;
using System.Collections.Generic;
using System.Text;

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
        /// 엘리베이터에서 수신한 데이터를 체크하여 변동 사항 체크 메소드
        /// </summary>
        /// <param name="status"></param>
        public Tuple<StatusNotification, DateTime> ReceiveStatus(StatusNotification status)
        {
            UnitData unitData = null;

            //호기 추출
            var unitName = Encoding.UTF8.GetString(status.Unit);

            Tuple<StatusNotification, DateTime> returnResult = null;

            //호기 정보 조회
            lock (unitDataCollection)
            {
                unitDataCollection.TryGetValue(unitName, out unitData);

                //기존에 호기 정보가 있다면, status 비교
                if (unitData != null)
                {
                    var recevieBytesData = Encoding.UTF8.GetString(status.GetByte());
                    var previewsBytesData = Encoding.UTF8.GetString(unitData.status.GetByte());

                    if (previewsBytesData.Substring(6, 5) != recevieBytesData.Substring(6, 5))
                    {
                        unitDataCollection[unitName].recevieDate = DateTime.Now;
                        unitDataCollection[unitName].status = status;

                        returnResult = new Tuple<StatusNotification, DateTime>(status, unitDataCollection[unitName].recevieDate);
                    }
                }
                else
                {
                    UnitData unit = new UnitData();
                    unit.unitName = unitName;
                    unit.recevieDate = DateTime.Now;
                    unit.status = status;

                    unitDataCollection.TryAdd(unitName, unit);

                    returnResult = new Tuple<StatusNotification, DateTime>(status, unit.recevieDate);
                }
            }

            return returnResult;
        }
    }
}
