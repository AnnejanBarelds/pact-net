using Microsoft.AspNetCore.Http;
using PactNet.Mappers;
using PactNet.Mocks.MockHttpService.Models;
using System.Threading.Tasks;

namespace PactNet.Mocks.MockHttpService.Mappers
{
    internal interface IHttpResponseMapper
    {
        Task Convert(HttpContext context, ProviderServiceResponse from);
    }
}