﻿using System;
using System.IO;

namespace OpenDMS.Networking.Protocols.Http
{
    public class Request 
        : Message.Base
    {
        public RequestLine RequestLine { get; set; }

        public Request(Methods.Base method, Uri uri)
        {
            RequestLine = new RequestLine() { HttpVersion = "HTTP/1.1", Method = method, RequestUri = uri };
        }

        public Request(System.Web.HttpRequest request)
        {
            Headers = new Message.HeaderCollection(request.Headers);
            RequestLine = new RequestLine()
            {
                HttpVersion = "HTTP/1.1",
                Method = Methods.Base.Parse(request.HttpMethod),
                RequestUri = request.Url
            };
            Body = new Message.Body() 
            { 
                IsChunked = false, 
                ReceiveStream = request.InputStream 
            };
        }

        public MemoryStream MakeRequestLineAndHeadersStream()
        {
            Message.HostHeader hostHeader;

            // Currently not supporting sending of chunked content
            if (ContentLength == null)
                throw new Message.HeaderException("Content-Length header is null");

            hostHeader = new Message.HostHeader(this.RequestLine.RequestUri.Host);
            Headers[hostHeader.Name] = hostHeader.Value;

            return new MemoryStream(System.Text.Encoding.ASCII.GetBytes(Headers.ToString()));
        }
    }
}