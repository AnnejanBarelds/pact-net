using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using PactNet.Logging;
using PactNet.Mocks.MockHttpService.Mappers;
using PactNet.Mocks.MockHttpService.Comparers;
using System.Linq;
using Thinktecture.IO;
using Thinktecture.IO.Adapters;

namespace PactNet.Mocks.MockHttpService.Host
{
    internal class Startup
    {
        public Startup(IHostingEnvironment env)
        {

        }

        public void ConfigureServices(IServiceCollection services)
        {
            var configService = services.First(service => service.ServiceType == typeof(IPactConfig));

            services.AddSingleton(LogProvider.GetLogger(((IPactConfig)configService.ImplementationInstance).LoggerName));
            services.AddSingleton<IMockProviderRepository, MockProviderRepository>();
            services.AddTransient<IProviderServiceRequestMapper, ProviderServiceRequestMapper>();
            services.AddTransient<IProviderServiceRequestComparer, ProviderServiceRequestComparer>();
            services.AddTransient<IHttpResponseMapper, HttpResponseMapper>();
            services.AddTransient<IMockProviderRequestHandler, MockProviderRequestHandler>();
            services.AddTransient<IMockProviderAdminRequestHandler, MockProviderAdminRequestHandler>();
            services.AddTransient<IFile, FileAdapter>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UsePact();
        }
    }
}
