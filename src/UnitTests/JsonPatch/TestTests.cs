using System;
using Foundatio.Skeleton.Core.JsonPatch;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Foundatio.Skeleton.UnitTests.Core {
    public class TestTests {
        [Fact]
        public void Test_a_value() {

            var sample = PatchTests.GetSample2();

            var patchDocument = new PatchDocument();
            var pointer = "/books/0/author";

            patchDocument.AddOperation(new TestOperation { Path = pointer, Value = new JValue("Billy Burton") });

            Assert.Throws(typeof(InvalidOperationException), () => {
                var patcher = new JsonPatcher();
                patcher.Patch(ref sample, patchDocument);
            });

        }
    }
}
