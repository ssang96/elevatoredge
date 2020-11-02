using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace elevatoredgemodule.MODEL
{
    /// <summary>
    /// IBS에서 Elevator서버로의 Packet format
    /// Number Separat or Sub Name Separator   Status ETX
    /// Field STX IBS IP Address Separator IBS Port Number  Separator  Sub Name    Separator Status  ETX
    /// bytes  1 	   15	           1	        4	           1	     8	        1	   1	  1
    /// ex)   0x02	"127.000.000.001  "!"	     "1234"	          "!"	"Elevator"	   "!"	  "1"	 0x03
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class ComCommand
    {
        public byte STX;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 15)]
        public byte[] IpAddress;

        public byte Seperator1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] PortNumber;

        public byte Seperator2;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] SubName;

        public byte Seperator3;

        /*
         * Status Code	Data	행위내용
         *    Stop	    ‘0’	    IBS시스템으로 데이터 송신을 중단한다.
         *    Start     ‘1’	    IBS시스템으로 데이터 송신을 재개한다.
         * ISB시스템이 엘리베이터 시스템의 alive 상태 확인으로 사용한 Refresh	'2'	엘리베이터 시스템의 데이터를 갱신하도록 요청한다.
         */
        public byte Status;

        public byte ETX;

        /// <summary>
        /// 생성자 
        /// 초기화 
        /// </summary>
        public ComCommand()
        {
            this.STX = 0x02;
            this.IpAddress = new byte[] {0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC,0xCC, 0xCC, 0xCC, 0xCC, 0xCC };
            this.Seperator1 = (byte) '!';
            this.PortNumber = new byte[] { 0xCC, 0xCC, 0xCC, 0xCC };
            this.Seperator2 = (byte)'!';
            this.SubName = new byte[] { 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC, 0xCC };
            this.SubName = System.Text.Encoding.UTF8.GetBytes("Elevator");
            this.Seperator3 = (byte)'!';
            this.ETX = 0x03;
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

        public static ComCommand SetByte(byte[] Buffer)
        {
            if (Marshal.SizeOf(typeof(ComCommand)) != Buffer.Length) throw new Exception("버퍼의 사이즈가 맞지 않습니다.");
            IntPtr ptrStruct = IntPtr.Zero;
            try
            {
                ptrStruct = Marshal.AllocHGlobal(System.Runtime.InteropServices.Marshal.SizeOf(typeof(ComCommand)));
                System.Runtime.InteropServices.Marshal.Copy(Buffer, 0, ptrStruct, Buffer.Length);
                return (ComCommand)Marshal.PtrToStructure(ptrStruct, typeof(ComCommand));
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
