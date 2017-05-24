﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using PactNet.Comparers;
using PactNet.Configuration.Json;
using PactNet.Logging;
using PactNet.Mocks.MockHttpService.Models;
using PactNet.Models;
using Thinktecture.IO;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Diagnostics;

namespace PactNet.Mocks.MockHttpService.Host
{
    internal class MockProviderAdminRequestHandler : IMockProviderAdminRequestHandler
    {
        private readonly IMockProviderRepository _mockProviderRepository;
        private readonly IFile _fileWrapper;
        private readonly IPactConfig _pactConfig;
        private readonly ILog _log;

        public MockProviderAdminRequestHandler(
            IMockProviderRepository mockProviderRepository,
            IFile fileWrapper,
            IPactConfig pactConfig,
            ILog log)
        {
            _mockProviderRepository = mockProviderRepository;
            _fileWrapper = fileWrapper;
            _pactConfig = pactConfig;
            _log = log;
        }

        public async Task Handle(HttpContext context)
        {
            Task task = null;
            //The first admin request with test context, we should log the context
            if (String.IsNullOrEmpty(_mockProviderRepository.TestContext) &&
                context.Request.Headers != null &&
                context.Request.Headers.Any(x => x.Key == Constants.AdministrativeRequestTestContextHeaderKey))
            {
                _mockProviderRepository.TestContext = context.Request.Headers.Single(x => x.Key == Constants.AdministrativeRequestTestContextHeaderKey).Value.Single();
                _log.InfoFormat("Test context {0}", _mockProviderRepository.TestContext);
            }

            if (context.Request.Method.Equals("DELETE", StringComparison.OrdinalIgnoreCase) &&
                context.Request.Path == Constants.InteractionsPath)
            {
                task = HandleDeleteInteractionsRequest(context);
            }

            else if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                context.Request.Path == Constants.InteractionsPath)
            {
                task = HandlePostInteractionsRequest(context);
            }

            else if (context.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                context.Request.Path == Constants.InteractionsVerificationPath)
            {
                task = HandleGetInteractionsVerificationRequest(context);
            }

            else if (context.Request.Method.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
                context.Request.Path == Constants.PactPath)
            {
                task = HandlePostPactRequest(context);
            }

            else
            {
                task = GenerateResponse(context, StatusCodes.Status404NotFound,
                  String.Format("The {0} request for path {1}, does not have a matching mock provider admin action.", context.Request.Method, context.Request.Path));
            }

            await task;
        }

        private async Task HandleDeleteInteractionsRequest(HttpContext context)
        {
            _mockProviderRepository.ClearTestScopedState();

            _log.Info("Cleared interactions");

            await GenerateResponse(context, StatusCodes.Status200OK, "Deleted interactions");
        }

        private async Task HandlePostInteractionsRequest(HttpContext context)
        {
            var interactionJson = ReadContent(context.Request.Body);
            var interaction = JsonConvert.DeserializeObject<ProviderServiceInteraction>(interactionJson);
            _mockProviderRepository.AddInteraction(interaction);

            _log.InfoFormat("Registered expected interaction {0} {1}", interaction.Request.Method.ToString().ToUpperInvariant(), interaction.Request.Path);
            _log.Debug(JsonConvert.SerializeObject(interaction, JsonConfig.PactFileSerializerSettings));

            await GenerateResponse(context, StatusCodes.Status200OK, "Added interaction");
        }

