using System;
using NSubstitute;
using PactNet.Logging;
using PactNet.Mocks.MockHttpService;
using PactNet.Mocks.MockHttpService.Mappers;
using PactNet.Mocks.MockHttpService.Models;
using PactNet.Mocks.MockHttpService.Host;
using Xunit;
using Microsoft.AspNetCore.Http;

namespace PactNet.Tests.Mocks.MockHttpService.Host
{
    public class MockProviderRequestHandlerTests
    {
        private IProviderServiceRequestMapper _mockRequestMapper;
        private IHttpResponseMapper _mockResponseMapper;
        private IMockProviderRepository _mockProviderRepository;
        private ILog _mockLog;

        private IMockProviderRequestHandler GetSubject()
        {
            _mockRequestMapper = Substitute.For<IProviderServiceRequestMapper>();
            _mockResponseMapper = Substitute.For<IHttpResponseMapper>();
            _mockProviderRepository = Substitute.For<IMockProviderRepository>();
            _mockLog = Substitute.For<ILog>();

            _mockLog.Log(Arg.Any<LogLevel>(), Arg.Any<Func<string>>(), Arg.Any<Exception>(), Arg.Any<object[]>())
                .Returns(true);

            return new MockProviderRequestHandler(_mockRequestMapper, _mockResponseMapper, _mockProviderRepository, _mockLog);
        }

        private HttpContext GetRequestContext(string method, string path, string protocol)
        {
            var context = new DefaultHttpContext();
            context.Request.Method = method;
            context.Request.Path = path;
            context.Request.Protocol = protocol;

            return context;
        }

        private HttpResponse GetResponse(int statuscode)
        {
            var context = new DefaultHttpContext();
            context.Response.StatusCode = statuscode;

            return context.Response;
        }

        [Fact]
        public void Handle_WithHttpContext_ConvertIsCalledOnThProviderServiceRequestMapper()
        {
            var expectedRequest = new ProviderServiceRequest
            {
                Method = HttpVerb.Get,
                Path = "/"
            };
            var expectedResponse = new ProviderServiceResponse();
            var context = GetRequestContext("GET", "/", "HTTP");
            var handler = GetSubject();

            _mockRequestMapper.Convert(context.Request).Returns(expectedRequest);

            var interaction = new ProviderServiceInteraction { Request = expectedRequest, Response = expectedResponse };

            _mockProviderRepository.GetMatchingTestScopedInteraction(expectedRequest)
                .Returns(interaction);

            handler.Handle(context);

            _mockRequestMapper.Received(1).Convert(context.Request);
        }

        [Fact]
        public void Handle_WithHttpContext_AddHandledRequestIsCalledOnTheMockProviderRepository()
        {
            var expectedRequest = new ProviderServiceRequest
            {
                Method = HttpVerb.Get,
                Path = "/"
            };
            var actualRequest = new ProviderServiceRequest
            {
                Method = HttpVerb.Get,
                Path = "/",
                Body = new { }
            };
            var expectedResponse = new ProviderServiceResponse();

            var handler = GetSubject();

            var context = GetRequestContext("GET", "/", "HTTP");

            var interaction = new ProviderServiceInteraction { Request = expectedRequest, Response = expectedResponse };

            _mockProviderRepository.GetMatchingTestScopedInteraction(Arg.Any<ProviderServiceRequest>())
                .Returns(interaction);

            _mockRequestMapper.Convert(context.Request).Returns(actualRequest);

            handler.Handle(context);

            _mockProviderRepository.Received(1).AddHandledRequest(Arg.Is<HandledRequest>(x => x.ActualRequest == actualRequest && x.MatchedInteraction == interaction));
        }

        [Fact]
        public void Handle_WithHttpContext_ConvertIsCalledOnTheHttpResponseMapper()
        {
            var expectedRequest = new ProviderServiceRequest
            {
                Method = HttpVerb.Get,
                Path = "/"
            };
            var expectedResponse = new ProviderServiceResponse();
            var context = GetRequestContext("GET", "/", "HTTP");

            var handler = GetSubject();

            _mockRequestMapper.Convert(context.Request).Returns(expectedRequest);

            var interaction = new ProviderServiceInteraction { Request = expectedRequest, Response = expectedResponse };

            _mockProviderRepository.GetMatchingTestScopedInteraction(expectedRequest)
                .Returns(interaction);

            handler.Handle(context);

            _mockResponseMapper.Received(1).Convert(context, expectedResponse);
        }

