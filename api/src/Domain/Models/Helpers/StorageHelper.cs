using System;

namespace Foundatio.Skeleton.Domain.Models {
    public static class StorageHelper {
        public static string GetPictureUrl(string path) {
            return path == null ? null : String.Concat(Settings.Current.PublicStorageUrlPrefix, "/", path);
        }
    }
}
