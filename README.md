# BasicHttpServer

Basic HTTP server for Unity. Based on Simple HTTP server by David Jeske ([link](https://www.codeproject.com/Articles/137979/Simple-HTTP-Server-in-C)/[GitHub](https://github.com/jeske/SimpleHttpServer)).

## Example

```c#
using RedShirt.BasicHttpServer.Structures;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using UnityEngine;

namespace RedShirt.BasicHttpServer.Demo
{
    public class StartHttpButtonScript : MonoBehaviour
    {
        private HttpServerBehaviour _server;

        public void Awake()
        {
            _server = GetComponent<HttpServerBehaviour>();
        }

        public void OnPress()
        {
            var port = 8080;
            var result = _server.StartServer(new HttpServerBehaviour.ConfigurationModel
            {
                Port = port,
                Endpoints = new List<IHttpEndpoint>
                {
                    new HelloEndpoint(),
                    new ToastEndpoint()
                }
            });

            ToastRegionScript.Instance.Add($"HTTP Started on port {port}: {result}");
        }

        public void Start()
        {
            OnPress();
        }

        private class HelloEndpoint : IHttpEndpoint
        {
            public HttpMethod Method => HttpMethod.Get;
            public string Path => "/hello";

            public HttpResponseMessage Handle(string body, Dictionary<string, string> parameters,
                Dictionary<string, string> headers)
            {
                var response = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Hello World")
                };
                return response;
            }
        }

        private class ToastEndpoint : IHttpEndpoint
        {
            public HttpMethod Method => HttpMethod.Post; // A PUT might be better, but it's only a demo and it rhymes!
            public string Path => "/toast";

            public HttpResponseMessage Handle(string body, Dictionary<string, string> parameters,
                Dictionary<string, string> headers)
            {
                ToastRegionScript.Instance.Add($"HTTP Message: {body}");
                var response = new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.Accepted
                };
                return response;
            }
        }
    }
}
```
