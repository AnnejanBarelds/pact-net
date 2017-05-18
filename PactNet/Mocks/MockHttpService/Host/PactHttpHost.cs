using System;
using PactNet.Extensions;
using PactNet.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Threading;

namespace PactNet.Mocks.MockHttpService.Host
{
    internal class PactHttpHost : IHttpHost
    {
        private readonly string _baseUri;
        private readonly ILog _log;
        private readonly IPactConfig _config;
        private IWebHost _host;

        internal PactHttpHost(Uri baseUri, string providerName, IPactConfig config) :
            this(baseUri, providerName, config, false)
        {

        }

        internal PactHttpHost(Uri baseUri, string providerName, IPactConfig config, bool bindOnAllAdapters)
        {
            var loggerName = LogProvider.CurrentLogProvider.AddLogger(config.LogDir, providerName.ToLowerSnakeCase(), "{0}_mock_service.log");
            config.LoggerName = loggerName;

            _baseUri = InitUri(baseUri, bindOnAllAdapters);
            _log = LogProvider.GetLogger(config.LoggerName);
            _config = config;
        }

        public void Start()
        {
            Stop();

            _host = new WebHostBuilder()
                .UseKestrel()
                .UseUrls(_baseUri)
                .ConfigureServices(services =>
                {
                    services.AddSingleton(_config);
                })
                .UseStartup<Startup>()
                .Build();
            _host.Start();
            
            _log.InfoFormat("Started {0}", _baseUri);
        }

        public void Stop()
        {
            if (_host != null)
            {
                Dispose(_host);
                _host = null;
                _log.InfoFormat("Stopped {0}", _baseUri);

                LogProvider.CurrentLogProvider.RemoveLogger(_config.LoggerName);
            }
        }

        private string InitUri(Uri baseUri, bool bindOnAllAdapters)
        {
            var prefix = new UriBuilder(baseUri).ToString();
            if (bindOnAllAdapters)
            {
                if (!baseUri.Host.Contains("."))
                {
                    prefix = prefix.Replace("localhost", "+");
                }
            }
            return prefix;
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