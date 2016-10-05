using Foundatio.Skeleton.Core.JsonPatch;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Foundatio.Skeleton.UnitTests.Core {
    public class AddTests {
        [Fact]
        public void Add_an_array_element() {

            var sample = PatchTests.GetSample2();

            var patchDocument = new PatchDocument();
            var pointer = "/books/-";

            patchDocument.AddOperation(new AddOperation { Path = pointer, Value = new JObject(new[] { new JProperty("author", "James Brown") }) });

            var patcher = new JsonPatcher();
            patcher.Patch(ref sample, patchDocument);

            var list = sample["books"] as JArray;

            Assert.Equal(3, list.Count);
        }

        [Fact]
        public void Add_an_existing_member_property()  // Why isn't this replace?
        {

            var sample = PatchTests.GetSample2();

            var patchDocument = new PatchDocument();
            var pointer = "/books/0/title";

            patchDocument.AddOperation(new AddOperation { Path = pointer, Value = "Little Red Riding Hood" });

            var patcher = new JsonPatcher();
            patcher.Patch(ref sample, patchDocument);


            var result = sample.SelectPatchToken(pointer).Value<string>();
            Assert.Equal("Little Red Riding Hood", result);

        }

        [Fact]
        public void Add_an_non_existing_member_property()  // Why isn't this replace?
        {
            var sample = PatchTests.GetSample2();

            var patchDocument = new PatchDocument();
            var pointer = "/books/0/SBN";

            patchDocument.AddOperation(new AddOperation { Path = pointer, Value = "213324234343" });

            var patcher = new JsonPatcher();
            patcher.Patch(ref sample, patchDocument);

            var result = sample.SelectPatchToken(pointer).Value<string>();
            Assert.Equal("213324234343", result);
        }
    }
}
