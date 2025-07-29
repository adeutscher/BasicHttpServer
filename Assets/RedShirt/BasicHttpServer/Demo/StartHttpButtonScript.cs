using RedShirt.BasicHttpServer.Responses;
using RedShirt.BasicHttpServer.Structures;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
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
                },
                Validators = new List<IHttpValidator>
                {
                    new BasicHttpAddressValidator("127.0.0.1")
                    //, new BasicHttpAuthorizationValidator("foo", "bar")
                }
            });

            ToastRegionScript.Instance.Add($"HTTP Started on port {port}: {result}");
        }

        public void Start()
        {
            OnPress();
        }

        /// <summary>
        ///     Demo of a validator
        /// </summary>
        private class BasicHttpAddressValidator : IHttpValidator
        {
            private readonly string _acceptedAddress;

            public BasicHttpAddressValidator(string acceptedAddress)
            {
                _acceptedAddress = acceptedAddress;
            }

            public Task<ValidatorResponse> ValidateAsync(SimpleHttpRequest request)
            {
                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (request.SourceAddress == _acceptedAddress)
                {
                    return Task.FromResult(ValidatorResponse.Ok);
                }

                ToastRegionScript.Instance.Add($"Rejected HTTP request from '{request.SourceAddress}'");
                return Task.FromResult(ValidatorResponse.Forbidden);
            }
        }

        /// <summary>
        ///     Return information to the HTTP client.
        /// </summary>
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

        /// <summary>
        ///     Accept and display information from the HTTP client.
        /// </summary>
        private class ToastEndpoint : IHttpEndpoint
        {
            public HttpMethod Method => HttpMethod.Post; // A PUT might be better, but it's only a demo and this rhymes!
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