using FluentValidation;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http.Filters;
using Foundatio.Skeleton.Core.Utility;

namespace Foundatio.Skeleton.Api.Utility {
    internal class ValidationResponse {
        internal struct PropertyError {
            public string Property { get; set; }

            public string Message { get; set; }
        }

        public List<PropertyError> Errors { get; private set; }

        public ValidationResponse(ValidationException e) {
            Errors = new List<PropertyError>();
            foreach (var err in e.Errors) {
                Errors.Add(new PropertyError() {
                    Message = err.ErrorMessage,
                    Property = err.PropertyName
                });
            }
        }
    }

    public class FluentValidationExceptionHandler : ExceptionFilterAttribute {
        public override void OnException(HttpActionExecutedContext actionExecutedContext) {
            if (!(actionExecutedContext.Exception is ValidationException))
                return;

            var res = new ValidationResponse(actionExecutedContext.Exception as ValidationException);

            var sb = new StringBuilder();
            var sw = new StringWriter(sb);
            var jsonWriter = new JsonTextWriter(sw);

            var js = actionExecutedContext.ActionContext.ControllerContext.Configuration.Formatters.JsonFormatter.CreateJsonSerializer();
            js.Serialize(jsonWriter, res);

            actionExecutedContext.Response = new HttpResponseMessage(HttpStatusCode.BadRequest) {
           	    Content = new StringContent(sw.ToString(), Encoding.UTF8, HttpClientHelper.KnownValues.MediaTypeJson)
            };
        }
    }
}
