using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using PactNet.Logging;
using PactNet.Mocks.MockHttpService.Mappers;
using PactNet.Mocks.MockHttpService.Comparers;
using Thinktecture.IO;
using Thinktecture.IO.Adapters;

namespace PactNet.Mocks.MockHttpService.Nancy
{
    public class Startup
    {
        //private readonly IConfiguration config;

        public Startup(IHostingEnvironment env)
        {

        }

        public void ConfigureServices(IServiceCollection services)
        {
            var config = new PactConfig(); // TODO: Get this from the caller

            services.AddSingleton(config); 
            services.AddSingleton(LogProvider.GetLogger(config.LoggerName));

            services.AddSingleton<IMockProviderRepository, MockProviderRepository>();
            services.AddTransient<IProviderServiceRequestMapper, ProviderServiceRequestMapper>();
            services.AddTransient<IProviderServiceRequestComparer, ProviderServiceRequestComparer>();
            services.AddTransient<INancyResponseMapper, NancyResponseMapper>();
            services.AddTransient<IMockProviderRequestHandler, MockProviderRequestHandler>();
            services.AddTransient<IMockProviderAdminRequestHandler, MockProviderAdminRequestHandler>();
            services.AddTransient<IFile, FileAdapter>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UsePact();
            //var appConfig = new AppConfiguration();
            //ConfigurationBinder.Bind(config, appConfig);
            //app.Run(async (context) =>
            //{
                
            //});

            //app.UseOwin(x => x.UseNancy(opt => opt.Bootstrapper = new MockProviderNancyBootstrapper(new PactConfig())));
        }
    }
}
