﻿using System;
using System.IO;
using System.IO.Compression;

namespace Foundatio.Skeleton.Core.Extensions {
    public static class ByteArrayExtensions {
        public static byte[] Decompress(this byte[] data, string encoding) {
            byte[] decompressedData = null;
            using (var outputStream = new MemoryStream()) {
                using (var inputStream = new MemoryStream(data)) {
                    if (encoding == "gzip")
                        using (var zip = new GZipStream(inputStream, CompressionMode.Decompress)) {
                            zip.CopyTo(outputStream);
                        }
                    else if (encoding == "deflate")
                        using (var zip = new DeflateStream(inputStream, CompressionMode.Decompress)) {
                            zip.CopyTo(outputStream);
                        }
                    else
                        throw new ArgumentException(String.Format("Unsupported encoding type \"{0}\".", encoding), "encoding");
                }

                decompressedData = outputStream.ToArray();
            }

            return decompressedData;
        }

        public static byte[] Compress(this byte[] data) {
            byte[] compressesData;
            using (var outputStream = new MemoryStream()) {
                using (var zip = new GZipStream(outputStream, CompressionMode.Compress)) {
                    zip.Write(data, 0, data.Length);
                }

                compressesData = outputStream.ToArray();
            }

            return compressesData;
        }
    }
}