using System;
using System.Collections.Generic;
using System.Text;
using LV.SignFlow.Application.Common.Models;
namespace LV.SignFlow.Application.Common.Interfaces
{
    public interface ICompletionCertificateGenerator
    {
        Task<byte[]> GenerateAsync(
            CompletionCertificateData data,
            CancellationToken cancellationToken = default);
    }
}
