namespace CosmicClash.Utils
{
    public class NetworkTimer
    {
        private float m_timer;
        public float MinTimeBetweenTicks { get; }
        public int CurrentTick { get; private set; }

        public NetworkTimer(float serverTickRate)
        {
            MinTimeBetweenTicks = 1f / serverTickRate;
        }

        public void Update(float deltaTime)
        {
            m_timer += deltaTime;
        }

        public bool ShouldTick()
        {
            if (m_timer >= MinTimeBetweenTicks)
            {
                m_timer -= MinTimeBetweenTicks;
                CurrentTick++;
                return true;
            }

            return false;
        }
    }
}