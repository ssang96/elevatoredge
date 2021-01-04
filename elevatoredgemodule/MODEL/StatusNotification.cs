using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace elevatoredgemodule.MODEL
{
    /// <summary>
    /// 엘리베이터 감시반에서 통합 서버로 보내는 정보
    /// Start를 받은 직후는 모든 호기에 대하여 일괄 전송한다.이후는 아래의 값중 하나라도 변경이 되면 보낸다. (IBS로부터의 응답은 없음)
    /// 
    ///   STX	1 Byte	"S" (엘리베이터), "E" (에스컬레이터)
    ///   호기	2 Bytes 호기번호("01" ~ "99") (ex: 4호기인경우 "04")
    ///   위치	3 Bytes 층위치(예를 들어 "0B9" ~ "118") = (지하9층 ~ 118층까지)  (ex: 1층인 경우 "001", "000"은 없
    ///   방향	1 Byte	"0" = STOP,  "1"  = UP,  "2" = DOWN(ES도 같음)
    ///   도어	1 Byte	"0" = Open,  "1" = Close
    ///   상태  1 Byte	"0" = 정상운전, "1" = 운전휴지, "2" = 독립운전. "3" = 전용운전, "4" = 보수운전 
    ///                 "5" = 정전운전, "6" = 화재운전, "7" = 지진운전, "8" = 고장, "9" = 피난운전   
    ///                 ES의 경우 "0"=정상, "8"=고장
    ///    알람  2 Bytes 알람Code("00"~"99") : Code별 내용은 아래 알람코드 참조 , 비상호출 버튼 추가
    ///    ETX 1 Byte	"E"
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class StatusNotification
    {
        public byte STX;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Unit;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Location;

        public byte Direction;
        public byte Door;
        public byte Status;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] Alarm;

        public byte ETX;

        public StatusNotification()
        {
            this.STX        = 0xCC;
            this.Unit       = new byte[] { 0xCC, 0xCC };
            this.Location   = new byte[] { 0xCC, 0xCC, 0xCC};
            this.Direction  = 0xCC;
            this.Door       = 0xCC;
            this.Status     = 0xCC;
            this.Alarm      = new byte[] { 0xCC, 0xCC };
            this.ETX        = 0xCC;
        }

        public byte[] GetByte()
        {
            byte[] Data = new byte[Marshal.SizeOf(this)];
            IntPtr ptrStruct = IntPtr.Zero;
            try
            {
                ptrStruct = Marshal.AllocHGlobal(Marshal.SizeOf(this));
                Marshal.StructureToPtr(this, ptrStruct, true);
                System.Runtime.InteropServices.Marshal.Copy(ptrStruct, Data, 0, Marshal.SizeOf(this));
            }
            catch (Exception E)
            {
                Debug.WriteLine(E.Message);
            }
            finally
            {
                if (ptrStruct != IntPtr.Zero)
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(ptrStruct);
            }
            return Data;
        }

        public StatusNotification SetByte(byte[] Buffer)
        {
            if (Marshal.SizeOf(typeof(StatusNotification)) != Buffer.Length) throw new Exception("버퍼의 사이즈가 맞지 않습니다.");
            IntPtr ptrStruct = IntPtr.Zero;
            try
            {
                ptrStruct = Marshal.AllocHGlobal(System.Runtime.InteropServices.Marshal.SizeOf(typeof(StatusNotification)));
                System.Runtime.InteropServices.Marshal.Copy(Buffer, 0, ptrStruct, Buffer.Length);
                return (StatusNotification)Marshal.PtrToStructure(ptrStruct, typeof(StatusNotification));
            }
            catch (Exception E)
            {
                Debug.WriteLine(E.Message);
                return null;
            }
            finally
            {
                if (ptrStruct != IntPtr.Zero)
                    System.Runtime.InteropServices.Marshal.FreeHGlobal(ptrStruct);
            }
        }
    }
}
