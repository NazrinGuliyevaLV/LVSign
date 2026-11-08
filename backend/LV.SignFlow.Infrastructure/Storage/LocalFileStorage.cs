using LV.SignFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Storage
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _rootPath;

        public LocalFileStorage(
            IOptions<StorageOptions> options)
        {
            var configuredPath =
                options.Value.LocalRootPath;

            if (string.IsNullOrWhiteSpace(
                    configuredPath))
            {
                _rootPath =
                    Path.Combine(
                        AppContext.BaseDirectory,
                        "storage");
            }
            else if (
                Path.IsPathRooted(
                    configuredPath))
            {
                _rootPath =
                    Path.GetFullPath(
                        configuredPath);
            }
            else
            {
                _rootPath =
                    Path.GetFullPath(
                        Path.Combine(
                            AppContext.BaseDirectory,
                            configuredPath));
            }

            Directory.CreateDirectory(
                _rootPath);
        }

        public async Task<string> UploadAsync(
            Stream stream,
            string fileName,
            string contentType,
            string folder,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(
                stream);

            var safeFileName =
                Path.GetFileName(
                    fileName);

            if (string.IsNullOrWhiteSpace(
                    safeFileName))
            {
                throw new ArgumentException(
                    "File name is required.",
                    nameof(fileName));
            }

            var storedFileName =
                $"{Guid.NewGuid():N}_{safeFileName}";

            var relativePath =
                BuildRelativePath(
                    folder,
                    storedFileName);

            var fullPath =
                GetFullPath(
                    relativePath);

            var directory =
                Path.GetDirectoryName(
                    fullPath);

            if (!string.IsNullOrWhiteSpace(
                    directory))
            {
                Directory.CreateDirectory(
                    directory);
            }

            await using var output =
                new FileStream(
                    fullPath,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true);

            await stream.CopyToAsync(
                output,
                cancellationToken);

            return relativePath;
        }

        public Task<Stream> OpenReadAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            var fullPath =
                GetFullPath(path);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    "Stored file was not found.",
                    fullPath);
            }

            Stream stream =
                new FileStream(
                    fullPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read,
                    81920,
                    useAsync: true);

            return Task.FromResult(
                stream);
        }

        public Task<Stream> ReadAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            return OpenReadAsync(
                path,
                cancellationToken);
        }

        public Task<bool> ExistsAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            var fullPath =
                GetFullPath(path);

            return Task.FromResult(
                File.Exists(fullPath));
        }

        public Task DeleteAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            var fullPath =
                GetFullPath(path);

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }

        private string GetFullPath(
            string relativePath)
        {
            if (string.IsNullOrWhiteSpace(
                    relativePath))
            {
                throw new ArgumentException(
                    "Storage path is required.",
                    nameof(relativePath));
            }

            var normalized =
                relativePath
                    .Replace('\\', '/')
                    .TrimStart('/');

            if (
                Path.IsPathRooted(
                    normalized) ||
                normalized
                    .Split(
                        '/',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Any(x => x == ".."))
            {
                throw new InvalidOperationException(
                    "Invalid storage path.");
            }

            var platformPath =
                normalized.Replace(
                    '/',
                    Path.DirectorySeparatorChar);

            var fullPath =
                Path.GetFullPath(
                    Path.Combine(
                        _rootPath,
                        platformPath));

            var normalizedRoot =
                Path.GetFullPath(
                    _rootPath)
                .TrimEnd(
                    Path.DirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (!fullPath.StartsWith(
                    normalizedRoot,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "Storage path is outside the storage root.");
            }

            return fullPath;
        }

        private static string BuildRelativePath(
            string folder,
            string fileName)
        {
            var normalizedFolder =
                (folder ?? string.Empty)
                    .Replace('\\', '/')
                    .Trim('/');

            if (
                normalizedFolder
                    .Split(
                        '/',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Any(x => x == ".."))
            {
                throw new InvalidOperationException(
                    "Invalid storage folder.");
            }

            return string.IsNullOrWhiteSpace(
                    normalizedFolder)
                ? fileName
                : $"{normalizedFolder}/{fileName}";
        }
    }
}
