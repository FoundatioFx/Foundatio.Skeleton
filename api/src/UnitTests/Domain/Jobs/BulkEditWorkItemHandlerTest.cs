using System;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Foundatio.Skeleton.UnitTests.Domain.Jobs {
    public class BulkEditWorkItemHandlerTest {

        [Fact]
        public void TestPathParse() {
            dynamic operation = JObject.Parse("{ \"op\":\"replace\",\"path\":\"/data/JobTitle\",\"value\":\"Engineer 2\"}");

            var value = (string)operation.value;
            var path = (string)operation.path;
            var parts = path.Split('/');

            var root = new JObject();
            var current = root;

            int len = parts.Length;

            for (int i = 1; i < len; i++) {
                var key = parts[i];
                if (i < len - 1) {
                    // create nested object
                    var c = new JObject();
                    current[key] = c;
                    current = c;
                }
                else {
                    // assign value
                    current[key] = new JValue(value);
                }
            }

            string json = root.ToString();
        }
    }
}
