using System;
using System.Collections.Generic;
using Foundatio.Repositories.Models;
using Foundatio.Skeleton.Core.Collections;
using Foundatio.Skeleton.Core.Models;

namespace Foundatio.Skeleton.Domain.Models
{
    public class Notification : IHaveData, IHaveDates, IOwnedByOrganizationWithIdentity
    {
        /// <summary>
        /// The identity for this notification.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The organization that owns this notification (used for datasources that are private to an org).
        /// </summary>
        public string OrganizationId { get; set; }

        /// <summary>
        /// The user that this notification is for. If null, notification is for whole organization.
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets the type of notification.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets or sets the notification message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Optional data entries that contain additional configuration information for this notification.
        /// </summary>
        public DataDictionary Data { get; set; }

        /// <summary>
        /// Gets or sets the users that have read this notification.
        /// </summary>
        public ISet<string> Readers { get; set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime UpdatedUtc { get; set; }

        public Notification()
        {
            Data = new DataDictionary();
            Readers = new HashSet<string>();
        }

    }
}
