using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using LV.SignFlow.Application.Common.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Storage
{
    public sealed class AzureBlobFileStorage
        : IFileStorage
    {
        private readonly BlobContainerClient
            _containerClient;

        public AzureBlobFileStorage(
            IOptions<StorageOptions> options)
        {
            var settings =
                options.Value;

            if (string.IsNullOrWhiteSpace(
                    settings.ServiceUri))
            {
                throw new InvalidOperationException(
                    "Storage:ServiceUri is required for Azure Blob Storage.");
            }

            if (string.IsNullOrWhiteSpace(
                    settings.ContainerName))
            {
                throw new InvalidOperationException(
                    "Storage:ContainerName is required.");
            }

            DefaultAzureCredential credential;

            if (string.IsNullOrWhiteSpace(
                    settings.ManagedIdentityClientId))
            {
                credential =
                    new DefaultAzureCredential();
            }
            else
            {
                credential =
                    new DefaultAzureCredential(
                        new DefaultAzureCredentialOptions
                        {
                            ManagedIdentityClientId =
                                settings.ManagedIdentityClientId
                        });
            }

            var serviceClient =
                new BlobServiceClient(
                    new Uri(
                        settings.ServiceUri),
                    credential);

            _containerClient =
                serviceClient
                    .GetBlobContainerClient(
                        settings.ContainerName);
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

            var blobPath =
                BuildBlobPath(
                    folder,
                    storedFileName);

            var blobClient =
                _containerClient
                    .GetBlobClient(
                        blobPath);

            var options =
                new BlobUploadOptions
                {
                    HttpHeaders =
                        new BlobHttpHeaders
                        {
                            ContentType =
                                string.IsNullOrWhiteSpace(
                                    contentType)
                                    ? "application/octet-stream"
                                    : contentType
                        }
                };

            await blobClient.UploadAsync(
                stream,
                options,
                cancellationToken);

            return blobPath;
        }

        public async Task<Stream> OpenReadAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            var blobPath =
                NormalizeBlobPath(
                    path);

            var blobClient =
                _containerClient
                    .GetBlobClient(
                        blobPath);

            var exists =
                await blobClient.ExistsAsync(
                    cancellationToken);

            if (!exists.Value)
            {
                throw new FileNotFoundException(
                    "Stored blob was not found.",
                    blobPath);
            }

            return await blobClient.OpenReadAsync(
                new BlobOpenReadOptions(
                    allowModifications: false),
                cancellationToken);
        }

        public Task<Stream> ReadAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            return OpenReadAsync(
                path,
                cancellationToken);
        }

        public async Task<bool> ExistsAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            var blobClient =
                _containerClient
                    .GetBlobClient(
                        NormalizeBlobPath(path));

            var result =
                await blobClient.ExistsAsync(
                    cancellationToken);

            return result.Value;
        }

        public async Task DeleteAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            var blobClient =
                _containerClient
                    .GetBlobClient(
                        NormalizeBlobPath(path));

            await blobClient.DeleteIfExistsAsync(
                DeleteSnapshotsOption.IncludeSnapshots,
                cancellationToken:
                    cancellationToken);
        }

        private static string BuildBlobPath(
            string folder,
            string fileName)
        {
            var normalizedFolder =
                NormalizeBlobPath(
                    folder,
                    allowEmpty: true);

            return string.IsNullOrWhiteSpace(
                    normalizedFolder)
                ? fileName
                : $"{normalizedFolder}/{fileName}";
        }

        private static string NormalizeBlobPath(
            string path,
            bool allowEmpty = false)
        {
            var normalized =
                (path ?? string.Empty)
                    .Replace('\\', '/')
                    .Trim('/');

            if (
                !allowEmpty &&
                string.IsNullOrWhiteSpace(
                    normalized))
            {
                throw new ArgumentException(
                    "Storage path is required.",
                    nameof(path));
            }

            if (
                normalized
                    .Split(
                        '/',
                        StringSplitOptions.RemoveEmptyEntries)
                    .Any(x => x == ".."))
            {
                throw new InvalidOperationException(
                    "Invalid blob path.");
            }

            return normalized;
        }
    }
}
