using System;

namespace Foundatio.Skeleton.Api.Utility.Results
{
    //public class ByteResult : IHttpActionResult
    //{
    //    private readonly byte[] _bytes;
    //    private readonly string _contentType;

    //    public ByteResult(byte[] bytes, string contentType)
    //    {
    //        if (bytes.Length == 0) throw new ArgumentNullException("bytes");

    //        this._bytes = bytes;
    //        this._contentType = contentType;
    //    }

    //    public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
    //    {
    //        return Task.Run(() =>
    //        {
    //            var response = new HttpResponseMessage(HttpStatusCode.OK)
    //            {
    //                Content = new StreamContent(new MemoryStream(_bytes))
    //            };

    //            response.Content.Headers.ContentType = new MediaTypeHeaderValue(_contentType);

    //            return response;

    //        }, cancellationToken);
    //    }
    //}
}