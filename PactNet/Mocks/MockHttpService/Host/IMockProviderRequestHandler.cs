using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace PactNet.Mocks.MockHttpService.Host
{
    internal interface IMockProviderRequestHandler
    {
        Task Handle(HttpContext context);
    }
}