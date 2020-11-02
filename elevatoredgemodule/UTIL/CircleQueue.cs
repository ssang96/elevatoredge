namespace elevatoredgemodule.UTIL
{
    /// <summary>
    /// CQueue에 대한 요약 설명입니다.
    /// </summary>
    public class CircleQueue
    {
        /// <summary>  
        /// 현재 버퍼의 데이터를 넣을 위치
        /// </summary>
        private int m_nSp;

        /// <summary> 
        /// 버퍼의 사이즈 
        /// </summary>
        private int m_nSize;

        /// <summary> 
        /// 버퍼 배열 
        /// </summary>
        private byte[] m_Buffer;

        /// <summary> 
        /// m_Sp의 속성 
        /// </summary>
        /// <seealso cref="m_Sp"> m_Sp의 속성 참고 </seealso>
        public int Sp
        {
            get { return m_nSp; }
            set { m_nSp = value; }
        }

        /// <summary> 
        /// m_Size의 속성 
        /// </summary>
        /// <seealso cref="m_Size"> m_Size 참고 </seealso>
        public int Size
        {
            get { return m_nSize; }
            set { m_nSize = value; }
        }
        public byte[] Buffer
        {
            get { return m_Buffer; }
        }

        /// <summary>
        /// CQueue생성자.
        /// </summary>
        public CircleQueue()
        {
            m_nSize = 0;
            m_nSp = 0;
            m_Buffer = null;
        }

        /// <summary>
        /// 버퍼의 사이즈 설정
        /// </summary>
        /// <param name="Size">버퍼의 배열 크기</param>
        /// <remarks>
        /// 버퍼의 크기를 설정하면 m_Sp = 0
        /// m_Size = Size로 설정되고
        /// 배열의 함수 Initialize()를 호출한다.
        /// </remarks>
        public void SetSize(int Size)
        {
            m_nSize = Size;
            m_nSp = 0;
            m_Buffer = null;
            m_Buffer = new byte[Size];
            m_Buffer.Initialize();
        }

        /// <summary>
        /// 버퍼에 데이터를 넣는다.
        /// </summary>
        /// <param name="Data">버퍼에 넣을 데이터(byte)</param>
        /// <returns>없음</returns>
        public void PutData(byte Data)
        {
            // 버퍼 크기가 정해져 있지 않을 경우를 대비해서 예외를 집어 넣었다.
            if (m_Buffer == null)
                throw new System.Exception("버퍼 크기가 설정 되어 있지 않습니다.");
            m_Buffer[m_nSp++] = Data;
            // 배열의 범위를 넘어가면 처음으로 포인터를 이동시킨다.
            if (m_nSp >= m_nSize)
                m_nSp = 0;
        }
    }
}
