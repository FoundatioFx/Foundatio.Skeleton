using System;
using System.Threading.Tasks;
using Foundatio.Caching;
using Foundatio.Skeleton.Core.Extensions;

namespace Foundatio.Skeleton.Extensions {
    public static class CacheClientExtensions {
        public static async Task<bool> TrySetAsync<T>(this ICacheClient client, string key, T value) {
            try {
                return await client.SetAsync(key, value).AnyContext();
            } catch (Exception) {
                return false;
            }
        }

        public static async Task<bool> TrySetAsync<T>(this ICacheClient client, string key, T value, TimeSpan expiresIn) {
            try {
                return await client.SetAsync(key, value, expiresIn).AnyContext();
            } catch (Exception) {
                return false;
            }
        }

        public static async Task<double> IncrementIfAsync(this ICacheClient client, string key, int value, TimeSpan timeToLive, bool shouldIncrement, long? startingValue = null) {
            if (!startingValue.HasValue)
                startingValue = 0;

            var count = await client.GetAsync<long>(key).AnyContext();
            if (!shouldIncrement)
                return count.HasValue ? count.Value : startingValue.Value;

            if (count.HasValue)
                return await client.IncrementAsync(key, value).AnyContext();

            long newValue = startingValue.Value + value;
            await client.SetAsync(key, newValue, timeToLive).AnyContext();
            return newValue;
        }

        public static async Task<double> IncrementAsync(this ICacheClient client, string key, int value, TimeSpan timeToLive, long? startingValue = null) {
            if (!startingValue.HasValue)
                startingValue = 0;

            var count = await client.GetAsync<long?>(key).AnyContext();
            if (count.HasValue)
                return await client.IncrementAsync(key, value).AnyContext();

            long newValue = startingValue.Value + value;
            await client.SetAsync(key, newValue, timeToLive).AnyContext();
            return newValue;
        }
    }
}
