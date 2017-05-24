using Microsoft.AspNetCore.Mvc;
using System;
using System.Text;

namespace Provider.Api.Web.Core.Controllers
{
    public class BlobsController : Controller
    {
        private const string Data = "This is a test";

        [HttpGet("blobs/{id}")]
        public IActionResult GetById(Guid id)
        {
            return new CreatedResult(string.Empty, Data);
        }

        [HttpPost("blobs/{id}")]
        public IActionResult Post(Guid id)
        {
            var length = Request.ContentLength.HasValue ? Request.ContentLength.Value : Request.Body.Length;
            var bytes = new byte[length];
            Request.Body.Read(bytes, 0, (int)length);
            var requestBody = Encoding.UTF8.GetString(bytes);

            if (requestBody != Data)
            {
                return new BadRequestResult();
            }

            return new CreatedResult("", Data);
        }
    }
}