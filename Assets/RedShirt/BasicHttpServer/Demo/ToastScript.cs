using TMPro;
using UnityEngine;

namespace RedShirt.BasicHttpServer.Demo
{
    public class ToastScript : MonoBehaviour
    {
        private TMP_Text _text;
        private readonly UnityTicker _ticker = new(6f);

        public void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        public void FixedUpdate()
        {
            if (_ticker.Tick(Time.fixedDeltaTime))
            {
                Destroy(gameObject);
            }
        }

        public void SetText(string text)
        {
            _text.text = text; // Text!
        }
    }
}