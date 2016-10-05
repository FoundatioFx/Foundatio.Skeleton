using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http.Filters;
using Foundatio.Skeleton.Core.Utility;

namespace Foundatio.Skeleton.Api.Utility {
    internal class UnauthorizedExceptionResult {
        public string Message { get; private set; }

        public UnauthorizedExceptionResult(UnauthorizedAccessException e) {
            Message = e.Message;
        }
    }

    public class UnauthorizedAccessExceptionHandler : ExceptionFilterAttribute {
        public override void OnException(HttpActionExecutedContext actionExecutedContext) {
            if (!(actionExecutedContext.Exception is UnauthorizedAccessException))
                return;

            var res = new UnauthorizedExceptionResult(actionExecutedContext.Exception as UnauthorizedAccessException);

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            var jsonWriter = new JsonTextWriter(sw);

            var js = actionExecutedContext.ActionContext.ControllerContext.Configuration.Formatters.JsonFormatter.CreateJsonSerializer();
            js.Serialize(jsonWriter, res);

            actionExecutedContext.Response = new HttpResponseMessage(HttpStatusCode.Unauthorized) {
                Content = new StringContent(sw.ToString(), Encoding.UTF8, HttpClientHelper.KnownValues.MediaTypeJson)
            };
        }
    }
}
