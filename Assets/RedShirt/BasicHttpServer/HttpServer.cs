using JetBrains.Annotations;
using RedShirt.BasicHttpServer.Exceptions;
using RedShirt.BasicHttpServer.Responses;
using RedShirt.BasicHttpServer.Structures;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using UnityEngine;

namespace RedShirt.BasicHttpServer
{
    // Based on Simple HTTP server by David Jeske.
    // https://www.codeproject.com/Articles/137979/Simple-HTTP-Server-in-C
    // simple HTTP explanation
    // http://www.jmarshall.com/easy/http/

    public class HttpServer
    {
        private const int BufSize = 4096;
        private readonly ConcurrentDictionary<Guid, TcpClient> _clients = new();

        private readonly bool _debugErrors;

        private readonly Dictionary<HttpMethod, Dictionary<string, IHttpEndpoint>> _endpoints = new();

        private readonly ConcurrentQueue<SimpleHttpRequest> _messagesToHandle = new();

        private readonly ConcurrentQueue<TcpClient> _newClients = new();
        private readonly AutoResetEvent _newClientsEvent = new(false);

        private readonly int _port;
        private readonly ConcurrentQueue<SimpleHttpResponse> _responses = new();
        private readonly AutoResetEvent _responsesEvent = new(false);

        private readonly List<IHttpValidator> _validators = new();

        private bool _isActive = true;
        private TcpListener _listener;

        [CanBeNull]
        private IHttpEndpoint FindEndpoint(SimpleHttpRequest request)
        {
            // ReSharper disable once ConvertIfStatementToReturnStatement
            if (!_endpoints.TryGetValue(request.Method, out var endpointsForMethod))
            {
                return null;
            }

            return endpointsForMethod.GetValueOrDefault(request.Path);
        }

        private static async Task<string> StreamReadLineAsync(Stream inputStream)
        {
            var data = "";
            while (true)
            {
                var nextChar = inputStream.ReadByte();
                if (nextChar == '\n')
                {
                    break;
                }

                if (nextChar == '\r')
                {
                    continue;
                }

                if (nextChar == -1)
                {
                    await Task.Delay(1);
                    continue;
                }

                data += Convert.ToChar(nextChar);
            }

            return data;
        }

        private static async Task<string> GetContentAsync(BufferedStream inputStream,
            Dictionary<string, string> headers)
        {
            var contentLength = headers.TryGetValue("Content-Length", out var contentLengthString)
                                && int.TryParse(contentLengthString, out var contentLengthParsed)
                                && contentLengthParsed > 0
                ? contentLengthParsed
                : 0;

            if (contentLength > 0)
            {
                return await DoGetRequestBodyByContentLengthAsync(inputStream, contentLength);
            }

            // Fallback
            return string.Empty;
        }

        public HttpServer(int port, bool debugErrors)
        {
            _debugErrors = debugErrors;
            _port = port;
        }

        public HttpServer AddEndpoint(IHttpEndpoint endpoint)
        {
            if (!_endpoints.TryGetValue(endpoint.Method, out var endpointsForMethod))
            {
                _endpoints[endpoint.Method] = endpointsForMethod = new Dictionary<string, IHttpEndpoint>();
            }

            endpointsForMethod[endpoint.Path] = endpoint;

            return this;
        }

        public HttpServer AddValidator(IHttpValidator validator)
        {
            _validators.Add(validator);

            return this;
        }

        internal static async Task<string> DoGetRequestBodyByContentLengthAsync(Stream inputStream, int contentLength)
        {
            using var ms = new MemoryStream();

            var buf = new byte[BufSize];
            var toRead = contentLength;
            while (toRead > 0)
            {
                var numRead = await inputStream.ReadAsync(buf, 0, Math.Min(BufSize, toRead));
                toRead -= numRead;
                if (numRead == 0)
                {
                    if (toRead == 0)
                    {
                        break;
                    }

                    throw new HttpBodyReadException();
                }

                await ms.WriteAsync(buf, 0, numRead);
            }

            ms.Seek(0, SeekOrigin.Begin);

            return await new StreamReader(ms).ReadToEndAsync();
        }

