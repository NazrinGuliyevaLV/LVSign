using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Common.Interfaces
{
    public interface IFileStorage
    {
        Task<string> UploadAsync(
            Stream stream,
            string fileName,
            string contentType,
            string folder,
            CancellationToken cancellationToken = default);

        Task<Stream> OpenReadAsync(
            string path,
            CancellationToken cancellationToken = default);

         Task<Stream> ReadAsync(
            string path,
            CancellationToken cancellationToken = default);

        Task<bool> ExistsAsync(
            string path,
            CancellationToken cancellationToken = default);

        Task DeleteAsync(
            string path,
            CancellationToken cancellationToken = default);
    }
}
