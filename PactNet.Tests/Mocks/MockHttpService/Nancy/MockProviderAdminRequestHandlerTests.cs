using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using NSubstitute;
using Newtonsoft.Json;
using PactNet.Configuration.Json;
using PactNet.Logging;
using PactNet.Mocks.MockHttpService;
using PactNet.Mocks.MockHttpService.Models;
using PactNet.Mocks.MockHttpService.Nancy;
using PactNet.Models;
using Xunit;
using Thinktecture.IO;
using Microsoft.AspNetCore.Http;

namespace PactNet.Tests.Mocks.MockHttpService.Nancy
{
    public class MockProviderAdminRequestHandlerTests
    {
        private IMockProviderRepository _mockProviderRepository;
        private IFile _mockFileAdapter;
        private ILog _mockLog;

        private IMockProviderAdminRequestHandler GetSubject(PactConfig pactConfig = null)
        {
            _mockProviderRepository = Substitute.For<IMockProviderRepository>();
            _mockFileAdapter = Substitute.For<IFile>();
            _mockLog = Substitute.For<ILog>();

            _mockLog.Log(Arg.Any<LogLevel>(), Arg.Any<Func<string>>(), Arg.Any<Exception>(), Arg.Any<object[]>())
                .Returns(true);

            return new MockProviderAdminRequestHandler(
                _mockProviderRepository,
                _mockFileAdapter,
                pactConfig ?? new PactConfig(),
                _mockLog);
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
        public void Handle_WithTheTestContextHeaderAttached_LogsTheTestContext()
        {
            const string testContext = "EventsApiConsumerTests.GetAllEvents_WhenCalled_ReturnsAllEvents";
            var headers = new Dictionary<string, IEnumerable<string>>
            {
                { Constants.AdministrativeRequestTestContextHeaderKey, new List<string> { testContext } }
            };

            var context = GetRequestContext("DELETE", "/interactions", "http", null, null, headers);

            var handler = GetSubject();

            handler.Handle(context);

            _mockLog.Received(1).Log(LogLevel.Info, Arg.Any<Func<string>>(), null, Arg.Is<object[]>(x => x.Single().ToString() == testContext));
        }

        [Fact]
        public void Handle_WithTheTestContextHeaderAttached_SetsTestContextOnTheRepository()
        {
            const string testContext = "EventsApiConsumerTests.GetAllEvents_WhenCalled_ReturnsAllEvents";
            var headers = new Dictionary<string, IEnumerable<string>>
            {
                { Constants.AdministrativeRequestTestContextHeaderKey, new List<string> { testContext } }
            };

            var context = GetRequestContext("DELETE", "/interactions", "http", null, null, headers);

            var handler = GetSubject();

            handler.Handle(context);

            _mockProviderRepository.Received(1).TestContext = testContext;
        }

        [Fact]
        public void Handle_WhenTestContextIsSetOnTheRepository_DoesNotLogTheTextContext()
        {
            const string testContext = "EventsApiConsumerTests.GetAllEvents_WhenCalled_ReturnsAllEvents";
            var headers = new Dictionary<string, IEnumerable<string>>
            {
                { Constants.AdministrativeRequestTestContextHeaderKey, new List<string> { testContext } }
            };

            var context = GetRequestContext("DELETE", "/interactions", "http", null, null, headers);

            var handler = GetSubject();

            _mockProviderRepository.TestContext.Returns(testContext);

            handler.Handle(context);

            _mockLog.Received(0).Log(LogLevel.Info, Arg.Any<Func<string>>(), null, Arg.Is<object[]>(x => x.Single().ToString() == testContext));
        }

        [Fact]
        public void Handle_WithADeleteRequestToInteractions_ClearHandledRequestsIsCalledOnTheMockProviderRepository()
        {
            var context = GetRequestContext("DELETE", "/interactions", "http");

            var handler = GetSubject();

            handler.Handle(context);

            _mockProviderRepository.Received(1).ClearTestScopedState();
        }

        [Fact]
        public void Handle_WithADeleteRequestToInteractions_ClearTestScopedInteractionsIsCalledOnTheMockProviderRepository()
        {
            var context = GetRequestContext("DELETE", "/interactions", "http");

            var handler = GetSubject();

            handler.Handle(context);

            _mockProviderRepository.Received(1).ClearTestScopedState();
        }

        [Fact]
        public void Handle_WithADeleteRequestToInteractions_ReturnsOkResponse()
        {
            var context = GetRequestContext("DELETE", "/interactions", "http");

            var handler = GetSubject();

            handler.Handle(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public void Handle_WithALowercasedDeleteRequestToInteractions_ReturnsOkResponse()
        {
            var context = GetRequestContext("delete", "/interactions", "http");

            var handler = GetSubject();

            var response = handler.Handle(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public void Handle_WithAPostRequestToInteractions_AddInteractionIsCalledOnTheMockProviderRepository()
        {
            var interaction = new ProviderServiceInteraction
            {
                Description = "My description",
                Request = new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/test"
                },
                Response = new ProviderServiceResponse
                {
                    Status = StatusCodes.Status204NoContent
                }
            };
            var interactionJson = interaction.AsJsonString();

            var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(interactionJson));

            //var requestStream = new RequestStream(jsonStream, jsonStream.Length, true);
            var context = GetRequestContext("POST", "/interactions", "http", "localhost", jsonStream);

            var handler = GetSubject();

            handler.Handle(context);

            _mockProviderRepository.Received(1).AddInteraction(Arg.Is<ProviderServiceInteraction>(x => x.AsJsonString() == interactionJson));
        }

        [Fact]
        public void Handle_WithAPostRequestToInteractions_ReturnsOkResponse()
        {
            var interaction = new ProviderServiceInteraction
            {
                Description = "My description",
                Request = new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/test"
                },
                Response = new ProviderServiceResponse
                {
                    Status = StatusCodes.Status204NoContent
                }
            };
            var interactionJson = interaction.AsJsonString();

            var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(interactionJson));

            //var requestStream = new RequestStream(jsonStream, jsonStream.Length, true);
            var context = GetRequestContext("POST", "/interactions", "http", "localhost", jsonStream);

            var handler = GetSubject();

            handler.Handle(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public void Handle_WithALowercasedPostRequestToInteractions_ReturnsOkResponse()
        {
            var interaction = new ProviderServiceInteraction
            {
                Description = "My description",
                Request = new ProviderServiceRequest
                {
                    Method = HttpVerb.Get,
                    Path = "/test"
                },
                Response = new ProviderServiceResponse
                {
                    Status = StatusCodes.Status204NoContent
                }
            };
            var interactionJson = interaction.AsJsonString();

            var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(interactionJson));

            //var requestStream = new RequestStream(jsonStream, jsonStream.Length, true);
            var context = GetRequestContext("post", "/interactions", "http", "localhost", jsonStream);

            var handler = GetSubject();

            handler.Handle(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public void Handle_WithAGetRequestToInteractionsVerification_ReturnsOkResponse()
        {
            var context = GetRequestContext("GET", "/interactions/verification", "http");

            var handler = GetSubject();

            var response = handler.Handle(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public void Handle_WithALowercasedGetRequestToInteractionsVerification_ReturnsOkResponse()
        {
            var context = GetRequestContext("get", "/interactions/verification", "http");

            var handler = GetSubject();

            var response = handler.Handle(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public void Handle_WithAGetRequestToInteractionsVerificationAndRegisteredInteractionWasCalledExactlyOnce_ReturnsOkResponse()
        {
            var context = GetRequestContext("GET", "/interactions/verification", "http");

            var interactions = new List<ProviderServiceInteraction>
            {
                new ProviderServiceInteraction()
            };

            var handler = GetSubject();

            _mockProviderRepository.TestScopedInteractions.Returns(interactions);

            _mockProviderRepository.HandledRequests.Returns(new List<HandledRequest>
            {
                new HandledRequest(new ProviderServiceRequest(), interactions.First())
            });

            var response = handler.Handle(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public void Handle_WithAGetRequestToInteractionsVerificationAndRegisteredInteractionWasNotCalled_ThrowsPactFailureExceptionAndLogsTheMissingRequest()
        {
            var context = GetRequestContext("GET", "/interactions/verification", "http");

            var handler = GetSubject();


            _mockProviderRepository.TestScopedInteractions.Returns(new List<ProviderServiceInteraction> { new ProviderServiceInteraction() });

            Assert.ThrowsAsync<PactFailureException>(() => handler.Handle(context));

            _mockLog.Received().Log(LogLevel.Error, Arg.Any<Func<string>>(), null, Arg.Any<object[]>());
        }

        [Fact]
        public void Handle_WithAGetRequestToInteractionsVerificationAndRegisteredInteractionWasCalledMultipleTimes_ThrowsPactFailureExceptionAndLogsTheError()
        {
            var context = GetRequestContext("GET", "/interactions/verification", "http");

            var interactions = new List<ProviderServiceInteraction>
            {
                new ProviderServiceInteraction()
            };

            var handler = GetSubject();

            _mockProviderRepository.TestScopedInteractions.Returns(interactions);

            _mockProviderRepository.HandledRequests.Returns(new List<HandledRequest>
            {
                new HandledRequest(new ProviderServiceRequest(), interactions.First()),
                new HandledRequest(new ProviderServiceRequest(), interactions.First())
            });

            Assert.ThrowsAsync<PactFailureException>(() => handler.Handle(context));

            _mockLog.Received().Log(LogLevel.Error, Arg.Any<Func<string>>(), null, Arg.Any<object[]>());
        }

        [Fact]
        public void Handle_WithAGetRequestToInteractionsVerificationAndNoInteractionsRegisteredHoweverMockProviderRecievedInteractions_ThrowsPactFailureException()
        {
            var context = GetRequestContext("GET", "/interactions/verification", "http");

            var handler = GetSubject();

            _mockProviderRepository.HandledRequests.Returns(new List<HandledRequest>
            {
                new HandledRequest(new ProviderServiceRequest(), new ProviderServiceInteraction())
            });

            Assert.ThrowsAsync<PactFailureException>(() => handler.Handle(context));
        }

        [Fact]
        public void Handle_WithAGetRequestToInteractionsVerificationAndCorrectlyMatchedHandledRequest_ReturnsOkResponse()
        {
            var context = GetRequestContext("GET", "/interactions/verification", "http");
            var interaction = new ProviderServiceInteraction();

            var handler = GetSubject();


            _mockProviderRepository.TestScopedInteractions.Returns(new List<ProviderServiceInteraction>
            {
                interaction
            });

            _mockProviderRepository.HandledRequests.Returns(new List<HandledRequest>
            {
                new HandledRequest(new ProviderServiceRequest(), interaction)
            });

            var response = handler.Handle(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public void Handle_WithAGetRequestToInteractionsVerificationAndAnIncorrectlyMatchedHandledRequest_ThrowsPactFailureException()
        {
            var context = GetRequestContext("GET", "/interactions/verification", "http");

            var handler = GetSubject();

            _mockProviderRepository.HandledRequests.Returns(new List<HandledRequest>
            {
                new HandledRequest(new ProviderServiceRequest(), new ProviderServiceInteraction())
            });

            Assert.ThrowsAsync<PactFailureException>(() => handler.Handle(context));
        }

        [Fact]
        public void Handle_WithAGetRequestToInteractionsVerificationAndAnInteractionWasSentButNotRegisteredByTheTest_ThrowsPactFailureExceptionWithTheCorrectMessageAndLogsTheUnexpectedRequest()
        {
            const string failure = "An unexpected request POST /tester was seen by the mock provider service.";
            var context = GetRequestContext("GET", "/interactions/verification", "http");

            var handler = GetSubject();

            var handledRequest = new ProviderServiceRequest();
            var handledInteraction = new ProviderServiceInteraction { Request = handledRequest };

            var unExpectedRequest = new ProviderServiceRequest { Method = HttpVerb.Post, Path = "/tester" };

            _mockProviderRepository.TestScopedInteractions
                .Returns(new List<ProviderServiceInteraction>
                {
                    handledInteraction
                });

            _mockProviderRepository.HandledRequests
                .Returns(new List<HandledRequest>
                {
                    new HandledRequest(handledRequest, handledInteraction),
                    new HandledRequest(unExpectedRequest, null)
                });

            var exception = Assert.ThrowsAsync<PactFailureException>(() => handler.Handle(context)).Result;

            _mockLog.Received().Log(LogLevel.Error, Arg.Any<Func<string>>(), null, Arg.Any<object[]>());
            Assert.Equal(failure, exception.Message);
        }

        [Fact]
        public void Handle_WithAGetRequestToInteractionsVerificationAndAFailureOcurrs_ThrowsPactFailureExceptionWithTheCorrectMessage()
        {
            const string failure = "The interaction with description '' and provider state '', was not used by the test. Missing request No Method No Path.";
            var context = GetRequestContext("GET", "/interactions/verification", "http");

            var handler = GetSubject();

            _mockProviderRepository.TestScopedInteractions.Returns(new List<ProviderServiceInteraction> { new ProviderServiceInteraction() });

            var expection = Assert.ThrowsAsync<PactFailureException>(() => handler.Handle(context)).Result;

            Assert.Equal(failure, expection.Message);
        }

        [Fact]
        public void Handle_WithAPostRequestToPactAndNoInteractionsHaveBeenRegistered_NewPactFileIsSavedWithNoInteractions()
        {
            var pactDetails = new PactDetails
            {
                Consumer = new Pacticipant { Name = "Consumer" },
                Provider = new Pacticipant { Name = "Provider" }
            };

            var pactFile = new ProviderServicePactFile
            {
                Provider = pactDetails.Provider,
                Consumer = pactDetails.Consumer,
                Interactions = new ProviderServiceInteraction[0]
            };

            var pactFileJson = JsonConvert.SerializeObject(pactFile, JsonConfig.PactFileSerializerSettings);
            var pactDetailsJson = JsonConvert.SerializeObject(pactDetails, JsonConfig.ApiSerializerSettings);

            var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(pactDetailsJson));

            var context = GetRequestContext("POST", "/pact", "http", "localhost", jsonStream);

            var handler = GetSubject();

            handler.Handle(context);

            _mockFileAdapter.Received(1).WriteAllText(Path.Combine(Constants.DefaultPactDir, pactDetails.GeneratePactFileName()), pactFileJson);
        }

        [Fact]
        public void Handle_WithAPostRequestToPactAndInteractionsHaveBeenRegistered_NewPactFileIsSavedWithInteractions()
        {
            var pactDetails = new PactDetails
            {
                Consumer = new Pacticipant { Name = "Consumer" },
                Provider = new Pacticipant { Name = "Provider" }
            };

            var interactions = new List<ProviderServiceInteraction>
            {
                new ProviderServiceInteraction
                {
                    Description = "My description",
                    Request = new ProviderServiceRequest
                    {
                        Method = HttpVerb.Get,
                        Path = "/test"
                    },
                    Response = new ProviderServiceResponse
                    {
                        Status = StatusCodes.Status204NoContent
                    }
                }
            };

            var pactFile = new ProviderServicePactFile
            {
                Provider = pactDetails.Provider,
                Consumer = pactDetails.Consumer,
                Interactions = interactions
            };

            var pactFileJson = JsonConvert.SerializeObject(pactFile, JsonConfig.PactFileSerializerSettings);
            var pactDetailsJson = JsonConvert.SerializeObject(pactDetails, JsonConfig.ApiSerializerSettings);

            var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(pactDetailsJson));

            var context = GetRequestContext("POST", "/pact", "http", "localhost", jsonStream);

            var handler = GetSubject();

            _mockProviderRepository.Interactions.Returns(interactions);

            handler.Handle(context);

            _mockFileAdapter.Received(1).WriteAllText(Path.Combine(Constants.DefaultPactDir, pactDetails.GeneratePactFileName()), pactFileJson);
        }

        [Fact]
        public void Handle_WithAPostRequestToPactAndInteractionsHaveBeenRegistered_ReturnsOkResponse()
        {
            var pactDetails = new PactDetails
            {
                Consumer = new Pacticipant { Name = "Consumer" },
                Provider = new Pacticipant { Name = "Provider" }
            };

            var interactions = new List<ProviderServiceInteraction>
            {
                new ProviderServiceInteraction
                {
                    Description = "My description",
                    Request = new ProviderServiceRequest
                    {
                        Method = HttpVerb.Get,
                        Path = "/test"
                    },
                    Response = new ProviderServiceResponse
                    {
                        Status = StatusCodes.Status204NoContent
                    }
                }
            };

            var pactDetailsJson = JsonConvert.SerializeObject(pactDetails, JsonConfig.ApiSerializerSettings);

            var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(pactDetailsJson));

            var context = GetRequestContext("POST", "/pact", "http", "localhost", jsonStream);

            var handler = GetSubject();

            _mockProviderRepository.Interactions.Returns(interactions);

            handler.Handle(context);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public void Handle_WithAPostRequestToPactAndInteractionsHaveBeenRegistered_ReturnsResponseWithPactFileJson()
        {
            var pactDetails = new PactDetails
            {
                Consumer = new Pacticipant { Name = "Consumer" },
                Provider = new Pacticipant { Name = "Provider" }
            };

            var interactions = new List<ProviderServiceInteraction>
            {
                new ProviderServiceInteraction
                {
                    Description = "My description",
                    Request = new ProviderServiceRequest
                    {
                        Method = HttpVerb.Get,
                        Path = "/test"
                    },
                    Response = new ProviderServiceResponse
                    {
                        Status = StatusCodes.Status204NoContent
                    }
                }
            };

            var pactFile = new ProviderServicePactFile
            {
                Provider = pactDetails.Provider,
                Consumer = pactDetails.Consumer,
                Interactions = interactions
            };

            var pactFileJson = JsonConvert.SerializeObject(pactFile, JsonConfig.PactFileSerializerSettings);
            var pactDetailsJson = JsonConvert.SerializeObject(pactDetails, JsonConfig.ApiSerializerSettings);

            var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(pactDetailsJson));

            var context = GetRequestContext("POST", "/pact", "http", "localhost", jsonStream);

            var handler = GetSubject();

            _mockProviderRepository.Interactions.Returns(interactions);

            var task = handler.Handle(context);
            task.Wait();

            Assert.Equal("application/json", context.Response.Headers["Content-Type"]);
            Assert.Equal(pactFileJson, ReadResponseContent(context.Response));
        }

        [Fact]
        public void Handle_WithAPostRequestToPactAndPactDirIsDifferentFromDefault_NewPactFileIsSavedWithInteractionsInTheSpecifiedPath()
        {
            var pactDetails = new PactDetails
            {
                Consumer = new Pacticipant { Name = "Consumer" },
                Provider = new Pacticipant { Name = "Provider" }
            };

            var interactions = new List<ProviderServiceInteraction>
            {
                new ProviderServiceInteraction
                {
                    Description = "My description",
                    Request = new ProviderServiceRequest
                    {
                        Method = HttpVerb.Get,
                        Path = "/test"
                    },
                    Response = new ProviderServiceResponse
                    {
                        Status = StatusCodes.Status204NoContent
                    }
                }
            };

            var pactFile = new ProviderServicePactFile
            {
                Provider = pactDetails.Provider,
                Consumer = pactDetails.Consumer,
                Interactions = interactions
            };

            var config = new PactConfig { PactDir = @"C:\temp" };
            var filePath = Path.Combine(config.PactDir, pactDetails.GeneratePactFileName());

            var pactFileJson = JsonConvert.SerializeObject(pactFile, JsonConfig.PactFileSerializerSettings);
            var pactDetailsJson = JsonConvert.SerializeObject(pactDetails, JsonConfig.ApiSerializerSettings);

            var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(pactDetailsJson));

            var context = GetRequestContext("POST", "/pact", "http", "localhost", jsonStream);

            var handler = GetSubject(config);

            _mockProviderRepository.Interactions.Returns(interactions);

            handler.Handle(context);

            _mockFileAdapter.Received(1).WriteAllText(filePath, pactFileJson);
        }

        [Fact]
        public void Handle_WithAPostRequestToPactAndDirectoryDoesNotExist_DirectoryIsCreatedAndNewPactFileIsSavedWithInteractions()
        {
            var pactDetails = new PactDetails
            {
                Consumer = new Pacticipant { Name = "Consumer" },
                Provider = new Pacticipant { Name = "Provider" }
            };

            var interactions = new List<ProviderServiceInteraction>
            {
                new ProviderServiceInteraction
                {
                    Description = "My description",
                    Request = new ProviderServiceRequest
                    {
                        Method = HttpVerb.Get,
                        Path = "/test"
                    },
                    Response = new ProviderServiceResponse
                    {
                        Status = StatusCodes.Status204NoContent
                    }
                }
            };

            var pactFile = new ProviderServicePactFile
            {
                Provider = pactDetails.Provider,
                Consumer = pactDetails.Consumer,
                Interactions = interactions
            };

            var pactFileJson = JsonConvert.SerializeObject(pactFile, JsonConfig.PactFileSerializerSettings);
            var pactDetailsJson = JsonConvert.SerializeObject(pactDetails, JsonConfig.ApiSerializerSettings);

            var jsonStream = new MemoryStream(Encoding.UTF8.GetBytes(pactDetailsJson));

            var context = GetRequestContext("POST", "/pact", "http", "localhost", jsonStream);

            var filePath = Path.Combine(Constants.DefaultPactDir, pactDetails.GeneratePactFileName());

            var handler = GetSubject();

            _mockProviderRepository.Interactions.Returns(interactions);

            var writeAllTextCount = 0;
            _mockFileAdapter
                .When(x => x.WriteAllText(filePath, pactFileJson))
                .Do(x =>
                {
                    writeAllTextCount++;
                    if (writeAllTextCount == 1)
                    {
                        throw new DirectoryNotFoundException("It doesn't exist");
                    }
                });

            handler.Handle(context);

            _mockFileAdapter.Received(2).WriteAllText(filePath, pactFileJson);
        }

        [Fact]
        public void Handle_WhenNoMatchingAdminAction_ReturnsNotFoundResponse()
        {
            var context = GetRequestContext("GET", "/tester/testing", "http");

            var handler = GetSubject();

            handler.Handle(context);

            Assert.Equal(StatusCodes.Status404NotFound, context.Response.StatusCode);
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
