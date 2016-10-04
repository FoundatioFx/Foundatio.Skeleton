using System;
using Foundatio.Repositories.Models;

namespace Foundatio.Skeleton.Domain.Models {
    public class FieldDelta : IHaveCreatedDate {
        public string Name { get; set; }
        public string Display { get; set; }
        public string Operation { get; set; }
        public object Current { get; set; }
        public object Original { get; set; }
        public DateTime CreatedUtc { get; set; }
    }
}
