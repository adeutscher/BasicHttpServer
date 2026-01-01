using RedShirt.BasicHttpServer.Demo.Endpoints;
using RedShirt.BasicHttpServer.Demo.Validators;
using RedShirt.BasicHttpServer.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

namespace RedShirt.BasicHttpServer.Demo
{
    public class DemoUIHandler : MonoBehaviour
    {
        public void AddToast(string text)
        {
            var textElement = new TextElement
            {
                text = text
            };
            var toast = new Toast
            {
                Text = textElement,
                CreatedAt = DateTime.UtcNow
            };
            _toastContentElement.Add(textElement);
            _toasts.Add(toast);
        }

        public void Awake()
        {
            _httpServerBehaviour = GetComponent<HttpServerBehaviour>();
        }

        private readonly UnityTicker CheckToastExpiryTicker = new(0.5f);

        private void ClickHttpButton()
        {
            if (_httpServerBehaviour.IsStarted)
            {
                _httpServerBehaviour.Stop();
                _httpToggleButton.text = "Turn On HTTP";
            }
            else
            {
                _httpServerBehaviour.StartServer(new HttpServerBehaviour.ConfigurationModel
                {
                    DebugErrors = false,
                    Port = 8080,
                    Endpoints = new List<IHttpEndpoint>
                    {
                        new HelloEndpoint(),
                        new ToastEndpoint(this)
                    },
                    Validators = new List<IHttpValidator>
                    {
                        new BasicHttpAddressValidator("127.0.0.1", this)
                        //, new BasicHttpAuthorizationValidator("foo", "bar")
                    }
                });
                _httpToggleButton.text = "Turn Off HTTP";
            }
        }

        private void ClickTestButton()
        {
            Debug.Log("ClickTestButton");
            AddToast("Test");
        }

        public void FixedUpdate()
        {
            // ReSharper disable once InvertIf
            if (CheckToastExpiryTicker.Tick(Time.fixedDeltaTime))
            {
                var ts = TimeSpan.FromSeconds(6);
                var toRemove = _toasts.Where(t => t.CreatedAt + ts < DateTime.UtcNow).ToList();
                foreach (var currentToast in toRemove)
                {
                    _toastContentElement.Remove(currentToast.Text);
                    _toasts.Remove(currentToast);
                }
            }
        }

        private void OnEnable()
        {
            _uiDocument = GetComponent<UIDocument>();

            var root = _uiDocument.rootVisualElement;

            if (root.Q<Button>("TestToast") is { } testButton)
            {
                testButton.clicked += ClickTestButton;
            }

            if (root.Q<Button>("ToggleHttp") is { } httpButton)
            {
                _httpToggleButton = httpButton;
                httpButton.clicked += ClickHttpButton;
            }

            if (root.Q<VisualElement>("ToastRegion") is { } toastContent)
            {
                _toastContentElement = toastContent;
            }
        }

        private HttpServerBehaviour _httpServerBehaviour;

        private TextElement _httpToggleButton;
        private VisualElement _toastContentElement;

        private readonly List<Toast> _toasts = new();
        private UIDocument _uiDocument;

        private class Toast
        {
            public TextElement Text { get; set; }
            public DateTime CreatedAt { get; set; }
        }
    }
}