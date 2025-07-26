using System.Collections.Generic;
using System.Net.Http;

namespace RedShirt.BasicHttpServer.Structures
{
    public interface IHttpEndpoint
    {
        HttpMethod Method { get; }
        string Path { get; }

        HttpResponseMessage Handle(string body, Dictionary<string, string> parameters,
            Dictionary<string, string> headers);
    }
}