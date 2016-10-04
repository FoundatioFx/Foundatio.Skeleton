using Foundatio.Skeleton.Core.JsonPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Foundatio.Skeleton.UnitTests.Core {
    public class ReplaceTests {
        [Fact]
        public void Replace_a_property_value_with_a_new_value() {
            var sample = PatchTests.GetSample2();

            var patchDocument = new PatchDocument();
            var pointer = "/books/0/author";

            patchDocument.AddOperation(new ReplaceOperation { Path = pointer, Value = "Bob Brown" });

            new JsonPatcher().Patch(ref sample, patchDocument);

            Assert.Equal("Bob Brown", sample.SelectPatchToken(pointer).Value<string>());
        }

        [Fact]
        public void Replace_non_existant_property() {
            var sample = JToken.Parse(@"{ ""data"": {} }");

            var patchDocument = new PatchDocument();
            var pointer = "/data/author";

            patchDocument.AddOperation(new ReplaceOperation { Path = pointer, Value = "Bob Brown" });

            new JsonPatcher().Patch(ref sample, patchDocument);

            Assert.Equal("Bob Brown", sample.SelectPatchToken(pointer).Value<string>());

            sample = JToken.Parse(@"{}");

            patchDocument = new PatchDocument();
            pointer = "/data/author";

            patchDocument.AddOperation(new ReplaceOperation { Path = pointer, Value = "Bob Brown" });

            new JsonPatcher().Patch(ref sample, patchDocument);

            Assert.Equal("Bob Brown", sample.SelectPatchToken(pointer).Value<string>());

            sample = JToken.Parse(@"{}");

            patchDocument = new PatchDocument();
            pointer = "/";

            patchDocument.AddOperation(new ReplaceOperation { Path = pointer, Value = "Bob Brown" });

            new JsonPatcher().Patch(ref sample, patchDocument);

            Assert.Equal("Bob Brown", sample.SelectPatchToken(pointer).Value<string>());

            sample = JToken.Parse(@"{}");

            patchDocument = new PatchDocument();
            pointer = "/hey/now/0/you";

            patchDocument.AddOperation(new ReplaceOperation { Path = pointer, Value = "Bob Brown" });

            new JsonPatcher().Patch(ref sample, patchDocument);

            Assert.Equal("{}", sample.ToString(Formatting.None));
        }

        [Fact]
        public void Replace_a_property_value_with_an_object() {
            var sample = PatchTests.GetSample2();

            var patchDocument = new PatchDocument();
            var pointer = "/books/0/author";

            patchDocument.AddOperation(new ReplaceOperation { Path = pointer, Value = new JObject(new[] { new JProperty("hello", "world") }) });

            new JsonPatcher().Patch(ref sample, patchDocument);

            var newPointer = "/books/0/author/hello";
            Assert.Equal("world", sample.SelectPatchToken(newPointer).Value<string>());
        }

        [Fact]
        public void Replace_multiple_property_values_with_jsonpath() {

            var sample = JToken.Parse(@"{
    'books': [
        {
          'title' : 'The Great Gatsby',
          'author' : 'F. Scott Fitzgerald'
        },
        {
          'title' : 'The Grapes of Wrath',
          'author' : 'John Steinbeck'
        },
        {
          'title' : 'Some Other Title',
          'author' : 'John Steinbeck'
        }
    ]
}");

            var patchDocument = new PatchDocument();
            var pointer = "$.books[?(@.author == 'John Steinbeck')].author";

            patchDocument.AddOperation(new ReplaceOperation { Path = pointer, Value = "Eric" });

            new JsonPatcher().Patch(ref sample, patchDocument);

            var newPointer = "/books/1/author";
            Assert.Equal("Eric", sample.SelectPatchToken(newPointer).Value<string>());

            newPointer = "/books/2/author";
            Assert.Equal("Eric", sample.SelectPatchToken(newPointer).Value<string>());
        }
    }
}
