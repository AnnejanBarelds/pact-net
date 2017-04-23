using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace PactNet.Mocks.MockHttpService.Nancy
{
    internal interface IMockProviderNancyRequestHandler
    {
        Task Handle(HttpContext context);
    }
}