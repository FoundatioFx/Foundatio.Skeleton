using System;
using System.Collections.Generic;
using System.Linq;

using Foundatio.Repositories.Models;
using Foundatio.Skeleton.Domain.Models;
using Foundatio.Skeleton.Core.Extensions;
using Foundatio.Skeleton.Core.Utility;

namespace Foundatio.Skeleton.Domain.Extensions {
    public static class CollectionExtensions {
        public static void SetDates<T>(this IEnumerable<T> values) where T : class, IHaveDates {
            if (values == null)
                return;
            foreach (T obj in values) {
                if (obj.CreatedUtc == DateTime.MinValue || obj.CreatedUtc > DateTime.UtcNow)
                    obj.CreatedUtc = DateTime.UtcNow;
                obj.UpdatedUtc = DateTime.UtcNow;
            }
        }

        public static void SetCreatedDates<T>(this IEnumerable<T> values) where T : class, IHaveCreatedDate {
            if (values == null)
                return;
            foreach (T obj in values) {
                if (obj.CreatedUtc == DateTime.MinValue || obj.CreatedUtc > DateTime.UtcNow)
                    obj.CreatedUtc = DateTime.UtcNow;
            }
        }

        public static void EnsureIds<T>(this ICollection<T> values) where T : class, IIdentity {
            if (values == null) return;

            foreach (var value in values.Where(value => value.Id == null))
                value.Id = ObjectId.GenerateNewId().ToString();
        }

        public static void EnsureOrganizationIds<T>(this ICollection<T> values, string organizationId) where T : class, IOwnedByOrganizationWithIdentity {
            if (values == null) return;

            foreach (var value in values.Where(value => value.OrganizationId == null))
                value.OrganizationId = organizationId;
        }

        public static bool TryUpdate<T>(this IList<T> values, T newValue) where T : class, IIdentity
        {
            if (values == null) return false;

            int i = values.IndexOf(e => e.Id == newValue.Id);
            if (i < 0) return false;
            values[i] = newValue;

            return true;
        }

        // TODO(derek): maybe also make an overload for T
        public static bool TryRemove<T>(this IList<T> values, string id) where T : class, IIdentity
        {
            if (values == null) return false;

            int i = values.IndexOf(e => e.Id == id);
            if (i < 0) return false;
            values.RemoveAt(i);

            return true;
        }

        public static bool IsGenericEnumerable(this Type type) {
            var types = type.GetInterfaces()
                .Where(x => x.IsGenericType
                    && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .ToArray();

            return types.Length == 1;
        }

        public static Type GetEnumerableItemType(this Type collectionType) {
            var types = collectionType.GetInterfaces()
                .Where(x => x.IsGenericType
                    && x.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                .ToArray();

            return types.Length == 1 ? types[0].GetGenericArguments()[0] : null;
        }
    }
}