        private async Task HandleGetInteractionsVerificationRequest(HttpContext context)
        {
            var registeredInteractions = _mockProviderRepository.TestScopedInteractions;

            var comparisonResult = new ComparisonResult();

            //Check all registered interactions have been used once and only once
            if (registeredInteractions.Any())
            {
                foreach (var registeredInteraction in registeredInteractions)
                {
                    var interactionUsages = _mockProviderRepository.HandledRequests.Where(x => x.MatchedInteraction != null && x.MatchedInteraction == registeredInteraction).ToList();

                    if (interactionUsages == null || !interactionUsages.Any())
                    {
                        comparisonResult.RecordFailure(new MissingInteractionComparisonFailure(registeredInteraction));
                    }
                    else if (interactionUsages.Count() > 1)
                    {
                        comparisonResult.RecordFailure(new ErrorMessageComparisonFailure(String.Format("The interaction with description '{0}' and provider state '{1}', was used {2} time/s by the test.", registeredInteraction.Description, registeredInteraction.ProviderState, interactionUsages.Count())));
                    }
                }
            }

            //Have we seen any request that has not be registered by the test?
            if (_mockProviderRepository.HandledRequests != null && _mockProviderRepository.HandledRequests.Any(x => x.MatchedInteraction == null))
            {
                foreach (var handledRequest in _mockProviderRepository.HandledRequests.Where(x => x.MatchedInteraction == null))
                {
                    comparisonResult.RecordFailure(new UnexpectedRequestComparisonFailure(handledRequest.ActualRequest));
                }
            }

            //Have we seen any requests when no interactions were registered by the test?
            if (!registeredInteractions.Any() && 
                _mockProviderRepository.HandledRequests != null && 
                _mockProviderRepository.HandledRequests.Any())
            {
                comparisonResult.RecordFailure(new ErrorMessageComparisonFailure("No interactions were registered, however the mock provider service was called."));
            }

            if (!comparisonResult.HasFailure)
            {
                _log.Info("Verifying - interactions matched");

                await GenerateResponse(context, StatusCodes.Status200OK, "Interactions matched");
                return;
            }

            _log.Error("Verifying - actual interactions do not match expected interactions");

            if (comparisonResult.Failures.Any(x => x is MissingInteractionComparisonFailure))
            {
                _log.Error("Missing requests: " + String.Join(", ", 
                    comparisonResult.Failures
                        .Where(x => x is MissingInteractionComparisonFailure)
                        .Cast<MissingInteractionComparisonFailure>()
                        .Select(x => x.RequestDescription)));
            }

            if (comparisonResult.Failures.Any(x => x is UnexpectedRequestComparisonFailure))
            {
                _log.Error("Unexpected requests: " + String.Join(", ", 
                    comparisonResult.Failures
                        .Where(x => x is UnexpectedRequestComparisonFailure)
                        .Cast<UnexpectedRequestComparisonFailure>()
                        .Select(x => x.RequestDescription)));
            }

            foreach (var failureResult in comparisonResult.Failures.Where(failureResult => !(failureResult is MissingInteractionComparisonFailure) && !(failureResult is UnexpectedRequestComparisonFailure)))
            {
                _log.Error(failureResult.Result);
            }

            var failure = comparisonResult.Failures.First();
            throw new PactFailureException(failure.Result);
        }

        private async Task HandlePostPactRequest(HttpContext context)
        {
            var pactDetailsJson = ReadContent(context.Request.Body);
            var pactDetails = JsonConvert.DeserializeObject<PactDetails>(pactDetailsJson);
            var pactFilePath = Path.Combine(_pactConfig.PactDir, pactDetails.GeneratePactFileName());

            var pactFile = new ProviderServicePactFile
            {
                Provider = pactDetails.Provider,
                Consumer = pactDetails.Consumer,
                Interactions = _mockProviderRepository.Interactions
            };

            var pactFileJson = JsonConvert.SerializeObject(pactFile, JsonConfig.PactFileSerializerSettings);

            try
            {
                _fileWrapper.WriteAllText(pactFilePath, pactFileJson);
            }
            catch (DirectoryNotFoundException)
            {
                Directory.CreateDirectory(_pactConfig.PactDir);
                _fileWrapper.WriteAllText(pactFilePath, pactFileJson);
            }

            await GenerateResponse(context, StatusCodes.Status200OK, pactFileJson, "application/json");
        }

        private async Task GenerateResponse(HttpContext context, int statusCode, string message, string contentType = "text/plain")
        {
            context.Response.Clear();
            context.Response.StatusCode = statusCode;

            context.Response.ContentType = contentType;
            
            await context.Response.WriteAsync(message);
        }

        private string ReadContent(Stream stream)
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }
    }
}