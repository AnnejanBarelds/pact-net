using System;
using Newtonsoft.Json;
using PactNet.Configuration.Json;
using PactNet.Logging;
using PactNet.Mocks.MockHttpService.Mappers;
using PactNet.Mocks.MockHttpService.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace PactNet.Mocks.MockHttpService.Host
{
    internal class MockProviderRequestHandler : IMockProviderRequestHandler
    {
        private readonly IHttpResponseMapper _responseMapper;
        private readonly IProviderServiceRequestMapper _requestMapper;
        private readonly IMockProviderRepository _mockProviderRepository;
        private readonly ILog _log;

        public MockProviderRequestHandler(
            IProviderServiceRequestMapper requestMapper,
            IHttpResponseMapper responseMapper,
            IMockProviderRepository mockProviderRepository,
            ILog log)
        {
            _requestMapper = requestMapper;
            _responseMapper = responseMapper;
            _mockProviderRepository = mockProviderRepository;
            _log = log;
        }

        public async Task Handle(HttpContext context)
        {
            await HandlePactRequest(context);
        }

        private async Task HandlePactRequest(HttpContext context)
        {
            var actualRequest = _requestMapper.Convert(context.Request);
            var actualRequestMethod = actualRequest.Method.ToString().ToUpperInvariant();
            var actualRequestPath = actualRequest.Path;

            _log.InfoFormat("Received request {0} {1}", actualRequestMethod, actualRequestPath);
            _log.Debug(JsonConvert.SerializeObject(actualRequest, JsonConfig.PactFileSerializerSettings));

            ProviderServiceInteraction matchingInteraction;
            
            try
            {
                matchingInteraction = _mockProviderRepository.GetMatchingTestScopedInteraction(actualRequest);
                _mockProviderRepository.AddHandledRequest(new HandledRequest(actualRequest, matchingInteraction));

                _log.InfoFormat("Found matching response for {0} {1}", actualRequestMethod, actualRequestPath);
                _log.Debug(JsonConvert.SerializeObject(matchingInteraction.Response, JsonConfig.PactFileSerializerSettings));
            }
            catch (Exception)
            {
                _log.ErrorFormat("No matching interaction found for {0} {1}", actualRequestMethod, actualRequestPath);
                _mockProviderRepository.AddHandledRequest(new HandledRequest(actualRequest, null));
                throw;
            }

            await _responseMapper.Convert(context, matchingInteraction.Response);
        }
    }
}