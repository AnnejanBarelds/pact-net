using System.Collections.Generic;
using PactNet.Mocks.MockHttpService.Models;
using System.Dynamic;

namespace PactNet.Mocks.MockHttpService.Mappers
{
    public interface IHttpBodyContentMapper
    {
        HttpBodyContent Convert(dynamic body, IDictionary<string, string> headers);
        HttpBodyContent Convert(byte[] content, IDictionary<string, string> headers);
    }
}