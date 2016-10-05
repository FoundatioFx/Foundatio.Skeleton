using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Results;

namespace Foundatio.Skeleton.Api.Utility.Results
{
    public class OkContentDownloadResult<T> : OkNegotiatedContentResult<T>
    {
        private readonly string _fileName;

        public OkContentDownloadResult(T content, ApiController controller, string fileName)
            :base(content, controller)
        {
            _fileName = fileName;
        }

        public override async Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            HttpResponseMessage response = await base.ExecuteAsync(cancellationToken);
            var cd = new ContentDispositionHeaderValue("attachment");
            cd.FileName = _fileName;

            response.Content.Headers.ContentDisposition = cd;
            return response;
        }
    }
}