        internal static async Task<Dictionary<string, string>> DoParseHeadersAsync(Stream inputStream)
        {
            var httpHeaders = new Dictionary<string, string>();
            while (await StreamReadLineAsync(inputStream) is { } line)
            {
                if (line.Equals(""))
                {
                    return httpHeaders;
                }

                var separator = line.IndexOf(':');
                if (separator == -1)
                {
                    throw new Exception("invalid http header line: " + line);
                }

                var name = line.Substring(0, separator);
                var pos = separator + 1;
                while (pos < line.Length && line[pos] == ' ')
                {
                    pos++; // strip any spaces
                }

                var value = line.Substring(pos, line.Length - pos);
                httpHeaders[name] = value;
            }

            return httpHeaders;
        }

        internal static async Task<HttpRequestLine> DoParseRequestAsync(Stream inputStream)
        {
            var request = await StreamReadLineAsync(inputStream);
            var tokens = request.Split(' ');
            if (tokens.Length != 3)
            {
                throw new Exception("invalid http request line");
            }

            var method = tokens[0].ToUpper() switch
            {
                "GET" => HttpMethod.Get,
                "POST" => HttpMethod.Post,
                "DELETE" => HttpMethod.Delete,
                "PUT" => HttpMethod.Put,
                "OPTIONS" => HttpMethod.Options,
                "TRACE" => HttpMethod.Trace,
                "PATCH" => HttpMethod.Patch,
                "HEAD" => HttpMethod.Head,
                _ => throw new ArgumentException(tokens[0])
            };

            return new HttpRequestLine
            {
                Method = method,
                Path = tokens[1],
                VersionString = tokens[2]
            };
        }

        internal static async Task<SimpleHttpRequest> GetHttpRequestMessageAsync(TcpClient client)
        {
            // we can't use a StreamReader for input, because it buffers up extra data on us inside it's
            // "processed" view of the world, and we want the data raw after the headers
            var inputStream = new BufferedStream(client.GetStream());

            var requestData = await DoParseRequestAsync(inputStream);
            var headerData = await DoParseHeadersAsync(inputStream);

            var content = await GetContentAsync(inputStream, headerData);

            var parameters = new Dictionary<string, string>();
            var pathParts = requestData.Path.Split('?');

            if (pathParts.Length >= 2)
            {
                var parsedParameters = HttpUtility.ParseQueryString(pathParts[1]);

                foreach (var key in parsedParameters.AllKeys)
                {
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        continue;
                    }

                    parameters[key] = parsedParameters.Get(key);
                }
            }

            var request = new SimpleHttpRequest
            {
                Path = pathParts[0],
                Method = requestData.Method,
                Body = content,
                Parameters = parameters,
                Headers = headerData
            };

            return request;
        }

