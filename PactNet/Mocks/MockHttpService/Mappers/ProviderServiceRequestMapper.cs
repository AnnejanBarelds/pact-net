﻿using System;
using System.IO;
using System.Linq;
using PactNet.Mocks.MockHttpService.Models;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace PactNet.Mocks.MockHttpService.Mappers
{
    internal class ProviderServiceRequestMapper : IProviderServiceRequestMapper
    {
        private readonly IHttpVerbMapper _httpVerbMapper;
        private readonly IHttpBodyContentMapper _httpBodyContentMapper;

        internal ProviderServiceRequestMapper(
            IHttpVerbMapper httpVerbMapper,
            IHttpBodyContentMapper httpBodyContentMapper)
        {
            _httpVerbMapper = httpVerbMapper;
            _httpBodyContentMapper = httpBodyContentMapper;
        }

        public ProviderServiceRequestMapper() : this(
            new HttpVerbMapper(),
            new HttpBodyContentMapper())
        {
        }

        public ProviderServiceRequest Convert(HttpRequest from)
        {
            if (from == null)
            {
                return null;
            }
                
            var httpVerb = _httpVerbMapper.Convert(from.Method.ToUpper());

            var to = new ProviderServiceRequest
            {
                Method = httpVerb,
                Path = from.Path,
                Query = from.QueryString.Value.TrimStart('?')
            };

            if (from.Headers != null && from.Headers.Any())
            {
                var fromHeaders = from.Headers.ToDictionary(x => x.Key, x => String.Join(", ", x.Value));
                to.Headers = fromHeaders;
            }

            if (from.Body != null && from.ContentLength.GetValueOrDefault(0) != 0)
            {
                var streamBytes = ConvertStreamToBytes(from.ContentLength.Value, from.Body);
                if (streamBytes.Length > 0)
                {
                    var httpBodyContent = _httpBodyContentMapper.Convert(new BinaryContentMapRequest { Content = streamBytes, Headers = to.Headers });

                    to.Body = httpBodyContent.Body;
                }
            }

            return to;
        }

        private static byte[] ConvertStreamToBytes(long length, Stream content)
        {
            byte[] buffer = new byte[length];
            content.Read(buffer, 0, (int)length);
            return buffer;
        }
    }
}