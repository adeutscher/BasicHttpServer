using System;
using System.Collections.Generic;
using System.Net.Http;

namespace RedShirt.BasicHttpServer.Structures
{
    public class SimpleHttpRequest
    {
        public string SourceAddress { get; set; }
        public Guid RequestId { get; set; }
        public string Path { get; set; }
        public HttpMethod Method { get; set; }
        public string Body { get; set; }
        public Dictionary<string, string> Parameters { get; set; }
        public Dictionary<string, string> Headers { get; set; }
    }
}