        [Fact]
        public void Handle_WithhHttpContextRequestThatMatchesExpectedRequest_SetsHttpResponse()
        {
            var expectedRequest = new ProviderServiceRequest
            {
                Method = HttpVerb.Get,
                Path = "/Test"
            };
            var actualRequest = new ProviderServiceRequest
            {
                Method = HttpVerb.Get,
                Path = "/Test"
            };
            var expectedResponse = new ProviderServiceResponse { Status = 200 };

            var handler = GetSubject();

            var context = GetRequestContext("GET", "/Test", "HTTP");

            var interaction = new ProviderServiceInteraction { Request = expectedRequest, Response = expectedResponse };

            _mockProviderRepository.GetMatchingTestScopedInteraction(Arg.Any<ProviderServiceRequest>())
                .Returns(interaction);

            _mockRequestMapper.Convert(context.Request).Returns(actualRequest);

            _mockResponseMapper.When(x => x.Convert(context, expectedResponse)).Do(x => x.Arg<HttpContext>().Response.StatusCode = StatusCodes.Status200OK);

            handler.Handle(context);

            Assert.Equal(200, context.Response.StatusCode);
        }

        [Fact]
        public void Handle_WhenExceptionIsThrownHandlingRequest_PactFailureExceptionIsThrown()
        {
            var compareException = new PactFailureException("Something\r\n \t \\ failed");

            var context = GetRequestContext("GET", "/Test", "HTTP");

            var handler = GetSubject();

            _mockRequestMapper
                .When(x => x.Convert(Arg.Any<HttpRequest>()))
                .Do(x => { throw compareException; });

            _mockResponseMapper
                .When(x => x.Convert(Arg.Any<HttpContext>(), Arg.Any<ProviderServiceResponse>()))
                .Do(x => x.Arg<HttpContext>().Response.StatusCode = StatusCodes.Status500InternalServerError);

            Assert.ThrowsAsync<PactFailureException>(() => handler.Handle(context));
        }

        [Fact]
        public void Handle_WhenNoMatchingInteractionsAreFound_RequestIsMarkedAsHandled()
        {
            const string exceptionMessage = "No matching mock interaction has been registered for the current request";
            var expectedRequest = new ProviderServiceRequest
            {
                Method = HttpVerb.Get,
                Path = "/Test"
            };
            var context = GetRequestContext("GET", "/Test", "HTTP");

            var handler = GetSubject();

            _mockRequestMapper
                .Convert(context.Request)
                .Returns(expectedRequest);

            _mockProviderRepository
                .When(x => x.GetMatchingTestScopedInteraction(expectedRequest))
                .Do(x => { throw new PactFailureException(exceptionMessage); });

            try
            {
                handler.Handle(context);
            }
            catch (Exception)
            {
            }

            _mockProviderRepository.Received(1).AddHandledRequest(Arg.Is<HandledRequest>(x => x.ActualRequest == expectedRequest && x.MatchedInteraction == null));
        }

        [Fact]
        public void Handle_WhenNoMatchingInteractionsAreFound_ErrorIsLogged()
        {
            const string exceptionMessage = "No matching mock interaction has been registered for the current request";
            var expectedRequest = new ProviderServiceRequest
            {
                Method = HttpVerb.Get,
                Path = "/Test"
            };
            var context = GetRequestContext("GET", "/Test", "HTTP");

            var handler = GetSubject();

            _mockRequestMapper
                .Convert(context.Request)
                .Returns(expectedRequest);

            _mockProviderRepository
                .When(x => x.GetMatchingTestScopedInteraction(expectedRequest))
                .Do(x => { throw new PactFailureException(exceptionMessage); });

            try
            {
                handler.Handle(context);
            }
            catch (Exception)
            {
            }

            _mockLog.Received().Log(LogLevel.Error, Arg.Any<Func<string>>(), null, Arg.Any<object[]>());
        }

        [Fact]
        public void Handle_WhenNoMatchingInteractionsAreFound_PactFailureExceptionIsThrown()
        {
            const string exceptionMessage = "No matching mock interaction has been registered for the current request";
            var expectedRequest = new ProviderServiceRequest
            {
                Method = HttpVerb.Get,
                Path = "/Test"
            };
            var context = GetRequestContext("GET", "/Test", "HTTP");

            var handler = GetSubject();

            _mockRequestMapper
                .Convert(context.Request)
                .Returns(expectedRequest);

            _mockProviderRepository
                .When(x => x.GetMatchingTestScopedInteraction(expectedRequest))
                .Do(x => { throw new PactFailureException(exceptionMessage); });

            Assert.ThrowsAsync<PactFailureException>(() => handler.Handle(context));
        }
    }
}
