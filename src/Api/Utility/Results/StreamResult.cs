using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Foundatio.Skeleton.Api.Utility.Results
{
    public class StreamResult : IHttpActionResult
    {
        private readonly Stream _stream;
        private readonly string _contentType;

        public StreamResult(Stream stream, string contentType)
        {
            if (stream.Length == 0) throw new ArgumentNullException("stream");

            _stream = stream;
            _contentType = contentType;
        }

        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            return Task.Run(() =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StreamContent(_stream)
                };

                response.Content.Headers.ContentType = new MediaTypeHeaderValue(_contentType);

                return response;

            }, cancellationToken);
        }
    }
}