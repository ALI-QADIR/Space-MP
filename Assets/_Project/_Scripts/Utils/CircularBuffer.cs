namespace CosmicClash.Utils
{
    public class CircularBuffer<T>
    {
        private T[] m_buffer;
        private readonly int _bufferSize;

        public CircularBuffer(int bufferSize)
        {
            _bufferSize = bufferSize;
            m_buffer = new T[_bufferSize];
        }

        public void Add(T item, int index) => m_buffer[index % _bufferSize] = item;

        public T Get(int index) => m_buffer[index % _bufferSize];

        public void Clear() => m_buffer = new T[_bufferSize];
    }
}