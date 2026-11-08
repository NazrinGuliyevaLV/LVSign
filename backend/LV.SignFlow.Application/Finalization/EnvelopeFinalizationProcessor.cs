using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Application.Common.Mapping;
using LV.SignFlow.Domain.Entities.Envelopes;
using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace LV.SignFlow.Application.Finalization
{
    public sealed class EnvelopeFinalizationProcessor
         : IEnvelopeFinalizationProcessor
    {
        private readonly IRepository<Envelope>
            _envelopeRepository;

        private readonly IRepository<Documents>
            _documentRepository;

        private readonly IRepository<Fields>
            _fieldRepository;

        private readonly IRepository<EnvelopeFieldValue>
            _fieldValueRepository;

        private readonly IRepository<EnvelopeAppliedSignature>
            _appliedSignatureRepository;

        private readonly IFinalPdfRenderer
            _finalPdfRenderer;

        private readonly IFileStorage
            _fileStorage;

        private readonly IUnitOfWork
            _unitOfWork;
        private readonly ICompletionCertificateGenerator
    _certificateGenerator;

        private readonly ICompletionCertificateDataBuilder
            _certificateDataBuilder;

        public EnvelopeFinalizationProcessor(
            IRepository<Envelope> envelopeRepository,
            IRepository<Documents> documentRepository,
            IRepository<Fields> fieldRepository,
            IRepository<EnvelopeFieldValue> fieldValueRepository,
            IRepository<EnvelopeAppliedSignature> appliedSignatureRepository,
            IFinalPdfRenderer finalPdfRenderer,
            IFileStorage fileStorage,
            IUnitOfWork unitOfWork,
            ICompletionCertificateGenerator certificateGenerator,
            ICompletionCertificateDataBuilder certificateDataBuilder)
        {
            _envelopeRepository =
                envelopeRepository;

            _documentRepository =
                documentRepository;

            _fieldRepository =
                fieldRepository;

            _fieldValueRepository =
                fieldValueRepository;

            _appliedSignatureRepository =
                appliedSignatureRepository;

            _finalPdfRenderer =
                finalPdfRenderer;

            _fileStorage =
                fileStorage;

            _unitOfWork =
                unitOfWork;
            _certificateGenerator = certificateGenerator;
            _certificateDataBuilder = certificateDataBuilder;
        }

        public async Task ProcessAsync(
            Guid envelopeId,
            CancellationToken cancellationToken = default)
        {
            if (envelopeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Envelope id is required.",
                    nameof(envelopeId));
            }

            /*
             * Final artifacts yalnız business lifecycle
             * tamamlandıqdan sonra yaradıla bilər.
             */
            var envelope =
                await _envelopeRepository.GetByIdAsync(
                    envelopeId,
                    cancellationToken);

            if (envelope is null ||
                envelope.IsDeleted)
            {
                throw new KeyNotFoundException(
                    "Envelope was not found.");
            }

            if (envelope.Status !=
                EnvelopeStatus.Completed)
            {
                throw new InvalidOperationException(
                    "Only a completed envelope can be finalized.");
            }

            var documents =
                (
                    await _documentRepository.FindAsync(
                        x =>
                            x.EnvelopeId == envelopeId,
                        cancellationToken)
                )
                .OrderBy(x => x.Id)
                .ToList();

            if (documents.Count == 0)
            {
                throw new InvalidOperationException(
                    "The envelope does not contain any documents.");
            }

            /*
             * Bütün envelope data-sını bir dəfə oxuyuruq.
             */
            var fields =
                (
                    await _fieldRepository.FindAsync(
                        x =>
                            x.EnvelopeId == envelopeId,
                        cancellationToken)
                ).ToList();

            var fieldIds =
                fields
                    .Select(x => x.Id)
                    .ToList();

            var values =
                fieldIds.Count == 0
                    ? new List<EnvelopeFieldValue>()
                    : (
                        await _fieldValueRepository.FindAsync(
                            x =>
                                fieldIds.Contains(
                                    x.EnvelopeFieldId),
                            cancellationToken)
                      ).ToList();

            var appliedMarks =
                fieldIds.Count == 0
                    ? new List<EnvelopeAppliedSignature>()
                    : (
                        await _appliedSignatureRepository.FindAsync(
                            x =>
                                x.EnvelopeId == envelopeId &&
                                fieldIds.Contains(
                                    x.EnvelopeFieldId),
                            cancellationToken)
                      ).ToList();

            foreach (var document in documents)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                
                if (IsAlreadyFinalized(document))
                {
                    continue;
                }

                await FinalizeDocumentAsync(
                    envelopeId,
                    document,
                    fields,
                    values,
                    appliedMarks,
                    cancellationToken);
            }
            await FinalizeCertificateAsync(
    envelope,
    documents,
    cancellationToken);
        }
        private async Task FinalizeCertificateAsync(
    Envelope envelope,
    IReadOnlyCollection<Documents> documents,
    CancellationToken cancellationToken)
        {
          
            if (IsCertificateAlreadyFinalized(
                    envelope))
            {
                return;
            }

            /*
             * Certificate yalnız bütün signed documents
             * finalized olduqdan sonra yaradıla bilər.
             */
            if (
                documents.Any(x =>
                    string.IsNullOrWhiteSpace(
                        x.FinalBlobPath) ||
                    string.IsNullOrWhiteSpace(
                        x.FinalHash) ||
                    !x.FinalizedAt.HasValue))
            {
                throw new InvalidOperationException(
                    "Completion certificate cannot be generated before all documents are finalized.");
            }

            var certificateData =
                await _certificateDataBuilder.BuildAsync(
                    envelope,
                    documents,
                    cancellationToken);

            var certificateBytes =
                await _certificateGenerator.GenerateAsync(
                    certificateData,
                    cancellationToken);

            if (certificateBytes.Length == 0)
            {
                throw new InvalidOperationException(
                    "Completion certificate generation returned an empty PDF.");
            }

            var certificateHash =
                Convert.ToHexString(
                    System.Security.Cryptography
                        .SHA256.HashData(
                            certificateBytes))
                .ToLowerInvariant();

            var previousCertificatePath =
                envelope.CertificateBlobPath;

            string? newCertificatePath =
                null;

            try
            {
                await using var stream =
                    new MemoryStream(
                        certificateBytes,
                        writable: false);

                newCertificatePath =
                    await _fileStorage.UploadAsync(
                        stream,
                        "completion-certificate.pdf",
                        "application/pdf",
                        $"envelopes/{envelope.Id:N}/certificate",
                        cancellationToken);

                var now =
                    DateTimeOffset.UtcNow;

                envelope.CertificateBlobPath =
                    newCertificatePath;

                envelope.CertificateHash =
                    certificateHash;

                envelope.CertificateGeneratedAt =
                    now;

                /*
                 * GetById result-in tracking behaviour-una
                 * dependency yaratmırıq.
                 */
                _envelopeRepository.Update(
                    envelope);

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }
            catch
            {
                /*
                 * Storage upload oldu, DB update fail oldusa
                 * orphan certificate saxlamırıq.
                 */
                if (!string.IsNullOrWhiteSpace(
                        newCertificatePath))
                {
                    try
                    {
                        await _fileStorage.DeleteAsync(
                            newCertificatePath,
                            cancellationToken);
                    }
                    catch
                    {
                        // Cleanup əsas exception-u gizlətməsin.
                    }
                }

                throw;
            }

            /*
             * Partial/stale certificate path varsa
             * yeni state uğurla DB-yə yazıldıqdan sonra sil.
             */
            if (
                !string.IsNullOrWhiteSpace(
                    previousCertificatePath) &&
                !string.Equals(
                    previousCertificatePath,
                    newCertificatePath,
                    StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await _fileStorage.DeleteAsync(
                        previousCertificatePath,
                        cancellationToken);
                }
                catch
                {
                    /*
                     * Yeni certificate artıq DB-də valid-dir.
                     * Köhnə artifact cleanup failure
                     * finalization-u fail etdirməsin.
                     */
                }
            }
        }
        private static bool IsCertificateAlreadyFinalized(
    Envelope envelope)
        {
            return
                !string.IsNullOrWhiteSpace(
                    envelope.CertificateBlobPath) &&

                !string.IsNullOrWhiteSpace(
                    envelope.CertificateHash) &&

                envelope.CertificateGeneratedAt.HasValue;
        }
        private async Task FinalizeDocumentAsync(
            Guid envelopeId,
            Documents document,
            IReadOnlyCollection<Fields> allFields,
            IReadOnlyCollection<EnvelopeFieldValue> allValues,
            IReadOnlyCollection<EnvelopeAppliedSignature> allAppliedMarks,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(
                    document.OriginalBlobPath))
            {
                throw new InvalidOperationException(
                    $"Original storage path is missing for document '{document.Id}'.");
            }

            var documentFields =
                allFields
                    .Where(x =>
                        x.EnvelopeDocumentId ==
                        document.Id)
                    .ToList();

            var documentFieldIds =
                documentFields
                    .Select(x => x.Id)
                    .ToHashSet();

            var documentValues =
                allValues
                    .Where(x =>
                        documentFieldIds.Contains(
                            x.EnvelopeFieldId))
                    .ToList();

            var documentMarks =
                allAppliedMarks
                    .Where(x =>
                        documentFieldIds.Contains(
                            x.EnvelopeFieldId))
                    .ToList();

            var renderFields =
                FinalPdfRenderFieldMapper.Map(
                    documentFields,
                    documentValues,
                    documentMarks);

            
            await using var sourceStream =
                await _fileStorage.OpenReadAsync(
                    document.OriginalBlobPath,
                    cancellationToken);

            using var originalPdf =
                new MemoryStream();

            await sourceStream.CopyToAsync(
                originalPdf,
                cancellationToken);

            if (originalPdf.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Original PDF is empty for document '{document.Id}'.");
            }

            originalPdf.Position = 0;

            
            var finalPdfBytes =
                await _finalPdfRenderer.RenderAsync(
                    originalPdf,
                    renderFields,
                    cancellationToken);

            if (finalPdfBytes.Length == 0)
            {
                throw new InvalidOperationException(
                    $"Final PDF rendering returned an empty document for '{document.Id}'.");
            }

            var finalHash =
                Convert.ToHexString(
                    SHA256.HashData(
                        finalPdfBytes))
                .ToLowerInvariant();

            var previousFinalBlobPath =
                document.FinalBlobPath;

            string? newFinalBlobPath =
                null;

            try
            {
                await using var finalPdfStream =
                    new MemoryStream(
                        finalPdfBytes,
                        writable: false);

                /*
                 * Storage provider physical path-i özü idarə edir.
                 *
                 * Local:
                 * storage/...
                 *
                 * Azure:
                 * private Blob container.
                 */
                newFinalBlobPath =
                    await _fileStorage.UploadAsync(
                        finalPdfStream,
                        "final.pdf",
                        "application/pdf",
                        $"envelopes/{envelopeId:N}/documents/{document.Id:N}/final",
                        cancellationToken);

                /*
                 * Repository FindAsync AsNoTracking olduğu üçün
                 * Update mütləqdir.
                 */
                document.FinalBlobPath =
                    newFinalBlobPath;

                document.FinalHash =
                    finalHash;

                document.FinalizedAt =
                    DateTimeOffset.UtcNow;

                _documentRepository.Update(
                    document);

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }
            catch
            {
                /*
                 * Blob upload oldu, amma DB save fail oldusa
                 * orphan final PDF saxlamırıq.
                 */
                if (!string.IsNullOrWhiteSpace(
                        newFinalBlobPath))
                {
                    try
                    {
                        await _fileStorage.DeleteAsync(
                            newFinalBlobPath,
                            cancellationToken);
                    }
                    catch
                    {
                        // Cleanup error əsas error-u gizlətməsin.
                    }
                }

                throw;
            }

            /*
             * Əgər əvvəlki incomplete/stale final artifact
             * mövcud idisə, yeni DB state uğurla save
             * olunandan SONRA köhnəni silirik.
             */
            if (
                !string.IsNullOrWhiteSpace(
                    previousFinalBlobPath) &&
                !string.Equals(
                    previousFinalBlobPath,
                    newFinalBlobPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await _fileStorage.DeleteAsync(
                        previousFinalBlobPath,
                        cancellationToken);
                }
                catch
                {
                    
                }
            }
        }

        private static bool IsAlreadyFinalized(
            Documents document)
        {
            return
                !string.IsNullOrWhiteSpace(
                    document.FinalBlobPath) &&

                !string.IsNullOrWhiteSpace(
                    document.FinalHash) &&

                document.FinalizedAt.HasValue;
        }
    }
}
