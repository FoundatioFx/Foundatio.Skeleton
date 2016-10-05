using System;
using Foundatio.Skeleton.Core.JsonPatch;

namespace Foundatio.Skeleton.Api.Models {
    public class EntitySelectionPatches : EntitySelection {
        public PatchDocument Patch { get; set; }
    }
}
