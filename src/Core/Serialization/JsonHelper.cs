using Foundatio.Serializer;
using Newtonsoft.Json;
﻿using System;
using System.Globalization;
using System.Text;
using Foundatio.Skeleton.Core.Collections;
using Newtonsoft.Json.Serialization;
using Foundatio.Skeleton.Core.Extensions;

namespace Foundatio.Skeleton.Core.Serialization {
    public static class JsonHelper {
        static JsonHelper() {
            DefaultContractResolver = new LowerCaseUnderscorePropertyNamesContractResolver();
            DefaultSerializerSettings = new JsonSerializerSettings {
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Include,
                Formatting = Formatting.Indented,
                ContractResolver = DefaultContractResolver
            };
            DefaultSerializer = JsonSerializer.Create(DefaultSerializerSettings);


            DefaultFoundatioSettings = new JsonSerializerSettings {
                DefaultValueHandling = DefaultValueHandling.Include,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
                ContractResolver = new LowerCaseUnderscorePropertyNamesContractResolver()
            };
            DefaultFoundatioSerializer = new JsonNetSerializer(DefaultFoundatioSettings);

            CamelCaseContractResolver = new CamelCasePropertyNamesContractResolver();
            CamelCasePropertyNamesSerializerSettings = new JsonSerializerSettings {
                DefaultValueHandling = DefaultValueHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented,
                ContractResolver = CamelCaseContractResolver
            };
            CamelCasePropertyNamesSerializer = JsonSerializer.Create(CamelCasePropertyNamesSerializerSettings);

            PascalCaseContractResolver = new PascalCasePropertyNamesContractResolver();
            PascalCasePropertyNamesSerializerSettings = new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore,
                ContractResolver = PascalCaseContractResolver
            };
            PascalCasePropertyNamesSerializer = JsonSerializer.Create(PascalCasePropertyNamesSerializerSettings);
        }


        public static IContractResolver DefaultContractResolver { get; }
        public static JsonSerializerSettings DefaultSerializerSettings { get; }
        public static JsonSerializer DefaultSerializer { get; }

        public static JsonSerializerSettings DefaultFoundatioSettings { get; set; }
        public static JsonNetSerializer DefaultFoundatioSerializer { get; }

        public static IContractResolver CamelCaseContractResolver { get; }
        public static JsonSerializerSettings CamelCasePropertyNamesSerializerSettings { get; }
        public static JsonSerializer CamelCasePropertyNamesSerializer { get; }

        public static IContractResolver PascalCaseContractResolver { get; }
        public static JsonSerializerSettings PascalCasePropertyNamesSerializerSettings { get; }
        public static JsonSerializer PascalCasePropertyNamesSerializer { get; }

        private class PascalCasePropertyNamesContractResolver : DefaultContractResolver {
            private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

            protected override string ResolvePropertyName(string s) {
                var sb = new StringBuilder(s.Length);

                bool isPrevLower = false;

                for (var i = 0; i < s.Length; i++) {
                    var c = s[i];

                    sb.Append(char.ToLower(c, Culture));

                    bool isNextUpper = i + 1 < s.Length && char.IsUpper(s[i + 1]);

                    if (isNextUpper && isPrevLower) {
                        sb.Append("_");
                    }

                    isPrevLower = char.IsLower(c);
                }

                return sb.ToString();
            }
        }

        private class LowerCaseUnderscorePropertyNamesContractResolver : DefaultContractResolver {
            protected override JsonDictionaryContract CreateDictionaryContract(Type objectType) {
                if (objectType != typeof(DataDictionary) &&
                    objectType != typeof(SettingsDictionary))
                    return base.CreateDictionaryContract(objectType);

                JsonDictionaryContract contract = base.CreateDictionaryContract(objectType);
                contract.DictionaryKeyResolver = propertyName => propertyName;
                return contract;
            }

            protected override string ResolvePropertyName(string propertyName) {
                return propertyName.ToLowerUnderscoredWords();
            }
        }

    }
}
