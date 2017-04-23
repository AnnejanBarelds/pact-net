using System;
using PactNet.Extensions;
using PactNet.Logging;
using Microsoft.AspNetCore.Hosting;
using System.Threading;

namespace PactNet.Mocks.MockHttpService.Nancy
{
    internal class NancyHttpHost : IHttpHost
    {
        private readonly Uri _baseUri;
        private readonly ILog _log;
        private readonly PactConfig _config;
        private IWebHost _host;

        internal NancyHttpHost(Uri baseUri, string providerName, PactConfig config) :
            this(baseUri, providerName, config, false)
        {

        }

        internal NancyHttpHost(Uri baseUri, string providerName, PactConfig config, bool bindOnAllAdapters)
        {
            var loggerName = LogProvider.CurrentLogProvider.AddLogger(config.LogDir, providerName.ToLowerSnakeCase(), "{0}_mock_service.log");
            config.LoggerName = loggerName;

            _baseUri = baseUri;
            //_bootstrapper = new MockProviderNancyBootstrapper(config);
            _log = LogProvider.GetLogger(config.LoggerName);
            _config = config;
        }

        public void Start()
        {
            Stop();

            _host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(_baseUri.ToString())
                .UseStartup<Startup>()
                .Build();
            _host.Start();
            
            _log.InfoFormat("Started {0}", _baseUri.OriginalString);
        }

        public void Stop()
        {
            if (_host != null)
            {
                Dispose(_host);
                _host = null;
                _log.InfoFormat("Stopped {0}", _baseUri.OriginalString);

                LogProvider.CurrentLogProvider.RemoveLogger(_config.LoggerName);
            }
        }

        private void Dispose(IDisposable disposable)
        {
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}