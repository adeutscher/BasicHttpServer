namespace RedShirt.BasicHttpServer.Demo
{
    /// <summary>
    ///     Ticker made for use with Unity projects.
    ///     Tick is intended to be used with Time.deltaTime or Time.fixedDelta time.
    /// </summary>
    public class UnityTicker
    {
        private float _interval;
        private float _tracker;

        public UnityTicker(float interval)
        {
            SetInterval(interval);
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public void SetInterval(float interval)
        {
            _interval = interval;
        }

        public bool Tick(float deltaTime)
        {
            _tracker += deltaTime;
            if (_tracker < _interval)
            {
                return false;
            }

            _tracker -= _interval;
            return true;
        }
    }
}