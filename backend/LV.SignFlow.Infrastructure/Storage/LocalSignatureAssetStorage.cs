using LV.SignFlow.Application.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace LV.SignFlow.Infrastructure.Storage
{
    public class LocalSignatureAssetStorage : ISignatureAssetStorage
    {
        private readonly string _rootPath;

        public LocalSignatureAssetStorage()
        {
            _rootPath = Path.Combine(
                AppContext.BaseDirectory,
                "Storage",
                "Signatures");
        }

        public async Task<string> SaveTypedAsync(
            Guid userId,
            string text,
            string? fontKey,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException(
                    "Signature text is required.");
            }

            var directory =
                Path.Combine(
                    _rootPath,
                    userId.ToString());

            Directory.CreateDirectory(directory);

            var fileName =
                $"{Guid.NewGuid():N}.svg";

            var fullPath =
                Path.Combine(
                    directory,
                    fileName);

            var encodedText =
                WebUtility.HtmlEncode(
                    text.Trim());

            var fontFamily =
                GetFontFamily(fontKey);

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

            await File.WriteAllTextAsync(
                fullPath,
                svg,
                Encoding.UTF8,
                cancellationToken);

            return Path.Combine(
                userId.ToString(),
                fileName);
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

            var base64 =
                imageDataUrl[prefix.Length..];

            byte[] bytes;

            try
            {
                bytes =
                    Convert.FromBase64String(
                        base64);
            }
            catch (FormatException)
            {
                throw new ArgumentException(
                    "Invalid signature image.");
            }

            // Təxminən 2 MB limit.
            if (bytes.Length > 2 * 1024 * 1024)
            {
                throw new ArgumentException(
                    "Signature image is too large.");
            }

            var directory =
                Path.Combine(
                    _rootPath,
                    userId.ToString());

            Directory.CreateDirectory(
                directory);

            var fileName =
                $"{Guid.NewGuid():N}.png";

            var fullPath =
                Path.Combine(
                    directory,
                    fileName);

            await File.WriteAllBytesAsync(
                fullPath,
                bytes,
                cancellationToken);

            return Path.Combine(
                userId.ToString(),
                fileName);
        }

        public async Task<(byte[] Content, string ContentType)>
            ReadAsync(
                string storagePath,
                CancellationToken cancellationToken = default)
        {
            var fullPath =
                Path.Combine(
                    _rootPath,
                    storagePath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException(
                    "Signature asset was not found.");
            }

            var content =
                await File.ReadAllBytesAsync(
                    fullPath,
                    cancellationToken);

            var extension =
                Path.GetExtension(fullPath)
                    .ToLowerInvariant();

            var contentType =
                extension switch
                {
                    ".svg" => "image/svg+xml",
                    ".png" => "image/png",
                    _ => "application/octet-stream"
                };

            return (
                content,
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
