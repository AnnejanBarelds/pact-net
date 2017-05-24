using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Text;
using NSubstitute;
using PactNet.Mocks.MockHttpService.Mappers;
using PactNet.Mocks.MockHttpService.Models;
using Xunit;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Linq;

namespace PactNet.Tests.Mocks.MockHttpService.Mappers
{
    public class ProviderServiceRequestMapperTests
    {
        private IProviderServiceRequestMapper GetSubject()
        {
            return new ProviderServiceRequestMapper();
        }

        private HttpContext GetRequestContext(string method, string path, string protocol, string host = null, Stream body = null, IDictionary<string, IEnumerable<string>> headers = null)//, string ip = null, X509Certificate certificate = null, string protocolVersion = null)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = method;
            if (!String.IsNullOrEmpty(host))
            {
                context.Request.Host = new HostString(host);
            }
            context.Request.Path = path;
            context.Request.Protocol = protocol;
            context.Request.Body = body;
            if (body != null)
            {
                context.Request.ContentLength = body.Length;
            }
            if (headers != null)
            {
                headers.ToList().ForEach(header => context.Request.Headers.Add(header.Key, new Microsoft.Extensions.Primitives.StringValues(header.Value.ToArray())));
            }

            context.Response.Body = new MemoryStream();
            
            return context;
        }

        [Fact]
        public void Convert_WithNullRequest_ReturnsNull()
        {
            var mapper = GetSubject();

            var result = mapper.Convert(null);

            Assert.Null(result);
        }

        [Fact]
        public void Convert_WithMethod_CallsHttpVerbMapperAndSetsHttpMethod()
        {
            const HttpVerb httpVerb = HttpVerb.Get;
            
            var context = GetRequestContext("GET", "/events", "Http");

            var mockHttpVerbMapper = Substitute.For<IHttpVerbMapper>();
            var mockHttpBodyContentMapper = Substitute.For<IHttpBodyContentMapper>();
            mockHttpVerbMapper.Convert("GET").Returns(httpVerb);

            var mapper = new ProviderServiceRequestMapper(mockHttpVerbMapper, mockHttpBodyContentMapper);

            var result = mapper.Convert(context.Request);

            Assert.Equal(httpVerb, result.Method);
            mockHttpVerbMapper.Received(1).Convert("GET");
        }

        [Fact]
        public void Convert_WithPath_CorrectlySetsPath()
        {
            const string path = "/events";
            const HttpVerb httpVerb = HttpVerb.Get;
            var context = GetRequestContext("GET", path, "Http");

            var mockHttpVerbMapper = Substitute.For<IHttpVerbMapper>();
            var mockHttpBodyContentMapper = Substitute.For<IHttpBodyContentMapper>();
            mockHttpVerbMapper.Convert("GET").Returns(httpVerb);

            var mapper = new ProviderServiceRequestMapper(mockHttpVerbMapper, mockHttpBodyContentMapper);

            var result = mapper.Convert(context.Request);

            Assert.Equal(path, result.Path);
        }

        [Fact]
        public void Convert_WithPathAndEmptyQuery_QueryIsSetToNull()
        {
            const string path = "/events";

            const HttpVerb httpVerb = HttpVerb.Get;
            var context = GetRequestContext("GET", path, "Http");

            var mockHttpVerbMapper = Substitute.For<IHttpVerbMapper>();
            var mockHttpBodyContentMapper = Substitute.For<IHttpBodyContentMapper>();
            mockHttpVerbMapper.Convert("GET").Returns(httpVerb);

            var mapper = new ProviderServiceRequestMapper(mockHttpVerbMapper, mockHttpBodyContentMapper);

            var result = mapper.Convert(context.Request);

            Assert.Equal(string.Empty, result.Query);
        }

        [Fact]
        public void Convert_WithPathAndQuery_CorrectlySetsPathAndQuery()
        {
            const string path = "/events";
            const string query = "test=2&test2=hello";
            const HttpVerb httpVerb = HttpVerb.Get;
            var request = GetPreCannedRequest();

            request.QueryString = new QueryString("?" + query);

            var mockHttpVerbMapper = Substitute.For<IHttpVerbMapper>();
            var mockHttpBodyContentMapper = Substitute.For<IHttpBodyContentMapper>();
            mockHttpVerbMapper.Convert("GET").Returns(httpVerb);

            var mapper = new ProviderServiceRequestMapper(mockHttpVerbMapper, mockHttpBodyContentMapper);

            var result = mapper.Convert(request);

            Assert.Equal(path, result.Path);
            Assert.Equal(query, result.Query);
        }

