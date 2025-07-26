using System.Collections.Concurrent;
using UnityEngine;

namespace RedShirt.BasicHttpServer.Demo
{
    public class ToastRegionScript : MonoBehaviour
    {
        private readonly ConcurrentQueue<string> _texts = new();

        [SerializeField]
        private ToastScript _toastScriptPrefab;

        public void Add(string text)
        {
            _texts.Enqueue(text);
        }

        public void Awake()
        {
            Instance = this;
        }

        public void FixedUpdate()
        {
            while (_texts.TryDequeue(out var text))
            {
                var item = Instantiate(_toastScriptPrefab, transform);
                item.SetText(text);
            }
        }

        public static ToastRegionScript Instance { get; private set; }
    }
}