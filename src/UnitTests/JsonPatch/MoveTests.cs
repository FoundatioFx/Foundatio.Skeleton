using Foundatio.Skeleton.Core.JsonPatch;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Foundatio.Skeleton.UnitTests.Core {
    public class MoveTests {
        [Fact]
        public void Move_property() {
            var sample = PatchTests.GetSample2();

            var patchDocument = new PatchDocument();
            var frompointer = "/books/0/author";
            var topointer = "/books/1/author";

            patchDocument.AddOperation(new MoveOperation { FromPath = frompointer, Path = topointer });

            var patcher = new JsonPatcher();
            patcher.Patch(ref sample, patchDocument);


            var result = sample.SelectPatchToken(topointer).Value<string>();
            Assert.Equal("F. Scott Fitzgerald", result);
        }

        [Fact]
        public void Move_array_element() {
            var sample = PatchTests.GetSample2();

            var patchDocument = new PatchDocument();
            var frompointer = "/books/1";
            var topointer = "/books/0/child";

            patchDocument.AddOperation(new MoveOperation { FromPath = frompointer, Path = topointer });

            var patcher = new JsonPatcher();
            patcher.Patch(ref sample, patchDocument);


            var result = sample.SelectPatchToken(topointer);
            Assert.IsType(typeof(JObject), result);
        }
    }
}