        public void HandleRequests()
        {
            while (_messagesToHandle.TryDequeue(out var simpleHttpRequest))
            {
                var endpoint = FindEndpoint(simpleHttpRequest);
                try
                {
                    var response =
                        endpoint?.Handle(simpleHttpRequest.Body, simpleHttpRequest.Parameters,
                            simpleHttpRequest.Headers) ??
                        new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.NotFound,
                            Content = _debugErrors ? new StringContent("Endpoint not found") : null
                        };

                    _responses.Enqueue(new SimpleHttpResponse
                    {
                        RequestId = simpleHttpRequest.RequestId,
                        HttpResponse = response
                    });
                }
                catch (Exception e)
                {
                    _responses.Enqueue(new SimpleHttpResponse
                    {
                        RequestId = simpleHttpRequest.RequestId,
                        HttpResponse = new HttpResponseMessage
                        {
                            StatusCode = HttpStatusCode.InternalServerError,
                            Content = _debugErrors ? new StringContent(e.Message) : null
                        }
                    });
                }

                _responsesEvent.Set();
            }
        }

        internal async Task Listener()
        {
            while (_isActive)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _newClients.Enqueue(client);
                _newClientsEvent.Set();
            }
        }

        internal async Task Parser()
        {
            while (_isActive)
            {
                _newClientsEvent.WaitOne();
                while (_newClients.TryDequeue(out var client))
                {
                    await ParserHandleAsync(client);
                }
            }
        }

        internal async Task ParserHandleAsync(TcpClient client)
        {
            var guid = Guid.NewGuid();

            try
            {
                var simpleHttpRequest = await GetHttpRequestMessageAsync(client);
                simpleHttpRequest.SourceAddress = IPAddress
                    .Parse(((IPEndPoint) client.Client.RemoteEndPoint).Address.ToString()).ToString();
                simpleHttpRequest.RequestId = guid;

                var validatorResponse = ValidatorResponse.Ok;
                foreach (var validator in _validators)
                {
                    validatorResponse = await validator.ValidateAsync(simpleHttpRequest);
                    if (validatorResponse == ValidatorResponse.Ok)
                    {
                        continue;
                    }

                    break; // No need to go further, already broken
                }

                _clients[guid] = client;

                switch (validatorResponse)
                {
                    case ValidatorResponse.Ok:
                        _messagesToHandle.Enqueue(simpleHttpRequest);
                        break;
                    case ValidatorResponse.BadRequest:
                        _responses.Enqueue(new SimpleHttpResponse
                        {
                            RequestId = guid,
                            HttpResponse = new HttpResponseMessage
                            {
                                StatusCode = HttpStatusCode.BadRequest
                            }
                        });
                        _responsesEvent.Set();

                        break;
                    case ValidatorResponse.Forbidden:
                        _responses.Enqueue(new SimpleHttpResponse
                        {
                            RequestId = guid,
                            HttpResponse = new HttpResponseMessage
                            {
                                StatusCode = HttpStatusCode.Forbidden
                            }
                        });
                        _responsesEvent.Set();

                        break;
                    case ValidatorResponse.AuthorizationRequired:
                        _responses.Enqueue(new SimpleHttpResponse
                        {
                            RequestId = guid,
                            HttpResponse = new HttpResponseMessage
                            {
                                StatusCode = HttpStatusCode.Unauthorized
                            }
                        });
                        _responsesEvent.Set();

                        break;
                }
            }
            catch (Exception e)
            {
                _clients[guid] = client;
                _responses.Enqueue(new SimpleHttpResponse
                {
                    RequestId = guid,
                    HttpResponse = new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        Content = _debugErrors ? new StringContent(e.Message) : null
                    }
                });
                _responsesEvent.Set();
            }
        }

        internal async Task Responder()
        {
            while (_isActive)
            {
                _responsesEvent.WaitOne();
                while (_responses.TryDequeue(out var response))
                {
                    if (!_clients.TryGetValue(response.RequestId, out var client))
                    {
                        // Ignore, unknown client
                        continue;
                    }

                    try
                    {
                        await using var outputStream = new BufferedStream(client.GetStream());
                        await SendAsync(outputStream, response.HttpResponse);
                    }
                    catch (Exception)
                    {
                        // Pass
                    }
                    finally
                    {
                        client.Close();
                    }
                }
            }
        }

        internal static async Task SendAsync(Stream outputStream, HttpResponseMessage response)
        {
            // we probably shouldn't be using a StreamWriter for all output from handlers...
            await using var outputWriter = new StreamWriter(outputStream);
            // Response Head
            await outputWriter.WriteLineAsync($"HTTP/1.0 {(int) response.StatusCode} {response.StatusCode}");
            // Headers
            // Standard
            await outputWriter.WriteLineAsync(
                $"Date: {DateTime.UtcNow}"); // TODO: Haven't checked on the expected date format, just throwing DateTime.ToString in
            await outputWriter.WriteLineAsync("Server: Unity BasicHttpServer");

            var content = response.Content is not null ? await response.Content.ReadAsStringAsync() : null;

            foreach (var header in response.Headers)
            {
                await outputWriter.WriteLineAsync($"{header.Key}: {header.Value}");
            }

            if (!string.IsNullOrWhiteSpace(content))
            {
                await outputWriter.WriteLineAsync($"Content-Length: {content.Length}");
                await outputWriter.WriteLineAsync($"Content-Type: {response.Content.Headers.ContentType}");
            }

            // Content demarcation
            await outputWriter.WriteLineAsync("");
            if (!string.IsNullOrWhiteSpace(content))
            {
                await outputWriter.WriteAsync(content);
            }

            await outputWriter.FlushAsync();
        }

        public bool Start()
        {
            try
            {
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();

                // Start threads after successfully binding

                // TODO: Track Tasks better (read: track them at all)
                Task.Run(Listener);
                Task.Run(Parser);
                Task.Run(Responder);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }

            return true;
        }

        public void Stop()
        {
            _isActive = false;
            _listener.Stop();
            _responsesEvent.Set();
            _newClientsEvent.Set();
        }

        public class HttpRequestLine
        {
            public HttpMethod Method { get; set; }
            public string Path { get; set; }
            public string VersionString { get; set; }
        }
    }
}