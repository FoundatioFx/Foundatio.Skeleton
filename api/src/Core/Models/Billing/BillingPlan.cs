using System;
using System.Diagnostics;

namespace GoodProspect.Core.Models.Billing {
    [DebuggerDisplay("Id: {Id} Name: {Name} Price: {Price}")]
    public class BillingPlan {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public int MaxUsers { get; set; }
        public bool IsHidden { get; set; }
    }
}