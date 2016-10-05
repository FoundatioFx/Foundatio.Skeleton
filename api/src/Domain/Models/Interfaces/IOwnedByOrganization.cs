﻿using System;

namespace Foundatio.Skeleton.Domain.Models
{
    public interface IOwnedByOrganization
    {
        /// <summary>
        /// The organization that the document belongs to.
        /// </summary>
        string OrganizationId { get; set; }
    }
}