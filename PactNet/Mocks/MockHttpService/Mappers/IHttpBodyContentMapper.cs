using System.Collections.Generic;
using PactNet.Mocks.MockHttpService.Models;
using System.Dynamic;

namespace PactNet.Mocks.MockHttpService.Mappers
{
    internal interface IHttpBodyContentMapper
    {
        HttpBodyContent Convert(DynamicBodyMapRequest request);
        HttpBodyContent Convert(BinaryContentMapRequest request);
    }
}