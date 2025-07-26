using RedShirt.BasicHttpServer;
using RedShirt.BasicHttpServer.Structures;
using System.Collections.Generic;
using UnityEngine;

namespace Plugins.RedShirt.BasicHttpServer
{
    public class HttpServerBehaviour : MonoBehaviour
    {
        private bool _isStarted;

        private HttpServer _server;

        public void FixedUpdate()
        {
            if (!_isStarted)
            {
                return;
            }

            // TODO: Add in some sort of max time processing per update invocation
            _server?.HandleRequests();
        }

        public void OnDestroy()
        {
            Stop();
        }

        public void OnDisable()
        {
            Stop();
        }

        public bool StartServer(ConfigurationModel configuration)
        {
            if (_isStarted)
            {
                // Already started
                return false;
            }

            _server = new HttpServer(configuration.Port, configuration.DebugErrors);
            foreach (var endpoint in configuration.Endpoints)
            {
                _server.AddEndpoint(endpoint);
            }

            if (!_server.Start())
            {
                Debug.LogError($"Failed to start HTTP server on port {configuration.Port}");
                return false;
            }

            _isStarted = true;
            return true;
        }

        internal void Stop()
        {
            _server?.Stop();
        }

        public class ConfigurationModel
        {
            public int Port { get; set; }
            public bool DebugErrors { get; set; }
            public List<IHttpEndpoint> Endpoints { get; set; } = new();
        }
    }
}