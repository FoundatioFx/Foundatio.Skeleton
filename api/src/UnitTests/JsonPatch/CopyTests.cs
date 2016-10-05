using Foundatio.Skeleton.Core.JsonPatch;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Foundatio.Skeleton.UnitTests.Core {
    public class CopyTests {
        [Fact]
        public void Copy_array_element() {
            var sample = PatchTests.GetSample2();

            var patchDocument = new PatchDocument();
            var frompointer = "/books/0";
            var topointer = "/books/-";

            patchDocument.AddOperation(new CopyOperation { FromPath = frompointer, Path = topointer });

            var patcher = new JsonPatcher();
            patcher.Patch(ref sample, patchDocument);

            var result = sample.SelectPatchToken("/books/2");
            Assert.IsType(typeof(JObject), result);

        }

        [Fact]
        public void Copy_property() {
            var sample = PatchTests.GetSample2();

            var patchDocument = new PatchDocument();
            var frompointer = "/books/0/ISBN";
            var topointer = "/books/1/ISBN";

            patchDocument.AddOperation(new AddOperation { Path = frompointer, Value = new JValue("21123123") });
            patchDocument.AddOperation(new CopyOperation { FromPath = frompointer, Path = topointer });

            var patcher = new JsonPatcher();
            patcher.Patch(ref sample, patchDocument);

            var result = sample.SelectPatchToken("/books/1/ISBN");
            Assert.Equal("21123123", result);
        }
    }
}
