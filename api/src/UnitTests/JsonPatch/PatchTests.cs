using System.IO;
using Foundatio.Skeleton.Core.JsonPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Foundatio.Skeleton.UnitTests.Core {
    public class PatchTests {
        [Fact]
        public void CreateEmptyPatch() {
            var sample = GetSample2();
            var sampletext = sample.ToString();

            var patchDocument = new PatchDocument();
            new JsonPatcher().Patch(ref sample, patchDocument);

            Assert.Equal(sampletext, sample.ToString());
        }

        [Fact]
        public void TestExample1() {
            var targetDoc = JToken.Parse("{ 'foo': 'bar'}");
            var patchDoc = PatchDocument.Parse(@"[
                                                    { 'op': 'add', 'path': '/baz', 'value': 'qux' }
                                                ]");
            new JsonPatcher().Patch(ref targetDoc, patchDoc);


            Assert.True(JToken.DeepEquals(JToken.Parse(@"{
                                                             'foo': 'bar',
                                                             'baz': 'qux'
                                                           }"), targetDoc));
        }

        [Fact]
        public void SerializePatchDocument() {
            var patchDoc = new PatchDocument(new Operation[]
            {
             new TestOperation {Path = "/a/b/c", Value = new JValue("foo")},
             new RemoveOperation {Path = "/a/b/c" },
             new AddOperation {Path = "/a/b/c", Value = new JArray(new JValue("foo"), new JValue("bar"))},
             new ReplaceOperation {Path = "/a/b/c", Value = new JValue(42)},
             new MoveOperation {FromPath = "/a/b/c", Path = "/a/b/d" },
             new CopyOperation {FromPath = "/a/b/d", Path = "/a/b/e" },
            });

            var outputstream = patchDoc.ToStream();
            var output = new StreamReader(outputstream).ReadToEnd();

            var jOutput = JToken.Parse(output);

            Assert.Equal(@"[{""op"":""test"",""path"":""/a/b/c"",""value"":""foo""},{""op"":""remove"",""path"":""/a/b/c""},{""op"":""add"",""path"":""/a/b/c"",""value"":[""foo"",""bar""]},{""op"":""replace"",""path"":""/a/b/c"",""value"":42},{""op"":""move"",""path"":""/a/b/d"",""from"":""/a/b/c""},{""op"":""copy"",""path"":""/a/b/e"",""from"":""/a/b/d""}]",
                jOutput.ToString(Formatting.None));
        }

        public static JToken GetSample2() {
            return JToken.Parse(@"{
    'books': [
        {
          'title' : 'The Great Gatsby',
          'author' : 'F. Scott Fitzgerald'
        },
        {
          'title' : 'The Grapes of Wrath',
          'author' : 'John Steinbeck'
        }
    ]
}");
        }
    }
}
