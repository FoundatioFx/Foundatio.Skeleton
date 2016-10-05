using System;
using System.Net.Http.Formatting;
using Foundatio.Skeleton.Core.Serialization;

namespace Foundatio.Skeleton.Api.Serialization {
    public class AppJsonMediaTypeFormatter : JsonMediaTypeFormatter {
        public AppJsonMediaTypeFormatter() {
            SerializerSettings = JsonHelper.DefaultSerializerSettings;
        }
    }
}
