using System;
using Foundatio.Skeleton.Core.JsonPatch;
using Xunit;

namespace Foundatio.Skeleton.UnitTests.Core {
    public class RemoveTests {
        [Fact]
        public void Remove_a_property() {
            var sample = PatchTests.GetSample2();

            var patchDocument = new PatchDocument();
            var pointer = "/books/0/author";

            patchDocument.AddOperation(new RemoveOperation { Path = pointer });

            new JsonPatcher().Patch(ref sample, patchDocument);

            Assert.Null(sample.SelectPatchToken(pointer));
        }

        [Fact]
        public void Remove_an_array_element() {
            var sample = PatchTests.GetSample2();

            var patchDocument = new PatchDocument();
            var pointer = "/books/0";

            patchDocument.AddOperation(new RemoveOperation { Path = pointer });

            var patcher = new JsonPatcher();
            patcher.Patch(ref sample, patchDocument);

            Assert.Null(sample.SelectPatchToken("/books/1"));
        }
    }
}
