namespace elevatoredgemodule.MODEL
{
    /// <summary>
    /// 웹앱으로 데이터 전송 HTTP 프로토콜
    /// </summary>
    class HttpPacket
    {
        public string building_id;
        public string device_id;
        public string event_time;
        public string elevator_number;
        public string type_cd;
        public string floor_value;
        public string direction_value;
        public string door_status;
        public string elevator_status;
        public string alarm_cd;
    }
}
