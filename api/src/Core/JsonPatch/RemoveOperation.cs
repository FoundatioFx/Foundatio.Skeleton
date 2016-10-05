using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Foundatio.Skeleton.Core.JsonPatch {
    public class RemoveOperation : Operation {
        public override void Write(JsonWriter writer) {
            writer.WriteStartObject();

            WriteOp(writer, "remove");
            WritePath(writer, Path);

            writer.WriteEndObject();
        }

        public override void Read(JObject jOperation) {
            Path = jOperation.Value<string>("path");
        }
    }
}