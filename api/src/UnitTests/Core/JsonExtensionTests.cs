using System;
using System.Linq;
using Foundatio.Skeleton.Core.Extensions;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Foundatio.Skeleton.UnitTests.Core {
    public class JsonExtensionTests {
        [Fact]
        public void CanFlattenJsonObject() {
            var json = JObject.Parse(@"{ 'test': 1, 'hello': { 'blah': true }, 'stuff': [ 1, 2, 3 ] }");
            var result = json.Flatten();
            var r2 = result.Unflatten();

            Assert.Equal(json.ToString().RemoveWhiteSpace(), r2.ToString().RemoveWhiteSpace());
        }

        [Fact]
        public void CanFlattenJsonArray() {
            var json = JArray.Parse(@"[{ 'test': 1, 'hello': { 'blah': true }, 'stuff': [ 1, 2, 3 ] },{ 'test': 1, 'hello': { 'blah': true }, 'stuff': [ 1, 2, 3 ] }]");
            var result = json.Flatten();
            var r2 = result.Unflatten();

            Assert.Equal(json.ToString().RemoveWhiteSpace(), r2.ToString().RemoveWhiteSpace());
        }



        [Fact]
        public void ParseTest() {
            var original = JArray.Parse(@"[
  {
    'source': '57eae2ce48163703c08251cf',
    'application': '3111176382',
    'program': 'Refactoring Program',
    'submitted': '2013-11-08T10:38:29.661-06:00',
    'status': 'Meh'
  },
  {
    'source': '57eae2ce48163703c08251cf',
    'application': '3121176399',
    'program': 'Submit Flow Test',
    'submitted': '2013-12-27T13:23:19.951-06:00',
    'status': null
  },
  {
    'source': '57eae2ce48163703c08251cd',
    'application': '3121176400',
    'program': 'Submit Flow Test D',
    'submitted': '2013-12-27T13:24:25.443-06:00',
    'status': null
  }
]");

            var current = JArray.Parse(@"[
  {
    'source': '57eae2ce48163703c08251cf',
    'application': '3111176382',
    'program': 'Refactoring Program',
    'submitted': '2013-11-08T10:38:29.661-06:00',
    'status': 'Meh'
  },
  {
    'source': '57eae2ce48163703c08251cf',
    'application': '3121176399',
    'program': 'Submit Flow Test',
    'submitted': '2013-12-27T13:23:19.951-06:00',
    'status': null
  },
  {
    'source': '57eae2ce48163703c08251cf',
    'application': '3121176400',
    'program': 'Submit Flow Test 2',
    'submitted': '2013-12-27T13:24:25.443-06:00',
    'status': null
  }
]");
            // 1) get all source ids from current
            var ids = current.Select(t => (string)t["source"]).Distinct().ToList();

            // 2) remove all found source ids from original
            var cleaned = original.Where(t => !ids.Contains((string)t["source"]));

            var working = new JArray(cleaned);
            string j1 = working.ToString();

            // 3) add current to original
            foreach (var c in current) {
                working.Add(c);
            }
            string j2 = working.ToString();

        }
    }
}
