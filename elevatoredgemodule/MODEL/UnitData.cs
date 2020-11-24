using System;

namespace elevatoredgemodule.MODEL
{
    /// <summary>
    /// 엘리베이터 또는 에스컬레이터 수신 정보 클래스
    /// </summary>
    class UnitData
    {
        /// <summary>
        /// 데이터를 수신한 시간
        /// </summary>
        public DateTime recevieDate { get; set; }

        /// <summary>
        /// 엘리베이터 또는 에스컬레이터 호기
        /// </summary>
        public string unitName { get; set; }

        /// <summary>
        /// IBS로부터 수신한 이벤트 데이터
        /// </summary>
        public string status { get; set; }
    }
}
