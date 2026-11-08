using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Application.Common.Mapping;
using LV.SignFlow.Application.Common.Models;
using LV.SignFlow.Application.Envelopes;
using LV.SignFlow.Application.Envelopes.Requests;
using LV.SignFlow.Application.Signing.Dtos;
using LV.SignFlow.Application.Signing.Requests;
using LV.SignFlow.Domain.Entities.Envelopes;
using LV.SignFlow.Domain.Entities.Signing;
using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using LV.SignFlow.Domain.Enums.SigningEnums;
using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace LV.SignFlow.Application.Signing
{
    public class SigningService : ISigningService
    {
        public IRepository<Envelope> _envelopeRepository;
        public IRepository<Recipients> _envelopeRecipient;
        public IRepository<Documents> _envelopeDocument;
        public IRepository<Fields> _envelopeField;
        public IRepository<EnvelopeFieldValue> _envelopeFieldValue;
        public IRepository<EnvelopeAppliedSignature> _envelopeAppliedSignature;
        public IRepository<Signature> _signatureRepository;
        public IRepository<Stamps> _stampRepository;
        public ICurrentUser _currentUser;
        public IUnitOfWork _unitOfWork;
        private readonly ISignatureAssetStorage _signatureAssetStorage;
        private readonly IEnvelopeService _envelopeService;
        private readonly IFinalPdfRenderer _finalPdfRenderer;
        private readonly IFileStorage _fileStorage;
        

        public SigningService(
        IRepository<Envelope> envelopeRepository, IRepository<Recipients> envelopeRecipient, IRepository<Documents> envelopeDocument, ICurrentUser currentUser, IRepository<Fields> envelopeField, IRepository<EnvelopeFieldValue> envelopeFieldValue, IRepository<EnvelopeAppliedSignature> envelopeAppliedSignature, IUnitOfWork unitOfWork, IRepository<Signature> signatureRepository, IRepository<Stamps> stampRepository, ISignatureAssetStorage signatureAssetStorage, IEnvelopeService envelopeService, IFinalPdfRenderer finalPdfRenderer, IFileStorage fileStorage)
        {
            _envelopeRepository = envelopeRepository;
            _envelopeRecipient = envelopeRecipient;
            _envelopeDocument = envelopeDocument;
            _currentUser = currentUser;
            _envelopeField = envelopeField;
            _envelopeFieldValue = envelopeFieldValue;
            _envelopeAppliedSignature = envelopeAppliedSignature;
            _unitOfWork = unitOfWork;
            _signatureRepository = signatureRepository;
            _stampRepository = stampRepository;
            _signatureAssetStorage = signatureAssetStorage;
            _envelopeService = envelopeService;
            _finalPdfRenderer = finalPdfRenderer;
            _fileStorage = fileStorage;
        }
        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthentificated) throw new UnauthorizedAccessException("Authentification is required");
        }

        private async Task<Recipients> GetActiveSigningRecipientAsync(
    Envelope envelope,
    CancellationToken cancellationToken)
        {
            var recipients =
                await _envelopeRecipient.FindAsync(
                    x =>
                        x.EnvelopeId == envelope.Id &&
                        x.UserId == _currentUser.UserId,
                    cancellationToken);

            if (recipients.Count == 0)
            {
                throw new UnauthorizedAccessException(
                    "You are not a recipient of this envelope.");
            }

            var recipient =
                recipients
                    .Where(x =>
                        (
                            x.Status == RecipientStatus.Active ||
                            x.Status == RecipientStatus.Viewed
                        ) &&
                        envelope.CurrentRoutingOrder.HasValue &&
                        x.RoutingOrder ==
                            envelope.CurrentRoutingOrder.Value)
                    .OrderBy(x => x.RoutingOrder)
                    .FirstOrDefault();

            if (recipient is null)
            {
                throw new InvalidOperationException(
                    "It is not this recipient's turn to act yet.");
            }

            return recipient;
        }
        public async Task<SigningSessionDto> GetSigningSessionAsync(
        Guid envelopeId, CancellationToken cancellationToken =
      default)
        {
            EnsureAuthenticated();
            if (envelopeId == Guid.Empty)
            {
                throw new ArgumentException("Envelope id is required.");
            }

            var envelope = await _envelopeRepository.GetByIdAsync(envelopeId, cancellationToken);

            if (envelope is null || envelope.IsDeleted || envelope.OrganizationId != _currentUser.OrganizationId)
            {
                throw new KeyNotFoundException("Envelope was not found.");
            }
            var isTerminalEnvelope =
         envelope.Status == EnvelopeStatus.Completed ||
         envelope.Status == EnvelopeStatus.Declined ||
         envelope.Status == EnvelopeStatus.Voided ||
         envelope.Status == EnvelopeStatus.Expired;

            if (!isTerminalEnvelope &&
                envelope.Status != EnvelopeStatus.Sent &&
                envelope.Status != EnvelopeStatus.InProgress)
            {
                throw new InvalidOperationException(
                    "This envelope is not available for signing.");
            }
            if (!isTerminalEnvelope &&
     envelope.ExpiresAt.HasValue &&
     envelope.ExpiresAt.Value < DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException(
                    "This envelope has expired.");
            }
            var userRecipients =
      await _envelopeRecipient.FindAsync(
          x =>
              x.EnvelopeId == envelopeId &&
              x.UserId == _currentUser.UserId,
          cancellationToken);

            if (userRecipients.Count == 0)
            {
                throw new UnauthorizedAccessException(
                    "You are not a recipient of this envelope.");
            }

            Recipients? recipient = null;

            if (!isTerminalEnvelope)
            {
                /*
                 * 1. Hazırda sign edə bilən role.
                 */
                recipient =
                    userRecipients
                        .Where(x =>
                            x.Status == RecipientStatus.Active ||
                            x.Status == RecipientStatus.Viewed)
                        .OrderBy(x => x.RoutingOrder)
                        .FirstOrDefault();

                /*
                 * 2. Active yoxdur:
                 * həmin user-in gələcək role-u varsa onu seç.
                 * Frontend "not your turn yet" göstərəcək.
                 */
                recipient ??=
                    userRecipients
                        .Where(x =>
                            x.Status == RecipientStatus.Created ||
                            x.Status == RecipientStatus.Pending ||
                            x.Status == RecipientStatus.WaitingForCondition)
                        .OrderBy(x => x.RoutingOrder)
                        .FirstOrDefault();
            }

            /*
             * 3. Envelope terminaldır və ya user-in artıq
             * başqa action-u yoxdur.
             */
            recipient ??=
                userRecipients
                    .Where(x =>
                        x.Status == RecipientStatus.Completed ||
                        x.Status == RecipientStatus.Declined ||
                        x.Status == RecipientStatus.Canceled ||
                        x.Status == RecipientStatus.Skipped)
                    .OrderByDescending(x => x.RoutingOrder)
                    .FirstOrDefault();

            recipient ??=
                userRecipients
                    .OrderBy(x => x.RoutingOrder)
                    .FirstOrDefault();

            if (recipient is null)
            {
                throw new UnauthorizedAccessException(
                    "You are not a recipient of this envelope.");
            }

            var isTerminalRecipient =
    recipient.Status == RecipientStatus.Completed ||
    recipient.Status == RecipientStatus.Declined ||
    recipient.Status == RecipientStatus.Canceled ||
    recipient.Status == RecipientStatus.Skipped;
            
            
            //if (recipient.Status == RecipientStatus.WaitingForCondition)
            //{
            //    throw new InvalidOperationException("This recipient is waiting for a routing condition.");
            //}

            var documents = await _envelopeDocument.FindAsync(
            x => x.EnvelopeId == envelopeId, cancellationToken);

            var fields = await _envelopeField.FindAsync(
            x => x.EnvelopeId == envelopeId && x.OwnerType == FieldOwnerType.Recipient && x.EnvelopeRecipientId == recipient.Id, cancellationToken);
            var fieldIds = fields.Select(x => x.Id).ToList();
            var fieldValues = fields.Count == 0 ? new List<EnvelopeFieldValue>() : (
            await _envelopeFieldValue.FindAsync(
            x => fieldIds.Contains(x.EnvelopeFieldId), cancellationToken)).ToList();

            var appliedSignatures = fieldIds.Count == 0 ? new List<EnvelopeAppliedSignature>() : (
            await _envelopeAppliedSignature.FindAsync(
            x => x.EnvelopeId == envelopeId && x.RecipientId == recipient.Id && fieldIds.Contains(x.EnvelopeFieldId), cancellationToken)).ToList();

            var fieldValuesByFieldId = fieldValues.ToDictionary(x => x.EnvelopeFieldId);

            var signaturesByFieldId = appliedSignatures.ToDictionary(x => x.EnvelopeFieldId);

            var savedSignatureEntities =
    await _signatureRepository.FindAsync(
        x =>
            x.UserId == _currentUser.UserId &&
            x.IsActive,
        cancellationToken);

            var savedSignatures =
                savedSignatureEntities
                    .Where(x =>
                        x.Kind == SignatureKind.Signature)
                    .Select(x =>
                        new SigningSavedSignatureDto
                        {
                            Id = x.Id,

                            Label = "Signature",

                            ImageUrl =
                                $"/api/signing/signatures/{x.Id}/content"
                        })
                    .ToList();

            var savedInitials =
                savedSignatureEntities
                    .Where(x =>
                        x.Kind == SignatureKind.Initials)
                    .Select(x =>
                        new SigningSavedSignatureDto
                        {
                            Id = x.Id,

                            Label = "Initials",

                            ImageUrl =
                                $"/api/signing/signatures/{x.Id}/content"
                        })
                    .ToList();

            var stampEntities =
    await _stampRepository.FindAsync(
        x => x.IsActive,
        cancellationToken);

            var stamps =
                stampEntities
                    .Select(x =>
                        new SigningStampDto
                        {
                            Id = x.Id,
                            Name = x.Name,

                            ImageUrl =
                                $"/api/signing/stamps/{x.Id}/content"
                        })
                    .ToList();

            return new SigningSessionDto
            {
                EnvelopeId = envelope.Id,
                Subject = envelope.Subject,
                Message = envelope.Message,
                EnvelopeStatus = envelope.Status,
                ExpiresAt = envelope.ExpiresAt,
                Recipient = new SigningRecipientDto
                {
                    Id = recipient.Id,
                    Name = recipient.Name,
                    Email = recipient.Email,
                    Status = recipient.Status,
                    RecipientType = recipient.RecipientType,
                    RoutingOrder = recipient.RoutingOrder
                },
                SavedInitials=savedInitials,
                Stamps=stamps,
                SavedSignatures=savedSignatures,

                Documents = documents.OrderBy(x => x.Order).Select(
              x => new SigningDocumentDto
              {
                  Id = x.Id,
                  FileName = x.OriginalFileName,
                  Order = x.Order,
                  PageCount = x.PageCount}).ToList(),

                Fields = fields.OrderBy(x => x.PageNumber).Select(
              x => {
                  fieldValuesByFieldId.TryGetValue(x.Id, out var fieldValue);

                  signaturesByFieldId.TryGetValue(x.Id, out var appliedSignature);

                  return new SigningFieldDto
                  {
                      Id = x.Id,
                      DocumentId = x.EnvelopeDocumentId,
                      FieldType = x.FieldType,
                      Label = x.Label,
                      PageNumber = x.PageNumber,
                      X = x.X,
                      Y = x.Y,
                      Width = x.Width,
                      Height = x.Height,
                      IsRequired = x.IsRequired,
                      Options = DeserializeOptions(x.OptionJson),
                      FieldValue = fieldValue is null ? null : new SigningFieldValueDto
                      {
                          ValueText = fieldValue.ValueText,
                          ValueNumber = fieldValue.ValueNumber,
                          ValueDate = fieldValue.ValueDate,
                          ValueBoolean = fieldValue.ValueBoolean,
                          FilledAt = fieldValue.FilledAt
                      },
                      AppliedSignature = appliedSignature is null ? null : new SigningAppliedSignatureDto
                      {
                          Id = appliedSignature.Id,
                          Kind = appliedSignature.Kind,
                          AppliedAt = appliedSignature.AppliedAt,
                          SourceSignatureId = appliedSignature.SourceSignatureId,
                          SourceStampId=appliedSignature.SourceStampId,
                          SignatureTypeSnapshot =
    appliedSignature.SignatureTypeSnapshot,

                          TypedTextSnapshot =
    appliedSignature.TypedTextSnapshot,

                          FontKeySnapshot =
    appliedSignature.FontKeySnapshot,

                          AppliedContentType =
    appliedSignature.AppliedContentType,
                      }
                  };
              }
              ).ToList()
            };
        }

        private static List<string>? DeserializeOptions(string? optionsJson)
        {
            if (string.IsNullOrWhiteSpace(optionsJson))
            {
                return null;
            }

            return JsonSerializer.Deserialize<List<string>>(optionsJson);
        }

        public async Task<SigningFieldValueDto> SaveFieldValueAsync(Guid envelopeId, Guid fieldId, UpdateSigningFieldValueRequest request, CancellationToken cancellationToken =
      default)
        {
            EnsureAuthenticated();
            if (envelopeId == Guid.Empty)
            {
                throw new ArgumentException("Envelope id is required.");
            }

            var envelope = await _envelopeRepository.GetByIdAsync(envelopeId, cancellationToken);

            if (envelope is null || envelope.IsDeleted || envelope.OrganizationId != _currentUser.OrganizationId)
            {
                throw new KeyNotFoundException("Envelope was not found.");
            }
            EnsureEnvelopeAvailableForSigning(
    envelope);

            var recipients = await _envelopeRecipient.FindAsync(
            x => x.EnvelopeId == envelopeId && x.UserId == _currentUser.UserId, cancellationToken);

            var recipient =
     await GetActiveSigningRecipientAsync(
         envelope,
         cancellationToken);

            if (recipient is null)
            {
                throw new UnauthorizedAccessException("You are not a recipient of this envelope.");
            }
            if (recipient.Status == RecipientStatus.Completed || recipient.Status == RecipientStatus.Declined || recipient.Status == RecipientStatus.Skipped)
            {
                throw new InvalidOperationException("This recipient can no longer sign this envelope.");
            }
            if (!envelope.CurrentRoutingOrder.HasValue || recipient.RoutingOrder != envelope.CurrentRoutingOrder.Value)
            {
                throw new InvalidOperationException("It is not this recipient's turn to act yet.");
            }

            var fields = await _envelopeField.FindAsync(
            x => x.Id == fieldId && x.EnvelopeId == envelopeId, cancellationToken);
            var field = fields.FirstOrDefault();
            if (field is null ||
    field.EnvelopeId != envelopeId ||
    field.EnvelopeRecipientId != recipient.Id)
            {
                throw new KeyNotFoundException(
                    "Signing field was not found.");
            }

            if (field.OwnerType != FieldOwnerType.Recipient || field.EnvelopeRecipientId != recipient.Id)
            {
                throw new UnauthorizedAccessException("This field is not assigned to you.");
            }

            if (field.FieldType == FieldType.Signature || field.FieldType == FieldType.Initial || field.FieldType == FieldType.Stamp)
            {
                throw new InvalidOperationException("Signature fields must be completed through the signature endpoint.");
            }
            var existingValues = await _envelopeFieldValue.FindAsync(
            x => x.EnvelopeFieldId == field.Id, cancellationToken);

            var fieldValue = existingValues.FirstOrDefault();

            var isNew = fieldValue is null;

            if (isNew)
            {
                fieldValue = new EnvelopeFieldValue
                {
                    Id = Guid.NewGuid(),

                    EnvelopeFieldId = field.Id
                };
            }

            if (fieldValue is null)
            {
                fieldValue = new EnvelopeFieldValue
                {
                    Id = Guid.NewGuid(),

                    EnvelopeFieldId = field.Id,

                    SourceType = FieldOwnerType.Recipient,

                    FilledByRecipientId = recipient.Id,

                    FilledByUserId = _currentUser.UserId
                };

                await _envelopeFieldValue.AddAsync(
                fieldValue, cancellationToken);
            }

            fieldValue.ValueText = null;
            fieldValue.ValueNumber = null;
            fieldValue.ValueDate = null;
            fieldValue.ValueBoolean = null;

            switch (field.FieldType)
            {
                case FieldType.Text:
                case FieldType.Fullname:
                case FieldType.Email:
                case FieldType.RadioButton:
                case FieldType.Dropdown:
                    fieldValue.ValueText = request.ValueText?.Trim();
                    break;

                case FieldType.Number:
                    fieldValue.ValueNumber = request.ValueNumber;
                    break;

                case FieldType.DateSigned:
                    fieldValue.ValueDate = request.ValueDate;
                    break;

                case FieldType.Checkbox:
                    fieldValue.ValueBoolean = request.ValueBoolean;
                    break;

                default:
                    throw new InvalidOperationException("This field type cannot store a normal field value.");
            }
            fieldValue.SourceType = FieldOwnerType.Recipient;

            fieldValue.FilledByRecipientId = recipient.Id;

            fieldValue.FilledByUserId = _currentUser.UserId;

            fieldValue.FilledAt = DateTimeOffset.UtcNow;

            if (isNew)
            {
                await _envelopeFieldValue.AddAsync(
                fieldValue, cancellationToken);
            }
            else
            {
                _envelopeFieldValue.Update(
                fieldValue);
            }

            await _unitOfWork.SaveChangesAsync(
            cancellationToken);

            return new SigningFieldValueDto
            {
                ValueText = fieldValue.ValueText,

                ValueNumber = fieldValue.ValueNumber,

                ValueDate = fieldValue.ValueDate,

                ValueBoolean = fieldValue.ValueBoolean,

                FilledAt = fieldValue.FilledAt
            };

        }
        private async Task<string> CopySignatureAppliedMarkToStorageAsync(
    string signatureBlobPath,
    Guid envelopeId,
    CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(signatureBlobPath))
            {
                throw new InvalidOperationException(
                    "Signature asset path is missing.");
            }

            var (content, contentType) =
                await _signatureAssetStorage.ReadAsync(
                    signatureBlobPath,
                    cancellationToken);

            var extension =
                Path.GetExtension(signatureBlobPath);

            if (string.IsNullOrWhiteSpace(extension))
            {
                extension =
                    contentType switch
                    {
                        "image/svg+xml" => ".svg",
                        "image/png" => ".png",
                        _ => ".bin"
                    };
            }

            await using var stream =
                new MemoryStream(
                    content,
                    writable: false);

            return await _fileStorage.UploadAsync(
                stream,
                $"signature{extension}",
                contentType,
                $"envelopes/{envelopeId}/applied-marks",
                cancellationToken);
        }

        private async Task<string> CopyStampAppliedMarkToStorageAsync(
    string stampBlobPath,
    Guid envelopeId,
    CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(stampBlobPath))
            {
                throw new InvalidOperationException(
                    "Stamp asset path is missing.");
            }

            await using var sourceStream =
                await _fileStorage.OpenReadAsync(
                    stampBlobPath,
                    cancellationToken);

            var extension =
                Path.GetExtension(stampBlobPath)
                    .ToLowerInvariant();

            var contentType =
                extension switch
                {
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    ".svg" => "image/svg+xml",
                    _ => "application/octet-stream"
                };

            return await _fileStorage.UploadAsync(
                sourceStream,
                $"stamp{extension}",
                contentType,
                $"envelopes/{envelopeId}/applied-marks",
                cancellationToken);
        }

        public async Task<SigningAppliedSignatureDto> ApplyMarkAsync(
    Guid envelopeId,
    Guid fieldId,
    ApplySigningMarkRequest request,
    CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            if (envelopeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Envelope id is required.");
            }

            if (fieldId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Field id is required.");
            }

            var envelope =
                await _envelopeRepository.GetByIdAsync(
                    envelopeId,
                    cancellationToken);

            if (
                envelope is null ||
                envelope.IsDeleted ||
                envelope.OrganizationId != _currentUser.OrganizationId)
            {
                throw new KeyNotFoundException(
                    "Envelope was not found.");
            }

            EnsureEnvelopeAvailableForSigning(
     envelope);

            /*
             * Yalnız hazırda active olan və
             * current routing order-də olan recipient
             * mark tətbiq edə bilər.
             */
            var recipient =
                await GetActiveSigningRecipientAsync(
                    envelope,
                    cancellationToken);

            var fields =
                await _envelopeField.FindAsync(
                    x =>
                        x.Id == fieldId &&
                        x.EnvelopeId == envelopeId,
                    cancellationToken);

            var field =
                fields.FirstOrDefault();

            if (field is null)
            {
                throw new KeyNotFoundException(
                    "Signing field was not found.");
            }

            if (
                field.OwnerType != FieldOwnerType.Recipient ||
                field.EnvelopeRecipientId != recipient.Id)
            {
                throw new UnauthorizedAccessException(
                    "This field is not assigned to you.");
            }

            /*
             * FieldType ilə request.Kind bir-birinə
             * uyğun olmalıdır.
             */
            var expectedKind =
                field.FieldType switch
                {
                    FieldType.Signature =>
                        AppliedMarkKind.Signature,

                    FieldType.Initial =>
                        AppliedMarkKind.Initial,

                    FieldType.Stamp =>
                        AppliedMarkKind.Stamp,

                    _ =>
                        throw new InvalidOperationException(
                            "This field is not a signature field.")
                };

            if (request.Kind != expectedKind)
            {
                throw new InvalidOperationException(
                    "The applied mark type does not match the field type.");
            }

            /*
             * Source və immutable snapshot metadata.
             */
            string sourceBlobPath;

            Guid? sourceSignatureId = null;
            Guid? sourceStampId = null;

            SignatureType? signatureTypeSnapshot = null;

            string? typedTextSnapshot = null;
            string? fontKeySnapshot = null;

            /*
             * Signature / Initial
             */
            if (
                request.Kind == AppliedMarkKind.Signature ||
                request.Kind == AppliedMarkKind.Initial)
            {
                if (!request.SourceSignatureId.HasValue)
                {
                    throw new ArgumentException(
                        "Source signature is required.");
                }

                var signature =
                    await _signatureRepository.GetByIdAsync(
                        request.SourceSignatureId.Value,
                        cancellationToken);

                if (
                    signature is null ||
                    !signature.IsActive ||
                    signature.UserId != _currentUser.UserId)
                {
                    throw new UnauthorizedAccessException(
                        "Signature was not found or does not belong to you.");
                }

                /*
                 * Signature field-ə Signature,
                 * Initial field-ə Initials source istifadə edilməlidir.
                 */
                if (
                    request.Kind == AppliedMarkKind.Signature &&
                    signature.Kind != SignatureKind.Signature)
                {
                    throw new InvalidOperationException(
                        "A signature field requires a signature source.");
                }

                if (
                    request.Kind == AppliedMarkKind.Initial &&
                    signature.Kind != SignatureKind.Initials)
                {
                    throw new InvalidOperationException(
                        "An initials field requires an initials source.");
                }

                sourceSignatureId =
                    signature.Id;

                sourceBlobPath =
                    signature.BlobPath;

                signatureTypeSnapshot =
                    signature.Type;

                /*
                 * Typed signature üçün text/font metadata-nı
                 * həmin anda snapshot edirik.
                 */
                if (signature.Type == SignatureType.Typed)
                {
                    if (string.IsNullOrWhiteSpace(
                            signature.TypedText))
                    {
                        throw new InvalidOperationException(
                            "Typed signature metadata is missing.");
                    }

                    typedTextSnapshot =
                        signature.TypedText.Trim();

                    fontKeySnapshot =
                        NormalizeSignatureFontKey(
                            signature.FontKey);
                }
            }

            /*
             * Stamp
             */
            else
            {
                if (!request.SourceStampId.HasValue)
                {
                    throw new ArgumentException(
                        "Source stamp is required.");
                }

                var stamp =
                    await _stampRepository.GetByIdAsync(
                        request.SourceStampId.Value,
                        cancellationToken);

                if (
                    stamp is null ||
                    !stamp.IsActive)
                {
                    throw new KeyNotFoundException(
                        "Stamp was not found.");
                }

                sourceStampId =
                    stamp.Id;

                sourceBlobPath =
                    stamp.BlobPath;
            }

            /*
             * Source asset-in immutable envelope copy-si:
             *
             * - ayrıca BlobPath
             * - ContentType
             * - SHA-256 hash
             */
            var snapshot =
                await SnapshotAppliedMarkAsync(
                    sourceBlobPath,
                    envelopeId,
                    cancellationToken);

            /*
             * Field-də əvvəl mark varsa replace edirik.
             * Repository FindAsync AsNoTracking olduğu üçün
             * mövcud entity sonda Update edilməlidir.
             */
            var existingMarks =
                await _envelopeAppliedSignature.FindAsync(
                    x =>
                        x.EnvelopeFieldId == field.Id &&
                        x.EnvelopeId == envelopeId,
                    cancellationToken);

            var appliedMark =
                existingMarks.FirstOrDefault();

            var isNew =
                appliedMark is null;

            var oldAppliedBlobPath =
                appliedMark?.AppliedBlobPath;

            if (isNew)
            {
                appliedMark =
                    new EnvelopeAppliedSignature
                    {
                        Id =
                            Guid.NewGuid(),

                        EnvelopeId =
                            envelope.Id,

                        EnvelopeFieldId =
                            field.Id,

                        RecipientId =
                            recipient.Id
                    };
            }
            else if (appliedMark!.RecipientId != recipient.Id)
            {
                /*
                 * Normalda bu hal baş verməməlidir.
                 * Evidence başqa recipient-ə aid olsa
                 * onu overwrite etmirik.
                 */
                try
                {
                    await _fileStorage.DeleteAsync(
                        snapshot.BlobPath,
                        cancellationToken);
                }
                catch
                {
                    // Əsas validation error-u gizlətmirik.
                }

                throw new InvalidOperationException(
                    "The existing applied mark belongs to another recipient.");
            }

            var now =
                DateTimeOffset.UtcNow;

            /*
             * Evidence metadata.
             */
            appliedMark!.AppliedByUserId =
                _currentUser.UserId;

            appliedMark.Kind =
                request.Kind;

            appliedMark.SourceSignatureId =
                sourceSignatureId;

            appliedMark.SourceStampId =
                sourceStampId;

            appliedMark.SignatureTypeSnapshot =
                signatureTypeSnapshot;

            appliedMark.TypedTextSnapshot =
                typedTextSnapshot;

            appliedMark.FontKeySnapshot =
                fontKeySnapshot;

            appliedMark.AppliedBlobPath =
                snapshot.BlobPath;

            appliedMark.AppliedContentType =
                snapshot.ContentType;

            appliedMark.AppliedHash =
                snapshot.Hash;

            appliedMark.AppliedAt =
                now;

            try
            {
                if (isNew)
                {
                    await _envelopeAppliedSignature.AddAsync(
                        appliedMark,
                        cancellationToken);
                }
                else
                {
                    _envelopeAppliedSignature.Update(
                        appliedMark);
                }

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }
            catch
            {
                /*
                 * Snapshot storage-a upload olunub,
                 * amma DB save fail olubsa orphan
                 * file saxlamırıq.
                 */
                try
                {
                    await _fileStorage.DeleteAsync(
                        snapshot.BlobPath,
                        cancellationToken);
                }
                catch
                {
                    // Cleanup error əsas exception-u gizlətməsin.
                }

                throw;
            }

            /*
             * Replace uğurla DB-yə yazıldıqdan sonra
             * əvvəlki immutable copy-ni silirik.
             *
             * Saved Signature/Stamp source-a toxunmuruq.
             */
            if (
                !isNew &&
                !string.IsNullOrWhiteSpace(
                    oldAppliedBlobPath) &&
                !string.Equals(
                    oldAppliedBlobPath,
                    snapshot.BlobPath,
                    StringComparison.OrdinalIgnoreCase))
            {
                var normalizedOldPath =
                    oldAppliedBlobPath
                        .Replace('\\', '/')
                        .TrimStart('/');

                var appliedMarksPrefix =
                    $"envelopes/{envelopeId:N}/applied-marks/";

                if (normalizedOldPath.StartsWith(
                        appliedMarksPrefix,
                        StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        await _fileStorage.DeleteAsync(
                            oldAppliedBlobPath,
                            cancellationToken);
                    }
                    catch
                    {
                        /*
                         * DB artıq yeni evidence-a keçirilib.
                         * Köhnə blob cleanup problemi
                         * signing əməliyyatını fail etməsin.
                         */
                    }
                }
            }

            return new SigningAppliedSignatureDto
            {
                Id =
                    appliedMark.Id,

                Kind =
                    appliedMark.Kind,

                AppliedAt =
                    appliedMark.AppliedAt,

                SourceSignatureId =
                    appliedMark.SourceSignatureId,

                SourceStampId =
                    appliedMark.SourceStampId,
                SignatureTypeSnapshot =
    appliedMark.SignatureTypeSnapshot,

                TypedTextSnapshot =
    appliedMark.TypedTextSnapshot,

                FontKeySnapshot =
    appliedMark.FontKeySnapshot,

                AppliedContentType =
    appliedMark.AppliedContentType,
            };
        }

        private async Task<string>
    CopyAppliedMarkToStorageAsync(
        string sourceBlobPath,
        Guid envelopeId,
        CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceBlobPath))
            {
                throw new InvalidOperationException(
                    "Source mark path is missing.");
            }

            await using var sourceStream =
                await _fileStorage.OpenReadAsync(
                    sourceBlobPath,
                    cancellationToken);

            var extension =
                Path.GetExtension(
                    sourceBlobPath)
                .ToLowerInvariant();

            var contentType =
                extension switch
                {
                    ".svg" => "image/svg+xml",
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",
                    _ => "application/octet-stream"
                };

            return await _fileStorage.UploadAsync(
                sourceStream,
                $"applied-mark{extension}",
                contentType,
                $"envelopes/{envelopeId:N}/applied-marks",
                cancellationToken);
        }
        private static string NormalizeSignatureFontKey(
    string? fontKey)
        {
            return fontKey?
                .Trim()
                .ToLowerInvariant()
                switch
            {
                "great-vibes" =>
                    "great-vibes",

                "mrs-saint-delafield" =>
                    "mrs-saint-delafield",

                "caveat" =>
                    "caveat",

                _ =>
                    "great-vibes"
            };
        }
        public async Task<CreatedSigningSignatureDto> CreateSignatureAsync(
    CreateSigningSignatureRequest request,
    CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            if (
                request.Kind != SignatureKind.Signature &&
                request.Kind != SignatureKind.Initials)
            {
                throw new ArgumentException(
                    "Invalid signature kind.");
            }

            if (
                request.Type != SignatureType.Typed &&
                request.Type != SignatureType.Drawn)
            {
                throw new ArgumentException(
                    "Only typed and drawn signatures can be created here.");
            }

            string blobPath;

            string? typedText = null;
            string? fontKey = null;

            if (request.Type == SignatureType.Typed)
            {
                if (string.IsNullOrWhiteSpace(
                        request.TypedText))
                {
                    throw new ArgumentException(
                        "Typed signature text is required.");
                }

                typedText =
                    request.TypedText.Trim();

                if (typedText.Length > 250)
                {
                    throw new ArgumentException(
                        "Typed signature text is too long.");
                }

                fontKey =
                    NormalizeSignatureFontKey(
                        request.FontKey);

                blobPath =
                    await _signatureAssetStorage.SaveTypedAsync(
                        _currentUser.UserId,
                        typedText,
                        fontKey,
                        cancellationToken);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(
                        request.ImageDataUrl))
                {
                    throw new ArgumentException(
                        "Drawn signature image is required.");
                }

                blobPath =
                    await _signatureAssetStorage.SaveDrawnAsync(
                        _currentUser.UserId,
                        request.ImageDataUrl,
                        cancellationToken);
            }

            var now =
                DateTimeOffset.UtcNow;

            var signature =
                new Signature
                {
                    Id =
                        Guid.NewGuid(),

                    UserId =
                        _currentUser.UserId,

                    Kind =
                        request.Kind,

                    Type =
                        request.Type,

                    BlobPath =
                        blobPath,

                    TypedText =
                        typedText,

                    FontKey =
                        fontKey,

                    IsDefault =
                        false,

                    IsActive =
                        true,

                    CreatedAt =
                        now,

                    UpdatedAt =
                        now
                };

            await _signatureRepository.AddAsync(
                signature,
                cancellationToken);

            try
            {
                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);
            }
            catch
            {
                try
                {
                    await _fileStorage.DeleteAsync(
                        blobPath,
                        cancellationToken);
                }
                catch
                {
                }

                throw;
            }

            return new CreatedSigningSignatureDto
            {
                Id =
                    signature.Id,

                Kind =
                    signature.Kind,

                Type =
                    signature.Type,

                ContentUrl =
                    $"/api/signing/signatures/{signature.Id}/content",

                CreatedAt =
                    signature.CreatedAt
            };
        }
        private sealed record AppliedMarkStorageResult(
    string BlobPath,
    string ContentType,
    string Hash);
        private async Task<AppliedMarkStorageResult>
    SnapshotAppliedMarkAsync(
        string sourceBlobPath,
        Guid envelopeId,
        CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(
                    sourceBlobPath))
            {
                throw new InvalidOperationException(
                    "Source mark path is missing.");
            }

            await using var sourceStream =
                await _fileStorage.OpenReadAsync(
                    sourceBlobPath,
                    cancellationToken);

            using var memory =
                new MemoryStream();

            await sourceStream.CopyToAsync(
                memory,
                cancellationToken);

            var bytes =
                memory.ToArray();

            if (bytes.Length == 0)
            {
                throw new InvalidOperationException(
                    "Source mark asset is empty.");
            }

            var hash =
                Convert.ToHexString(
                    System.Security.Cryptography
                        .SHA256.HashData(bytes))
                .ToLowerInvariant();

            var extension =
                Path.GetExtension(
                    sourceBlobPath)
                .ToLowerInvariant();

            var contentType =
                extension switch
                {
                    ".svg" => "image/svg+xml",
                    ".png" => "image/png",
                    ".jpg" => "image/jpeg",
                    ".jpeg" => "image/jpeg",

                    _ => throw new InvalidOperationException(
                        $"Unsupported mark asset format '{extension}'.")
                };

            await using var uploadStream =
                new MemoryStream(
                    bytes,
                    writable: false);

            var blobPath =
                await _fileStorage.UploadAsync(
                    uploadStream,
                    $"applied-mark{extension}",
                    contentType,
                    $"envelopes/{envelopeId:N}/applied-marks",
                    cancellationToken);

            return new AppliedMarkStorageResult(
                blobPath,
                contentType,
                hash);
        }
        public async Task<(byte[] Content, string ContentType)> GetSignatureContentAsync(Guid signatureId, CancellationToken cancellationToken =
default)
        {
            EnsureAuthenticated();

            var signature = await _signatureRepository.GetByIdAsync(
            signatureId, cancellationToken);

            if (signature is null || !signature.IsActive)
            {
                throw new KeyNotFoundException("Signature was not found.");
            }

            if (signature.UserId != _currentUser.UserId)
            {
                throw new UnauthorizedAccessException("You cannot access this signature.");
            }

            return await _signatureAssetStorage.ReadAsync(
            signature.BlobPath, cancellationToken);
        }

        public async Task RemoveMarkAsync(Guid envelopeId,  Guid fieldId, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var envelope =
                await _envelopeRepository.GetByIdAsync(
                    envelopeId,
                    cancellationToken);

            if (
                envelope is null ||
                envelope.IsDeleted ||
                envelope.OrganizationId != _currentUser.OrganizationId)
            {
                throw new KeyNotFoundException(
                    "Envelope was not found.");
            }

            EnsureEnvelopeAvailableForSigning(envelope);

            var recipients =
                await _envelopeRecipient.FindAsync(
                    x =>
                        x.EnvelopeId == envelopeId &&
                        x.UserId == _currentUser.UserId,
                    cancellationToken);

            var recipient =
    await GetActiveSigningRecipientAsync(
        envelope,
        cancellationToken);

            if (recipient is null)
            {
                throw new UnauthorizedAccessException(
                    "You are not a recipient of this envelope.");
            }

            if (
                recipient.Status == RecipientStatus.Completed ||
                recipient.Status == RecipientStatus.Declined ||
                recipient.Status == RecipientStatus.Skipped ||
                recipient.Status == RecipientStatus.WaitingForCondition)
            {
                throw new InvalidOperationException(
                    "This recipient cannot update this envelope.");
            }

            if (
                !envelope.CurrentRoutingOrder.HasValue ||
                recipient.RoutingOrder != envelope.CurrentRoutingOrder.Value)
            {
                throw new InvalidOperationException(
                    "It is not this recipient's turn to act.");
            }

            var fields =
                await _envelopeField.FindAsync(
                    x =>
                        x.Id == fieldId &&
                        x.EnvelopeId == envelopeId,
                    cancellationToken);

            var field =
                fields.FirstOrDefault();

            if (field is null)
            {
                throw new KeyNotFoundException(
                    "Field was not found.");
            }

            if (
                field.OwnerType != FieldOwnerType.Recipient ||
                field.EnvelopeRecipientId != recipient.Id)
            {
                throw new UnauthorizedAccessException(
                    "This field is not assigned to you.");
            }

            if (
                field.FieldType != FieldType.Signature &&
                field.FieldType != FieldType.Initial &&
                field.FieldType != FieldType.Stamp)
            {
                throw new InvalidOperationException(
                    "This field does not contain an applied mark.");
            }

            var marks =
    await _envelopeAppliedSignature.FindAsync(
        x =>
            x.EnvelopeId == envelopeId &&
            x.EnvelopeFieldId == fieldId &&
            x.RecipientId == recipient.Id,
        cancellationToken);

            var mark =
                marks.FirstOrDefault();

            if (mark is null)
            {
                return;
            }

            _envelopeAppliedSignature.Delete(mark);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }


        public async Task<DeclineSigningResultDto>
    DeclineSigningAsync(
        Guid envelopeId,
        string reason,
        CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            if (envelopeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Envelope id is required.");
            }

            if (string.IsNullOrWhiteSpace(reason))
            {
                throw new ArgumentException(
                    "Decline reason is required.");
            }

            reason = reason.Trim();

            if (reason.Length > 1000)
            {
                throw new ArgumentException(
                    "Decline reason cannot exceed 1000 characters.");
            }

            var envelope =
      await _envelopeRepository.GetByIdAsync(
          envelopeId,
          cancellationToken);

            if (envelope is null ||
                envelope.IsDeleted ||
                envelope.OrganizationId != _currentUser.OrganizationId)
            {
                throw new KeyNotFoundException(
                    "Envelope was not found.");
            }

            var recipient =
                await GetActiveSigningRecipientAsync(
                    envelope,
                    cancellationToken);

            if (recipient is null)
            {
                throw new InvalidOperationException(
                    "There is no active signing action for this user.");
            }
            await _envelopeService.DeclineRecipientAsync(
                envelopeId,
                recipient.Id,
                new VoidAndDeclineEnvelopeRequest
                {
                    Reason = reason
                },
                cancellationToken);

            var updatedEnvelope =
                await _envelopeRepository.GetByIdAsync(
                    envelopeId,
                    cancellationToken);

            var updatedRecipient =
                await _envelopeRecipient.GetByIdAsync(
                    recipient.Id,
                    cancellationToken);

            if (
                updatedEnvelope is null ||
                updatedRecipient is null)
            {
                throw new InvalidOperationException(
                    "The decline operation completed but the final state could not be loaded.");
            }

            return new DeclineSigningResultDto
            {
                RecipientId =
                    updatedRecipient.Id,

                RecipientStatus =
                    updatedRecipient.Status,

                EnvelopeStatus =
                    updatedEnvelope.Status,

                Reason =
                    updatedRecipient.DeclineReason ??
                    reason,

                DeclinedAt =
                    updatedRecipient.DeclinedAt
            };
        }
     //   private static List<FinalPdfRenderField> BuildFinalPdfRenderFields(
     //IEnumerable<Fields> fields,
     //IEnumerable<EnvelopeFieldValue> values,
     //IEnumerable<EnvelopeAppliedSignature> appliedMarks)
     //   {
     //       var valuesByFieldId =
     //           values
     //               .GroupBy(x => x.EnvelopeFieldId)
     //               .ToDictionary(
     //                   x => x.Key,
     //                   x => x.Last());

     //       var marksByFieldId =
     //           appliedMarks
     //               .GroupBy(x => x.EnvelopeFieldId)
     //               .ToDictionary(
     //                   x => x.Key,
     //                   x => x.Last());

     //       var result =
     //           new List<FinalPdfRenderField>();

     //       foreach (var field in fields)
     //       {
     //           valuesByFieldId.TryGetValue(
     //               field.Id,
     //               out var value);

     //           marksByFieldId.TryGetValue(
     //               field.Id,
     //               out var mark);

     //           result.Add(
     //               new FinalPdfRenderField
     //               {
     //                   FieldId =
     //                       field.Id,

     //                   PageNumber =
     //                       field.PageNumber,

     //                   X =
     //                       field.X,

     //                   Y =
     //                       field.Y,

     //                   Width =
     //                       field.Width,

     //                   Height =
     //                       field.Height,

     //                   FieldType =
     //                       field.FieldType,

     //                   /*
     //                    * Normal field values
     //                    */
     //                   ValueText =
     //                       value?.ValueText,

     //                   ValueNumber =
     //                       value?.ValueNumber,

     //                   ValueDate =
     //                       value?.ValueDate,

     //                   ValueBoolean =
     //                       value?.ValueBoolean,

     //                   /*
     //                    * Immutable mark snapshot
     //                    */
     //                   AppliedBlobPath =
     //                       mark?.AppliedBlobPath,

     //                   AppliedContentType =
     //                       mark?.AppliedContentType,

     //                   SignatureTypeSnapshot =
     //                       mark?.SignatureTypeSnapshot,

     //                   TypedTextSnapshot =
     //                       mark?.TypedTextSnapshot,

     //                   FontKeySnapshot =
     //                       mark?.FontKeySnapshot
     //               });
     //       }

     //       return result;
     //   }
        public async Task<byte[]> GenerateTestFinalPdfAsync(
    Guid envelopeId,
    Guid documentId,
    CancellationToken cancellationToken = default)
        {
            if (envelopeId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Envelope id is required.");
            }

            if (documentId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Document id is required.");
            }

            var document =
                await _envelopeDocument.GetByIdAsync(
                    documentId,
                    cancellationToken);

            if (
                document is null ||
                document.EnvelopeId != envelopeId)
            {
                throw new KeyNotFoundException(
                    "Document was not found.");
            }

            /*
             * IMPORTANT:
             * OriginalFileName storage path deyil.
             *
             * PDF storage-dan yalnız
             * OriginalBlobPath ilə açılır.
             */
            if (string.IsNullOrWhiteSpace(
                    document.OriginalBlobPath))
            {
                throw new FileNotFoundException(
                    $"Original document storage path is missing for document '{document.Id}'.");
            }

            var fields =
                (
                    await _envelopeField.FindAsync(
                        x =>
                            x.EnvelopeId == envelopeId &&
                            x.EnvelopeDocumentId == documentId,
                        cancellationToken)
                ).ToList();

            var fieldIds =
                fields
                    .Select(x => x.Id)
                    .ToList();

            List<EnvelopeFieldValue> values;

            List<EnvelopeAppliedSignature> appliedMarks;

            if (fieldIds.Count == 0)
            {
                values =
                    new List<EnvelopeFieldValue>();

                appliedMarks =
                    new List<EnvelopeAppliedSignature>();
            }
            else
            {
                values =
                    (
                        await _envelopeFieldValue.FindAsync(
                            x =>
                                fieldIds.Contains(
                                    x.EnvelopeFieldId),
                            cancellationToken)
                    ).ToList();

                appliedMarks =
                    (
                        await _envelopeAppliedSignature.FindAsync(
                            x =>
                                x.EnvelopeId == envelopeId &&
                                fieldIds.Contains(
                                    x.EnvelopeFieldId),
                            cancellationToken)
                    ).ToList();
            }

            var renderFields =
    FinalPdfRenderFieldMapper.Map(
        fields,
        values,
        appliedMarks);

            
            await using var originalStream =
                await _fileStorage.OpenReadAsync(
                    document.OriginalBlobPath,
                    cancellationToken);

            /*
             * Renderer üçün seekable memory stream istifadə edirik.
             * Local və Azure Blob provider davranışından
             * asılı qalmırıq.
             */
            using var originalPdf =
                new MemoryStream();

            await originalStream.CopyToAsync(
                originalPdf,
                cancellationToken);

            originalPdf.Position = 0;

            /*
             * Yeni 6.4E contract:
             *
             * Stream
             * +
             * FinalPdfRenderField[]
             * +
             * CancellationToken
             */
            var renderedPdf =
                await _finalPdfRenderer.RenderAsync(
                    originalPdf,
                    renderFields,
                    cancellationToken);

            return renderedPdf;
        }

        private static void EnsureEnvelopeAvailableForSigning(
    Envelope envelope)
        {
            if (
                envelope.Status != EnvelopeStatus.Sent &&
                envelope.Status != EnvelopeStatus.InProgress)
            {
                throw new InvalidOperationException(
                    "This envelope is not available for signing.");
            }

            if (
                envelope.ExpiresAt.HasValue &&
                envelope.ExpiresAt.Value <=
                    DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException(
                    "This envelope has expired.");
            }
        }
    }
}