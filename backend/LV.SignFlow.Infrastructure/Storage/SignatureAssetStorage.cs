using LV.SignFlow.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace LV.SignFlow.Infrastructure.Storage
{
    public sealed class SignatureAssetStorage
        : ISignatureAssetStorage
    {
        private readonly IFileStorage
            _fileStorage;

        public SignatureAssetStorage(
            IFileStorage fileStorage)
        {
            _fileStorage =
                fileStorage;
        }

        public async Task<string> SaveTypedAsync(
            Guid userId,
            string text,
            string? fontKey,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(
                    text))
            {
                throw new ArgumentException(
                    "Signature text is required.");
            }

            var encodedText =
                WebUtility.HtmlEncode(
                    text.Trim());

            var fontFamily =
                GetFontFamily(
                    fontKey);

            var svg =
                $$"""
                <svg xmlns="http://www.w3.org/2000/svg"
                     width="800"
                     height="240"
                     viewBox="0 0 800 240">
                    <rect width="100%"
                          height="100%"
                          fill="transparent" />

                    <text x="30"
                          y="155"
                          font-family="{{fontFamily}}"
                          font-size="110"
                          fill="#111827">
                        {{encodedText}}
                    </text>
                </svg>
                """;

            var bytes =
                Encoding.UTF8.GetBytes(
                    svg);

            await using var stream =
                new MemoryStream(
                    bytes,
                    writable: false);

            return await _fileStorage.UploadAsync(
                stream,
                "signature.svg",
                "image/svg+xml",
                $"signatures/{userId:N}",
                cancellationToken);
        }

        public async Task<string> SaveDrawnAsync(
            Guid userId,
            string imageDataUrl,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(
                    imageDataUrl))
            {
                throw new ArgumentException(
                    "Signature image is required.");
            }

            const string prefix =
                "data:image/png;base64,";

            if (!imageDataUrl.StartsWith(
                    prefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(
                    "Only PNG signature images are supported.");
            }

            byte[] bytes;

            try
            {
                bytes =
                    Convert.FromBase64String(
                        imageDataUrl[
                            prefix.Length..]);
            }
            catch (FormatException)
            {
                throw new ArgumentException(
                    "Invalid signature image.");
            }

            if (bytes.Length >
                2 * 1024 * 1024)
            {
                throw new ArgumentException(
                    "Signature image is too large.");
            }

            await using var stream =
                new MemoryStream(
                    bytes,
                    writable: false);

            return await _fileStorage.UploadAsync(
                stream,
                "signature.png",
                "image/png",
                $"signatures/{userId:N}",
                cancellationToken);
        }

        public async Task<(
            byte[] Content,
            string ContentType)> ReadAsync(
            string storagePath,
            CancellationToken cancellationToken = default)
        {
            await using var stream =
                await _fileStorage.OpenReadAsync(
                    storagePath,
                    cancellationToken);

            using var memory =
                new MemoryStream();

            await stream.CopyToAsync(
                memory,
                cancellationToken);

            var extension =
                Path.GetExtension(
                    storagePath)
                    .ToLowerInvariant();

            var contentType =
                extension switch
                {
                    ".svg" =>
                        "image/svg+xml",

                    ".png" =>
                        "image/png",

                    ".jpg" or ".jpeg" =>
                        "image/jpeg",

                    _ =>
                        "application/octet-stream"
                };

            return (
                memory.ToArray(),
                contentType);
        }

        private static string GetFontFamily(
            string? fontKey)
        {
            return fontKey switch
            {
                "serif" =>
                    "Georgia, serif",

                "script" =>
                    "cursive",

                "classic" =>
                    "Times New Roman, serif",

                _ =>
                    "cursive"
            };
        }
    }
}
