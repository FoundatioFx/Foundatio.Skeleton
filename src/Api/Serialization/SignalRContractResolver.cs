using System;
using System.Reflection;
using Foundatio.Skeleton.Core.Serialization;
using Microsoft.AspNet.SignalR.Infrastructure;
using Newtonsoft.Json.Serialization;

namespace Foundatio.Skeleton.Api.Serialization {
    public class SignalRContractResolver : IContractResolver {
        private readonly Assembly _assembly;
        //  camel case on 9-16-2016;             todo:  make this the default across the board
        private readonly IContractResolver _defaultContractResolver;
        //  lower case underscore on 9-16-2016;  todo:  change this to be the native default
        private readonly IContractResolver _appDefaultContractResolver;

        public SignalRContractResolver() {
            _defaultContractResolver = new DefaultContractResolver();
            _appDefaultContractResolver = JsonHelper.DefaultContractResolver;
            _assembly = typeof(Connection).Assembly;
        }

        public JsonContract ResolveContract(Type type) {
            if (type.Assembly.Equals(_assembly))
                return _defaultContractResolver.ResolveContract(type);

            return _appDefaultContractResolver.ResolveContract(type);
        }
    }
}
