using RedShirt.BasicHttpServer.Structures;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace RedShirt.BasicHttpServer.Demo.Endpoints
{
    /// <summary>
    ///     Accept and display information from the HTTP client.
    /// </summary>
    public class ToastEndpoint : IHttpEndpoint
    {
        private readonly DemoUIHandler _handler;

        public ToastEndpoint(DemoUIHandler handler)
        {
            _handler = handler;
        }

        public HttpMethod Method => HttpMethod.Post; // A PUT might be better, but it's only a demo and this rhymes!
        public string Path => "/toast";

        public HttpResponseMessage Handle(string body, Dictionary<string, string> parameters,
            Dictionary<string, string> headers)
        {
            _handler.AddToast($"HTTP Message: {body}");
            var response = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.Accepted
            };
            return response;
        }
    }
}