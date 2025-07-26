using System;
using System.Net.Http;

namespace RedShirt.BasicHttpServer.Structures
{
    public class SimpleHttpResponse
    {
        public Guid RequestId { get; set; }
        public HttpResponseMessage HttpResponse { get; set; }
    }
}