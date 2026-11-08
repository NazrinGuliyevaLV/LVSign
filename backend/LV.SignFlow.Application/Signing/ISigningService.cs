using LV.SignFlow.Application.Signing.Dtos;
using LV.SignFlow.Application.Signing.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Signing
{
    public interface ISigningService
    {
        public Task<SigningSessionDto> GetSigningSessionAsync(Guid envelopeId,CancellationToken cancellationToken=default);
        public Task<SigningFieldValueDto> SaveFieldValueAsync(Guid envelopeId, Guid fieldId, UpdateSigningFieldValueRequest request, CancellationToken cancellationToken = default);
        public Task<SigningAppliedSignatureDto> ApplyMarkAsync(Guid envelopeId, Guid fieldId, ApplySigningMarkRequest request, CancellationToken cancellationToken = default);
        Task<CreatedSigningSignatureDto>  CreateSignatureAsync(CreateSigningSignatureRequest request,CancellationToken cancellationToken = default);
        Task<(byte[] Content, string ContentType)> GetSignatureContentAsync(Guid signatureId,CancellationToken cancellationToken = default);
        Task RemoveMarkAsync(Guid envelopeId, Guid fieldId, CancellationToken cancellationToken = default);

        Task<DeclineSigningResultDto> DeclineSigningAsync(Guid envelopeId,string reason,CancellationToken cancellationToken = default);
        Task<byte[]> GenerateTestFinalPdfAsync(
    Guid envelopeId,
    Guid documentId,
    CancellationToken cancellationToken = default);
    }
}
