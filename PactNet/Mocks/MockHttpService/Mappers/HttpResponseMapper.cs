using System.Collections.Generic;
using PactNet.Mocks.MockHttpService.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace PactNet.Mocks.MockHttpService.Mappers
{
    internal class HttpResponseMapper : IHttpResponseMapper
    {
        private readonly IHttpBodyContentMapper _httpBodyContentMapper;

        internal HttpResponseMapper(IHttpBodyContentMapper httpBodyContentMapper)
        {
            _httpBodyContentMapper = httpBodyContentMapper;
        }

        public HttpResponseMapper() : this(new HttpBodyContentMapper())
        {
        }

        public async Task Convert(HttpContext context, ProviderServiceResponse from)
        {
            if (from == null)
            {
                return;
            }
            
            context.Response.StatusCode = from.Status;
            if (from.Headers != null && from.Headers.Count > 0)
            foreach (var header in from.Headers)
            {
                context.Response.Headers.Add(header.Key, new Microsoft.Extensions.Primitives.StringValues(header.Value));
            }

            if (from.Body != null)
            {
                HttpBodyContent bodyContent = _httpBodyContentMapper.Convert(body: from.Body, headers: from.Headers);
                context.Response.ContentType = bodyContent.ContentType.MediaType;
                await context.Response.WriteAsync(bodyContent.Content);
            }
        }
    }
}