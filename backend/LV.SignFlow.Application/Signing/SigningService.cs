using LV.SignFlow.Application.Common.Interfaces;
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

        public SigningService(
        IRepository<Envelope> envelopeRepository, IRepository<Recipients> envelopeRecipient, IRepository<Documents> envelopeDocument, ICurrentUser currentUser, IRepository<Fields> envelopeField, IRepository<EnvelopeFieldValue> envelopeFieldValue, IRepository<EnvelopeAppliedSignature> envelopeAppliedSignature, IUnitOfWork unitOfWork, IRepository<Signature> signatureRepository, IRepository<Stamps> stampRepository, ISignatureAssetStorage signatureAssetStorage, IEnvelopeService envelopeService)
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
            if (envelope.Status != EnvelopeStatus.Sent && envelope.Status != EnvelopeStatus.InProgress)
            {
                throw new InvalidOperationException("This envelope is not available for signing.");
            }

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

        public async Task<SigningAppliedSignatureDto> ApplyMarkAsync(Guid envelopeId, Guid fieldId, ApplySigningMarkRequest request, CancellationToken cancellationToken =
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
            if (envelope.Status != EnvelopeStatus.Sent && envelope.Status != EnvelopeStatus.InProgress)
            {
                throw new InvalidOperationException("This envelope is not available for signing.");
            }

            var recipients = await _envelopeRecipient.FindAsync(
            x => x.EnvelopeId == envelopeId && x.UserId == _currentUser.UserId, cancellationToken);

            var recipient =await GetActiveSigningRecipientAsync(
          envelope,
          cancellationToken);

            if (recipient is null)
            {
                throw new UnauthorizedAccessException("You are not a recipient of this envelope.");
            }
            if (recipient.Status == RecipientStatus.Completed || recipient.Status == RecipientStatus.Declined || recipient.Status == RecipientStatus.Skipped || recipient.Status == RecipientStatus.WaitingForCondition)
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

            var expectedKind = field.FieldType
            switch
            {
                FieldType.Signature => AppliedMarkKind.Signature,

                FieldType.Initial => AppliedMarkKind.Initial,

                FieldType.Stamp => AppliedMarkKind.Stamp,

                _ =>
                throw new InvalidOperationException("This field is not a signature field.")
            };

            if (request.Kind != expectedKind)
            {
                throw new InvalidOperationException("The applied mark type does not match the field type.");
            }
            string appliedBlobPath;

            if (request.Kind == AppliedMarkKind.Signature || request.Kind == AppliedMarkKind.Initial)
            {
                if (!request.SourceSignatureId.HasValue)
                {
                    throw new ArgumentException("Source signature is required.");
                }

                var signature = await _signatureRepository.GetByIdAsync(
                request.SourceSignatureId.Value, cancellationToken);

                if (signature is null || !signature.IsActive || signature.UserId != _currentUser.UserId)
                {
                    throw new UnauthorizedAccessException("Signature was not found or does not belong to you.");
                }

                appliedBlobPath = signature.BlobPath;

                request.SourceStampId = null;
            }
            else
            {
                if (!request.SourceStampId.HasValue)
                {
                    throw new ArgumentException("Source stamp is required.");
                }

                var stamp = await _stampRepository.GetByIdAsync(
                request.SourceStampId.Value, cancellationToken);

                if (stamp is null || !stamp.IsActive)
                {
                    throw new KeyNotFoundException("Stamp was not found.");
                }

                appliedBlobPath = stamp.BlobPath;

                request.SourceSignatureId = null;
            }

            var existingMarks = await _envelopeAppliedSignature.FindAsync(
            x => x.EnvelopeFieldId == field.Id, cancellationToken);

            var appliedMark = existingMarks.FirstOrDefault();

            var isNew = appliedMark is null;

            if (isNew)
            {
                appliedMark = new EnvelopeAppliedSignature
                {
                    Id = Guid.NewGuid(),

                    EnvelopeId = envelope.Id,

                    EnvelopeFieldId = field.Id,

                    RecipientId = recipient.Id
                };
            }

            appliedMark!.AppliedByUserId = _currentUser.UserId;

            appliedMark.Kind = request.Kind;

            appliedMark.SourceSignatureId = request.SourceSignatureId;

            appliedMark.SourceStampId = request.SourceStampId;

            appliedMark.AppliedBlobPath = appliedBlobPath;

            appliedMark.AppliedAt = DateTimeOffset.UtcNow;

            if (isNew)
            {
                await _envelopeAppliedSignature.AddAsync(
                appliedMark, cancellationToken);
            }
            else
            {
                _envelopeAppliedSignature.Update(
                appliedMark);
            }

            await _unitOfWork.SaveChangesAsync(
            cancellationToken);

            return new SigningAppliedSignatureDto
            {
                Id = appliedMark.Id,

                Kind = appliedMark.Kind,

                AppliedAt = appliedMark.AppliedAt,
                SourceSignatureId = appliedMark.SourceSignatureId,
                SourceStampId = appliedMark.SourceStampId,
            };

        }

        public async Task<CreatedSigningSignatureDto> CreateSignatureAsync(CreateSigningSignatureRequest request, CancellationToken cancellationToken =default)
        {
            EnsureAuthenticated();

            if (request.Kind != SignatureKind.Signature && request.Kind != SignatureKind.Initials)
            {
                throw new ArgumentException("Invalid signature kind.");
            }

            if (request.Type != SignatureType.Typed && request.Type != SignatureType.Drawn)
            {
                throw new ArgumentException("Only typed and drawn signatures can be created here.");
            }

            string blobPath;

            if (request.Type == SignatureType.Typed)
            {
                if (string.IsNullOrWhiteSpace(request.TypedText))
                {
                    throw new ArgumentException("Typed signature text is required.");
                }

                blobPath = await _signatureAssetStorage.SaveTypedAsync(
                _currentUser.UserId, request.TypedText, request.FontKey, cancellationToken);
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.ImageDataUrl))
                {
                    throw new ArgumentException("Drawn signature image is required.");
                }

                blobPath = await _signatureAssetStorage.SaveDrawnAsync(
                _currentUser.UserId, request.ImageDataUrl, cancellationToken);
            }

            var now = DateTimeOffset.UtcNow;

            var signature = new Signature
            {
                Id = Guid.NewGuid(),

                UserId = _currentUser.UserId,

                Kind = request.Kind,

                Type = request.Type,

                BlobPath = blobPath,

                IsDefault = false,

                IsActive = true,

                CreatedAt = now,

                UpdatedAt = now
            };

            await _signatureRepository.AddAsync(
            signature, cancellationToken);

            await _unitOfWork.SaveChangesAsync(
            cancellationToken);

            return new CreatedSigningSignatureDto
            {
                Id = signature.Id,

                Kind = signature.Kind,

                Type = signature.Type,

                ContentUrl = $"/api/signing/signatures/{signature.Id}/content",

                CreatedAt = signature.CreatedAt
            };
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

            if (
                envelope.Status != EnvelopeStatus.Sent &&
                envelope.Status != EnvelopeStatus.InProgress)
            {
                throw new InvalidOperationException(
                    "This envelope is not available for signing.");
            }

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

    }
}