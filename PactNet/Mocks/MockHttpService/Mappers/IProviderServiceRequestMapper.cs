using Microsoft.AspNetCore.Http;
using PactNet.Mappers;
using PactNet.Mocks.MockHttpService.Models;

namespace PactNet.Mocks.MockHttpService.Mappers
{
    internal interface IProviderServiceRequestMapper : IMapper<HttpRequest, ProviderServiceRequest>
    {
    }
}