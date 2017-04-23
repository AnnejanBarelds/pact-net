using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PactNet.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Builder;

namespace PactNet.Mocks.MockHttpService.Nancy
{
    internal class PactMiddleware// : IRequestDispatcher
    {
        private readonly IMockProviderRequestHandler _requestHandler;
        private readonly IMockProviderAdminRequestHandler _adminRequestHandler;
        private readonly ILog _log;
        private readonly PactConfig _pactConfig;

        public PactMiddleware(RequestDelegate next,
    IMockProviderRequestHandler requestHandler,
    IMockProviderAdminRequestHandler adminRequestHandler,
    ILog log,
    PactConfig pactConfig) : this(requestHandler, adminRequestHandler, log, pactConfig)
        {
            //_requestHandler = requestHandler;
            //_adminRequestHandler = adminRequestHandler;
            //_log = log;
            //_pactConfig = pactConfig;
        }

        public PactMiddleware(
            IMockProviderRequestHandler requestHandler,
            IMockProviderAdminRequestHandler adminRequestHandler,
            ILog log,
            PactConfig pactConfig)
        {
            _requestHandler = requestHandler;
            _adminRequestHandler = adminRequestHandler;
            _log = log;
            _pactConfig = pactConfig;
        }

        public async Task Invoke(HttpContext context)//, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentException("context is null");
            }

            try
            {
                if (IsAdminRequest(context.Request))
                {
                    await _adminRequestHandler.Handle(context);
                }
                else
                {
                    await _requestHandler.Handle(context);
                }
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(PactFailureException))
                {
                    _log.ErrorException("Failed to handle the request", ex);
                }

                var exceptionMessage = String.Format("{0} See {1} for details.", 
                    JsonConvert.ToString(ex.Message).Trim('"'), 
                    !String.IsNullOrEmpty(_pactConfig.LoggerName) ? LogProvider.CurrentLogProvider.ResolveLogPath(_pactConfig.LoggerName) : "logs");

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = exceptionMessage;
                await context.Response.WriteAsync(exceptionMessage);
            }
        }

        private static bool IsAdminRequest(HttpRequest request)
        {
            return request.Headers != null &&
                   request.Headers.Any(x => x.Key == Constants.AdministrativeRequestHeaderKey);
        }
    }

    internal static class PactMiddlewareExtensions
    {
        public static IApplicationBuilder UsePact(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PactMiddleware>();
        }
    }
}