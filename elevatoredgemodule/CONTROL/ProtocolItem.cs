using elevatoredgemodule.UTIL;

namespace elevatoredgemodule.CONTROL
{
    /// <summary> Check Function Delegate </summary>
    public delegate bool CheckFunction(object sender, CircleQueue m_Queue);
    /// <summary> Catch Function Delegate </summary>
    public delegate bool CatchFunction(object sender, byte[] Data);

    /// <summary>
    /// ProtocolItem 
    /// </summary>
    /// <remarks>
    /// 내부 큐버퍼를 가지며 데이터가 들어 올때 Delegate를 호출한다.
    /// </remarks>
    public class ProtocolItem
    {
        /// <summary> 
        /// 프로토콜 검사를 할 것인지 설정 
        /// </summary>
        private bool m_bUsing;

        /// <summary> 
        /// 내부 큐 버퍼 
        /// </summary>
        private CircleQueue m_Queue = null;

        /// <summary> 
        /// Check Delegate  
        /// </summary>
        private CheckFunction OnCheckFunc = null;

        /// <summary> 
        /// Catch Delegate 
        /// </summary>
        private CatchFunction OnCatchFunc = null;

        public CircleQueue Queue
        {
            get { return m_Queue; }
        }

        public CheckFunction OnCheck
        {
            get { return OnCheckFunc; }
            set { OnCheckFunc = value; }
        }

        public CatchFunction OnCatch
        {
            get { return OnCatchFunc; }
            set { OnCatchFunc = value; }
        }

        public bool Using
        {
            get { return this.m_bUsing; }
            set { this.m_bUsing = value; }
        }

        public ProtocolItem()
        {
            m_Queue = null;
            m_bUsing = false;
            m_Queue = new CircleQueue();
        }

        /// <summary>
        /// 데이터를 내부 큐에 넣고 Delegate를 호출한다.
        /// </summary>
        /// <param name="data"> 데이터 </param>
        public void ProcessingData(byte data)
        {
            // 내부 큐 버퍼에 데이터를 넣는다.
            m_Queue.PutData(data);

            if (this.m_bUsing == true)
            {
                // Delegate를 호출한다.
                if (Check() == true)
                    Catch();
            }
        }

        /// <summary>
        /// Check 함수( Delegate CheckFunction을 호출한다. )
        /// </summary>
        /// <returns> Delegate의 리턴 값</returns>
        public virtual bool Check()
        {
            if (OnCheckFunc == null) return false;
            return OnCheckFunc(this, m_Queue);
        }

        /// <summary>
        /// Catch 함수( Delegate CatchFunction을 호출한다. )
        /// </summary>
        /// <returns> Delegate CatchFunction의 리턴 값</returns>
        public virtual bool Catch()
        {
            if (OnCatchFunc == null) return false;
            // 데이터를 순서 대로 배열에 넣은 후에 delegate 호출
            byte[] data = new byte[m_Queue.Size];
            for (int i = 0; i < m_Queue.Size; i++) data[i] = m_Queue.Buffer[(m_Queue.Sp + i + m_Queue.Size) % m_Queue.Size];
            return OnCatchFunc(this, data);
        }
    }
}