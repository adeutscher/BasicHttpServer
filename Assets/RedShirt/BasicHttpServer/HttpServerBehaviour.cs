using RedShirt.BasicHttpServer.Structures;
using System.Collections.Generic;
using UnityEngine;

namespace RedShirt.BasicHttpServer
{
    public class HttpServerBehaviour : MonoBehaviour
    {
        public void FixedUpdate()
        {
            if (!IsStarted)
            {
                return;
            }

            // TODO: Add in some sort of max time processing per update invocation
            _server?.HandleRequests();
        }

        public bool IsStarted { get; private set; }

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
            if (IsStarted)
            {
                // Already started
                return false;
            }

            _server = new HttpServer(configuration.Port, configuration.DebugErrors);
            foreach (var endpoint in configuration.Endpoints)
            {
                _server.AddEndpoint(endpoint);
            }

            foreach (var validator in configuration.Validators)
            {
                _server.AddValidator(validator);
            }

            if (!_server.Start())
            {
                Debug.LogError($"Failed to start HTTP server on port {configuration.Port}");
                return false;
            }

            IsStarted = true;
            return true;
        }

        public void Stop()
        {
            _server?.Stop();
            IsStarted = false;
        }

        private HttpServer _server;

        public class ConfigurationModel
        {
            public int Port { get; set; }
            public bool DebugErrors { get; set; }
            public List<IHttpEndpoint> Endpoints { get; set; } = new();
            public List<IHttpValidator> Validators { get; set; } = new();
        }
    }
}