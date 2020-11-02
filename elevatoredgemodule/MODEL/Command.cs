using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace elevatoredgemodule.MODEL
{
    /// <summary>
    /// IBS에서 Elevator서버로의 Packet format
    /// Number Separat or Sub Name Separator   Status ETX
    /// Field STX Sub System Name Separator Car No Separator Address(word)   Separator Value(byte) Seperator  ETX
    /// bytes   1	   7	         1	       2	  1	         4	              1	        2	       1	   1
    ///  ex)  0x02	"Command"	    "|"	     '01'	 "|"	'0000' ~ 'FFFF'	     "|"	'00'~'FF'	"|"	      0x03
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class Command
    {
        public byte STX;

        /* Sub System Name : ‘Command’ */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public byte[] SubSystemName;

        public byte Seperator1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] CarNo;

        public byte Seperator2;

        /*
         * Address/Value pair : ex) '0101' : 화재명령 '02' : ON, '00' : OFF
         * Ex) STX "Command|01|0101|02|" ETX : 1호기로 0x0101 (화재관제) 를 요청(x02)한다.
         */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] Address;

        public byte Seperator3;

        /*
         * Address	Value	행위내용
         *  '0101'	'02'	엘리베이터 시스템에 소방(화재)관제를 요청한다.
         *  '0101'	'00'	엘리베이터 시스템에 소방(화재)관제 해제를 요청한다.
         *  '002C'	'FF'	엘리베이터 시스템에 정전운전 관제를 요청한다.
         *  '002C'	'00'	엘리베이터 시스템에 정전운전 관제 해제를 요청한다.
         *  '0100'	'FF'	엘리베이터 시스템에 지진운전 관제를 요청한다.
         *  '0100'	'00'	엘리베이터 시스템에 지진운전 관제 해제를 요청한다.
         */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] value;

        public byte Seperator4;

        public byte ETX;

        /// <summary>
        /// 생성자 
        /// 초기화 
        /// </summary>
        public Command()
        {
            this.STX = 0x02;
            this.SubSystemName = new byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            this.SubSystemName = System.Text.Encoding.UTF8.GetBytes("Command");
            this.Seperator1 = (byte)'|';
            this.CarNo = new byte[] { 0xCC, 0xCC };
            this.Seperator2 = (byte)'|';
            this.Address = new byte[] { 0xCC, 0xCC, 0xCC, 0xCC };
            this.Seperator3 = (byte)'|';
            this.value = new byte[] { 0xCC, 0xCC };
            this.Seperator4 = (byte)'!';
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

        public static Command SetByte(byte[] Buffer)
        {
            if (Marshal.SizeOf(typeof(Command)) != Buffer.Length) throw new Exception("버퍼의 사이즈가 맞지 않습니다.");
            IntPtr ptrStruct = IntPtr.Zero;
            try
            {
                ptrStruct = Marshal.AllocHGlobal(System.Runtime.InteropServices.Marshal.SizeOf(typeof(Command)));
                System.Runtime.InteropServices.Marshal.Copy(Buffer, 0, ptrStruct, Buffer.Length);
                return (Command)Marshal.PtrToStructure(ptrStruct, typeof(Command));
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
