using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using NSubstitute;
using PactNet.Mocks.MockHttpService.Mappers;
using PactNet.Mocks.MockHttpService.Models;
using Xunit;
using Microsoft.AspNetCore.Http;

namespace PactNet.Tests.Mocks.MockHttpService.Mappers
{
    public class HttpResponseMapperTests
    {
        private IHttpResponseMapper GetSubject()
        {
            return new HttpResponseMapper();
        }

        [Fact]
        public void Convert_WithResponseWithStatusCode_ReturnsHttpResponseWithStatusCode()
        {
            var response = new ProviderServiceResponse
            {
                Status = 200
            };

            var context = new DefaultHttpContext();
            context.Response.StatusCode = StatusCodes.Status418ImATeapot;

            var mapper = GetSubject();

            var result = mapper.Convert(context, response);

            Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
        }

        [Fact]
        public void Convert_WithResponseThatHasANullBodyAndACustomHeader_ReturnsHttpResponseWithHeaderAndDoesNotAddAContentLengthHeader()
        {
            var response = new ProviderServiceResponse
            {
                Status = 200,
                Headers = new Dictionary<string, string>
                {
                    { "X-Test", "Tester" }
                }
            };
            var context = new DefaultHttpContext();

            var mapper = GetSubject();
            
            var result = mapper.Convert(context, response);

            Assert.Equal(response.Headers.First().Key, context.Response.Headers.First().Key);
            Assert.Equal(response.Headers.First().Value, context.Response.Headers.First().Value);
            Assert.Equal(1, context.Response.Headers.Count());
        }

        [Fact]
        public void Convert_WithResponseThatHasANullBodyAndAContentLengthHeader_ReturnsHttpResponseWithNullBodyAndContentLengthHeader()
        {
            var response = new ProviderServiceResponse
            {
                Status = 200,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Length", "100" }
                }
            };
            var context = new DefaultHttpContext();
            var mapper = GetSubject();

            var result = mapper.Convert(context, response);

            Assert.Equal(0, context.Response.Body.Length);
            Assert.Equal("Content-Length", context.Response.Headers.Last().Key);
            Assert.Equal("100", context.Response.Headers.Last().Value);
        }

        [Fact]
        public void Convert_WithPlainTextBody_CallsConvertOnHttpBodyContentMapperAndAssignsContents()
        {
            const string contentTypeString = "text/plain";
            var response = new ProviderServiceResponse
            {
                Status = 200,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", contentTypeString }
                },
                Body = "This is a plain body"
            };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var httpBodyContent = new HttpBodyContent(body: response.Body, contentType: new MediaTypeHeaderValue(contentTypeString) { CharSet = "utf-8" });

            var mockHttpBodyContentMapper = Substitute.For<IHttpBodyContentMapper>();

            mockHttpBodyContentMapper.Convert(body: Arg.Any<object>(), headers: response.Headers)
                .Returns(httpBodyContent);

            var mapper = new HttpResponseMapper(mockHttpBodyContentMapper);

            var result = mapper.Convert(context, response);

            string content;

            context.Response.Body.Position = 0;
            using (var reader = new StreamReader(context.Response.Body))
            {
                content = reader.ReadToEnd();
            }
            
            Assert.Equal(response.Body, content);
            mockHttpBodyContentMapper.Received(1).Convert(body: Arg.Any<object>(), headers: response.Headers);
        }

        [Fact]
        public void Convert_WithJsonBody_CallsConvertOnHttpBodyContentMapperAndAssignsContents()
        {
            const string contentTypeString = "application/json";
            var response = new ProviderServiceResponse
            {
                Status = 200,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", contentTypeString }
                },
                Body = new
                {
                    Test = "tester",
                    Test2 = 1
                }
            };
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var jsonBody = "{\"Test\":\"tester\",\"Test2\":1}";
            var httpBodyContent = new HttpBodyContent(content: Encoding.UTF8.GetBytes(jsonBody), contentType: new MediaTypeHeaderValue(contentTypeString) { CharSet = "utf-8" });

            var mockHttpBodyContentMapper = Substitute.For<IHttpBodyContentMapper>();

            mockHttpBodyContentMapper.Convert(body: Arg.Any<object>(), headers: response.Headers)
                .Returns(httpBodyContent);

            var mapper = new HttpResponseMapper(mockHttpBodyContentMapper);

            var result = mapper.Convert(context, response);

            string content;
            context.Response.Body.Position = 0;
            using (var reader = new StreamReader(context.Response.Body))
            {
                content = reader.ReadToEnd();
            }

            Assert.Equal(jsonBody, content);
            mockHttpBodyContentMapper.Received(1).Convert(body: Arg.Any<object>(), headers: response.Headers);
        }
    }
}