        [Fact]
        public void Convert_WithHeaders_CorrectlySetsHeaders()
        {
            var contentType = "text/plain";
            var contentEncoding = "charset=utf-8";

            var customHeaderValue = "Custom Header Value";

            var headers = new Dictionary<string, IEnumerable<string>>
            {
                { "Content-Type", new List<string> { contentType, contentEncoding } },
                { "X-Custom", new List<string> { customHeaderValue } }
            };
            var request = GetPreCannedRequest(headers);

            var mockHttpVerbMapper = Substitute.For<IHttpVerbMapper>();
            var mockHttpBodyContentMapper = Substitute.For<IHttpBodyContentMapper>();
            mockHttpVerbMapper.Convert("GET").Returns(HttpVerb.Get);

            var mapper = new ProviderServiceRequestMapper(mockHttpVerbMapper, mockHttpBodyContentMapper);

            var result = mapper.Convert(request);

            Assert.Equal(contentType + ", " + contentEncoding, result.Headers["Content-Type"]);
            Assert.Equal(customHeaderValue, result.Headers["X-Custom"]);
        }

        [Fact]
        public void Convert_WithPlainTextBody_CallsHttpBodyContentMapperAndCorrectlySetsBody()
        {
            const string content = "Plain text body";
            var request = GetPreCannedRequest(content: content);
            var httpBodyContent = new HttpBodyContent(new DynamicBody { Body = content, ContentType = new MediaTypeHeaderValue("text/plain") { CharSet = "utf-8" } });

            var mockHttpVerbMapper = Substitute.For<IHttpVerbMapper>();
            var mockHttpBodyContentMapper = Substitute.For<IHttpBodyContentMapper>();
            mockHttpVerbMapper.Convert("GET").Returns(HttpVerb.Get);
            mockHttpBodyContentMapper.Convert(Arg.Any<BinaryContentMapRequest>()).Returns(httpBodyContent);

            var mapper = new ProviderServiceRequestMapper(mockHttpVerbMapper, mockHttpBodyContentMapper);

            var result = mapper.Convert(request);

            Assert.Equal(content, result.Body);
            mockHttpBodyContentMapper.Received(1).Convert(Arg.Any<BinaryContentMapRequest>());
        }

        [Fact]
        public void Convert_WithJsonBody_CallsHttpBodyContentMapperAndCorrectlySetsBody()
        {
            var headers = new Dictionary<string, IEnumerable<string>>
            {
                { "Content-Type", new List<string> { "application/json", "charset=utf-8" } }
            };
            var body = new
            {
                Test = "tester",
                test2 = 1
            };
            const string content = "{\"Test\":\"tester\",\"test2\":1}";
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var request = GetPreCannedRequest(headers: headers, content: content);
            var httpBodyContent = new HttpBodyContent(new BinaryContent { Content = contentBytes, ContentType = new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" } });

            var mockHttpVerbMapper = Substitute.For<IHttpVerbMapper>();
            var mockHttpBodyContentMapper = Substitute.For<IHttpBodyContentMapper>();
            mockHttpVerbMapper.Convert("GET").Returns(HttpVerb.Get);
            mockHttpBodyContentMapper.Convert(Arg.Any<BinaryContentMapRequest>()).Returns(httpBodyContent);

            var mapper = new ProviderServiceRequestMapper(mockHttpVerbMapper, mockHttpBodyContentMapper);

            var result = mapper.Convert(request);

            Assert.Equal(body.Test, (string)result.Body.Test);
            Assert.Equal(body.test2, (int)result.Body.test2);
            mockHttpBodyContentMapper.Received(1).Convert(Arg.Any<BinaryContentMapRequest>());
        }

        private HttpRequest GetPreCannedRequest(IDictionary<string, IEnumerable<string>> headers = null, string content = null)
        {
            MemoryStream stream = null;

            if (!String.IsNullOrEmpty(content))
            {
                var contentBytes = Encoding.UTF8.GetBytes(content);
                stream = new MemoryStream(contentBytes);
            }

            var context = GetRequestContext("Get", "/events", "http", "localhost:1234", stream, headers);

            return context.Request;
        }
    }
}