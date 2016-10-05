using System;
using Newtonsoft.Json;

namespace Foundatio.Skeleton.Core.Extensions {
    public static class ObjectExtensions {
        public static T Copy<T>(this T source) {
            var serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized);
        }
    }
}
