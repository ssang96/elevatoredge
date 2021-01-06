namespace elevatoredgemodule.MODEL
{
    /// <summary>
    /// IBS와의 통신 체크 메세지 클래스
    /// </summary>
    class ComHttpPacket
    {
        public string building_id;
        public string inspection_result_cd;
        public string inspection_result_val;
        public string inspection_datetime;
    }
}
