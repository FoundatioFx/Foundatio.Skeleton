using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Foundatio.Storage;

namespace Foundatio.Skeleton.Core.Utility {
    public interface IPublicFileStorage : IFileStorage { }

    public class PublicFileStorage : IPublicFileStorage {
        private readonly IFileStorage _storage;

        public PublicFileStorage(IFileStorage fileStorage) {
            _storage = fileStorage;
        }

        public Task<Stream> GetFileStreamAsync(string path, CancellationToken cancellationToken = new CancellationToken()) {
            return _storage.GetFileStreamAsync(path, cancellationToken);
        }

        public Task<FileSpec> GetFileInfoAsync(string path) {
            return _storage.GetFileInfoAsync(path);
        }

        public Task<bool> ExistsAsync(string path) {
            return _storage.ExistsAsync(path);
        }

        public Task<bool> SaveFileAsync(string path, Stream stream, CancellationToken cancellationToken = new CancellationToken()) {
            return _storage.SaveFileAsync(path, stream, cancellationToken);
        }

        public Task<bool> RenameFileAsync(string path, string newpath, CancellationToken cancellationToken = new CancellationToken()) {
            return _storage.RenameFileAsync(path, newpath, cancellationToken);
        }

        public Task<bool> CopyFileAsync(string path, string targetpath, CancellationToken cancellationToken = new CancellationToken()) {
            return _storage.CopyFileAsync(path, targetpath, cancellationToken);
        }

        public Task<bool> DeleteFileAsync(string path, CancellationToken cancellationToken = new CancellationToken()) {
            return _storage.DeleteFileAsync(path, cancellationToken);
        }

        public Task<IEnumerable<FileSpec>> GetFileListAsync(string searchPattern = null, int? limit = null, int? skip = null,
            CancellationToken cancellationToken = new CancellationToken()) {
                return _storage.GetFileListAsync(searchPattern, limit, skip, cancellationToken);
        }

        public void Dispose() {
            _storage.Dispose();
        }
    }
}
