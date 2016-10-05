﻿using System;
using System.Linq;
using System.Text;
using Elasticsearch.Net;
using Nest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Foundatio.Skeleton.Domain.Extensions {
    public static class ElasticSearchExtensions {
        public static string GetRequest(this IResponseWithRequestInformation response) {
            if (response?.RequestInformation == null)
                return String.Empty;

            string json = String.Empty;
            if (response.RequestInformation.RequestUrl.EndsWith("_bulk") && response.RequestInformation?.Request != null && response.RequestInformation.Request.Length > 0) {
                string[] bulkCommands = Encoding.UTF8.GetString(response.RequestInformation.Request).Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                json = String.Join("\r\n", bulkCommands.Select(c => JObject.Parse(c).ToString(Formatting.Indented)));
            } else if (response.RequestInformation?.Request != null && response.RequestInformation.Request.Length > 0) {
                json = JObject.Parse(Encoding.UTF8.GetString(response.RequestInformation.Request)).ToString(Formatting.Indented);
            }

            return $"{response.RequestInformation.RequestMethod.ToUpper()} {response.RequestInformation.RequestUrl}\r\n{json}\r\n";
        }

        public static string GetErrorMessage(this IResponse response) {
            var sb = new StringBuilder();

            if (response.ConnectionStatus?.OriginalException != null)
                sb.AppendLine($"Original: ({response.ConnectionStatus.HttpStatusCode} - {response.ConnectionStatus.OriginalException.GetType().Name}) {response.ConnectionStatus.OriginalException.Message}");

            if (response.ServerError != null)
                sb.AppendLine($"Server: ({response.ServerError.Status} - {response.ServerError.ExceptionType}) {response.ServerError.Error}");
            
            if (sb.Length == 0)
                sb.AppendLine("Unknown error.");

            return sb.ToString();
        }
    }
}
