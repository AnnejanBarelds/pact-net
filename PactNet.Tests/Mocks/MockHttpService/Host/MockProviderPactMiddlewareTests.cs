using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NSubstitute;
using PactNet.Logging;
using PactNet.Mocks.MockHttpService.Host;
using Xunit;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace PactNet.Tests.Mocks.MockHttpService.Host
{
    public class MockProviderPactMiddlewareTests
    {
        private IMockProviderRequestHandler _mockRequestHandler;
        private IMockProviderAdminRequestHandler _mockAdminRequestHandler;
        private ILog _log;

        private PactMiddleware GetSubject()
        {
            _mockRequestHandler = Substitute.For<IMockProviderRequestHandler>();
            _mockAdminRequestHandler = Substitute.For<IMockProviderAdminRequestHandler>();
            _log = Substitute.For<ILog>();

            return new PactMiddleware(null, _mockRequestHandler, _mockAdminRequestHandler, _log, new PactConfig());
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
            if (headers != null)
            {
                headers.ToList().ForEach(header => context.Request.Headers.Add(header.Key, new Microsoft.Extensions.Primitives.StringValues(header.Value.ToArray())));
            }

            context.Response.Body = new MemoryStream();
            return context;
        }

        [Fact]
        public void Invoke_WithHttpContext_CallsRequestHandlerWithContext()
        {
            var context = GetRequestContext("GET", "/Test", "HTTP");

            var middleware = GetSubject();

            middleware.Invoke(context);

            _mockRequestHandler.Received(1).Handle(context);
        }

        [Fact]
        public void Invoke_WithHttpContextThatContainsAdminHeader_CallsAdminRequestHandlerWithContext()
        {
            var headers = new Dictionary<string, IEnumerable<string>>
            {
                { Constants.AdministrativeRequestHeaderKey, new List<string> { "true" } }
            };

            var context = GetRequestContext("GET", "/Test", "HTTP", null, null, headers);

            var middleware = GetSubject();

            middleware.Invoke(context);

            _mockAdminRequestHandler.Received(1).Handle(context);
        }

        [Fact]
        public void Invoke_WithNullHttpContext_ArgumentExceptionIsThrown()
        {
            var middleware = GetSubject();

            Assert.ThrowsAsync<ArgumentException>(() => middleware.Invoke(null));
        }

        [Fact]
        public void Invoke_WithHttpContext_SetsContextResponse()
        {
            var context = GetRequestContext("GET", "/Test", "HTTP");
            context.Response.StatusCode = StatusCodes.Status418ImATeapot;

            var middleware = GetSubject();

            _mockRequestHandler.When(x => x.Handle(context)).Do(x => x.Arg<HttpContext>().Response.StatusCode = StatusCodes.Status200OK);

            middleware.Invoke(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public void Invoke_WithHttpContext_NoExceptionIsSetOnTask()
        {
            var context = GetRequestContext("GET", "/Test", "HTTP");
            context.Response.StatusCode = StatusCodes.Status418ImATeapot;

            var middleware = GetSubject();

            _mockRequestHandler.When(x => x.Handle(context)).Do(x => x.Arg<HttpContext>().Response.StatusCode = StatusCodes.Status200OK);

            var response = middleware.Invoke(context);

            Assert.Null(response.Exception);
        }

        [Fact]
        public void Invoke_WhenRequestHandlerThrows_InternalServerErrorResponseIsReturned()
        {
            var exception = new InvalidOperationException("Something failed.");
            const string expectedMessage = "Something failed. See logs for details.";
            var context = GetRequestContext("GET", "/Test", "HTTP");
            context.Response.StatusCode = StatusCodes.Status418ImATeapot;

            var middleware = GetSubject();

            _mockRequestHandler
                .When(x => x.Handle(Arg.Any<HttpContext>()))
                .Do(x => { throw exception; });

            middleware.Invoke(context);

            Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
            Assert.Equal(expectedMessage, context.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase);
            Assert.Equal(expectedMessage, ReadResponseContent(context.Response));
        }

        [Fact]
        public void Invoke_WhenRequestHandlerThrowsWithMessageThatContainsSlashes_ResponseContentAndReasonPhrasesIsReturnedWithoutSlashes()
        {
            var exception = new InvalidOperationException("Something\r\n \t \\ failed.");
            const string expectedMessage = @"Something\r\n \t \\ failed. See logs for details.";
            var context = GetRequestContext("GET", "/Test", "HTTP");
            context.Response.StatusCode = StatusCodes.Status418ImATeapot;

            var middleware = GetSubject();

            _mockRequestHandler
                .When(x => x.Handle(Arg.Any<HttpContext>()))
                .Do(x => { throw exception; });

            middleware.Invoke(context);

            Assert.Equal(expectedMessage, context.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase);
            Assert.Equal(expectedMessage, ReadResponseContent(context.Response));
        }

        [Fact]
        public void Invoke_WhenRequestHandlerThrows_TheExceptionIsLogged()
        {
            var exception = new InvalidOperationException("Something failed.");
            var context = GetRequestContext("GET", "/Test", "HTTP");
            context.Response.StatusCode = StatusCodes.Status418ImATeapot;

            var middleware = GetSubject();

            _mockRequestHandler
                .When(x => x.Handle(Arg.Any<HttpContext>()))
                .Do(x => { throw exception; });

            middleware.Invoke(context);

            _log.Received(1).ErrorException(Arg.Any<string>(), exception);
        }

        [Fact]
        public void Invoke_WhenRequestHandlerThrowsAPactFailureException_TheExceptionIsNotLogged()
        {
            var exception = new PactFailureException("Something failed");
            var context = GetRequestContext("GET", "/Test", "HTTP");
            context.Response.StatusCode = StatusCodes.Status418ImATeapot;

            var middleware = GetSubject();

            _mockRequestHandler
                .When(x => x.Handle(Arg.Any<HttpContext>()))
                .Do(x => { throw exception; });

            middleware.Invoke(context);

            _log.DidNotReceive().ErrorException(Arg.Any<string>(), Arg.Any<Exception>());
        }

        private string ReadResponseContent(HttpResponse response)
        {
            string content;
            using (var reader = new StreamReader(response.Body))
            {
                response.Body.Position = 0;
                content = reader.ReadToEnd();
            }

            return content;
        }
    }
}
