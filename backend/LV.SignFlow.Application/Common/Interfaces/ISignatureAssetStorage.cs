using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Common.Interfaces
{
    public interface ISignatureAssetStorage
    {
        Task<string> SaveTypedAsync(
            Guid userId,
            string text,
            string? fontKey,
            CancellationToken cancellationToken = default);

        Task<string> SaveDrawnAsync(
            Guid userId,
            string imageDataUrl,
            CancellationToken cancellationToken = default);

        Task<(byte[] Content, string ContentType)> ReadAsync(
            string storagePath,
            CancellationToken cancellationToken = default);
    }
}
