using Foundatio.Skeleton.Core.Collections;
using System;
using System.Collections.Generic;
using Foundatio.Repositories.Models;

namespace Foundatio.Skeleton.Domain.Models {
    public class Token : IOwnedByOrganizationWithIdentity, IHaveDates {
        public Token() {
            Scopes = new HashSet<string>();
            Data = new DataDictionary();
        }

        public string Id { get; set; }

        public string OrganizationId { get; set; }

        public string UserId { get; set; }

        public string Refresh { get; set; }

        public TokenType Type { get; set; }

        public HashSet<string> Scopes { get; set; }

        public DateTime? ExpiresUtc { get; set; }

        public string Notes { get; set; }

        public string CreatedBy { get; set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime UpdatedUtc { get; set; }
        public DataDictionary Data { get; set; }
    }

    public enum TokenType {
        Authentication,
        Access
    }
}
