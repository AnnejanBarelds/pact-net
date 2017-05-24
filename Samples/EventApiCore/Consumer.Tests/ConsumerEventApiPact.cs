using System;
using PactNet;
using PactNet.Mocks.MockHttpService;

namespace Consumer.Core.Tests
{
    public class ConsumerEventApiPact : IDisposable
    {
        public IPactBuilder PactBuilder { get; private set; }
        public IMockProviderService MockProviderService { get; private set; }

        public int MockServerPort { get { return 1234; } }
        public string MockProviderServiceBaseUri { get { return String.Format("http://localhost:{0}", MockServerPort); } }

        public ConsumerEventApiPact()
        {
            var config = new PactConfig()
            {
                LogDir = @"..\..\..\logs\",
                PactDir = @"..\..\..\pacts\"
            };
            PactBuilder = new PactBuilder(config)
                .ServiceConsumer("Consumer")
                .HasPactWith("Event API");

            MockProviderService = PactBuilder.MockService(MockServerPort);
        }

        public void Dispose()
        {
            PactBuilder.Build();
        }
    }
}