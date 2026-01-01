using RedShirt.BasicHttpServer.Structures;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace RedShirt.BasicHttpServer.Demo.Endpoints
{
    /// <summary>
    ///     Return information to the HTTP client.
    /// </summary>
    public class HelloEndpoint : IHttpEndpoint
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
}