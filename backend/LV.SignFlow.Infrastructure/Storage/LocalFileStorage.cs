using LV.SignFlow.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Storage
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _rootPath;

        public LocalFileStorage()
        {
            _rootPath = Path.Combine(Directory.GetCurrentDirectory(),"storage");
            Directory.CreateDirectory(_rootPath);
        }

        public Task DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            var fullPath = Path.Combine(
             _rootPath,
             path.Replace("/", Path.DirectorySeparatorChar.ToString()));

            if (File.Exists(fullPath))
                File.Delete(fullPath);

            return Task.CompletedTask;
        }

        public Task<Stream> OpenReadAsync(string path, CancellationToken cancellationToken = default)
        {
            var fullPath = Path.Combine(
            _rootPath,
            path.Replace("/", Path.DirectorySeparatorChar.ToString()));

            Stream stream = new FileStream(
                fullPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.Read,
                81920,
                useAsync: true);

            return Task.FromResult(stream);
        }

        public async Task<string> UploadAsync(Stream stream, string fileName, string contentType, string folder, CancellationToken cancellationToken = default)
        {
            var safeFileName =
             $"{Guid.NewGuid()}_{Path.GetFileName(fileName)}";

            var relativePath = Path.Combine(
                folder,
                safeFileName);

            var fullPath = Path.Combine(
                _rootPath,
                relativePath);

            var directory = Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await using var fileStream =
                new FileStream(
                    fullPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.None,
                    81920,
                    useAsync: true);

            await stream.CopyToAsync(
                fileStream,
                cancellationToken);

            return relativePath.Replace("\\", "/");
        }
    }
}
