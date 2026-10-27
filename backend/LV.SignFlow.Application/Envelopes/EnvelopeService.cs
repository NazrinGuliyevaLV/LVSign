using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Application.Envelopes.Dtos;
using LV.SignFlow.Application.Envelopes.Requests;
using LV.SignFlow.Application.Templates;
using LV.SignFlow.Domain.Entities.Envelopes;
using LV.SignFlow.Domain.Entities.Signing;
using LV.SignFlow.Domain.Entities.Templates;
using LV.SignFlow.Domain.Entities.Users;
using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using LV.SignFlow.Domain.Enums.SigningEnums;
using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace LV.SignFlow.Application.Envelopes
{
    public class EnvelopeService : IEnvelopeService
    {
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorage _fileStorage;
        private readonly IRepository<Envelope> _envelopeRepository;
        private readonly IRepository<Fields> _envelopeField;
        private readonly IRepository<Recipients> _envelopeRecipient;
        private readonly IRepository<Documents> _envelopeDocument;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Events> _eventRepository;
        private readonly IRepository<StatusHistory> _statusHistoryRepository;
        private readonly IRepository<TemplateShare> _templateShareRepository;
        private readonly IRepository<TemplateDocument> _templateDocumentRepository;
        private readonly IRepository<TemplateField> _templateFieldRepository;
        private readonly IRepository<TemplateRecipientRole> _templateRecipientRoleRepository;
        private readonly IRepository<TemplateRoutingRule> _templateRoutingRuleRepository;
        private readonly IRepository<TemplateRoutingRuleSet> _templateRoutingRuleSetRepository;
        private readonly IRepository<EnvelopeRoutingRule> _envelopeRoutingRuleRepository;
        private readonly IRepository<EnvelopeRoutingRuleSet> _envelopeRoutingRuleSetRepository;
        private readonly IRepository<TemplateRoutingCondition> _templateRoutingConditionRepository;
        private readonly IRepository<EnvelopeRoutingCondition> _envelopeRoutingConditionRepository;
        private readonly ITemplateRepository _templateRepository;
        private readonly IRepository<EnvelopeFieldValue> _fieldValueRepository;
        private readonly IRepository<Signature> _signatureRepository;
        private readonly IRepository<Stamps> _stampRepository;
        private readonly IRepository<EnvelopeRoutingRule> _envelopeRoutingRule;
        private readonly IRepository<EnvelopeRoutingRuleSet> _envelopeRoutingRuleSet;
        private readonly IRepository<EnvelopeRoutingCondition> _envelopeRoutingCondition;
        private readonly IRepository<EnvelopeAppliedSignature> _appliedSignatureRepository;

        public EnvelopeService(ICurrentUser currentUser, IUnitOfWork unitOfWork, IFileStorage fileStorage, IRepository<Envelope> envelopeRepository, IRepository<Fields> envelopeField, IRepository<Recipients> envelopeRecipient, IRepository<Documents> envelopeDocument, IRepository<User> userRepository, IRepository<TemplateShare> templateShareRepository, ITemplateRepository templateRepository, IRepository<TemplateDocument> templateDocumentRepository, IRepository<TemplateField> templateFieldRepository, IRepository<TemplateRecipientRole> templateRecipientRoleRepository, IRepository<TemplateRoutingRule> templateRoutingRuleRepository, IRepository<TemplateRoutingRuleSet> templateRoutingRuleSetRepository, IRepository<EnvelopeRoutingRule> envelopeRoutingRuleRepository, IRepository<EnvelopeRoutingRuleSet> envelopeRoutingRuleSetRepository, IRepository<TemplateRoutingCondition> templateRoutingConditionRepository, IRepository<EnvelopeRoutingCondition> envelopeRoutingConditionRepository, IRepository<Events> eventRepository, IRepository<StatusHistory> statusHistoryRepository, IRepository<EnvelopeFieldValue> fieldValueRepository, IRepository<Signature> signatureRepository, IRepository<Stamps> stampRepository, IRepository<EnvelopeAppliedSignature> appliedSignatureRepository, IRepository<EnvelopeRoutingRule> envelopeRoutingRule, IRepository<EnvelopeRoutingRuleSet> envelopeRoutingRuleSet, IRepository<EnvelopeRoutingCondition> envelopeRoutingCondition)
        {
            _currentUser = currentUser;
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
            _envelopeRepository = envelopeRepository;
            _envelopeField = envelopeField;
            _envelopeRecipient = envelopeRecipient;
            _envelopeDocument = envelopeDocument;
            _userRepository = userRepository;
            _templateShareRepository = templateShareRepository;
            _templateRepository = templateRepository;
            _templateDocumentRepository = templateDocumentRepository;
            _templateFieldRepository = templateFieldRepository;
            _templateRecipientRoleRepository = templateRecipientRoleRepository;
            _templateRoutingRuleRepository = templateRoutingRuleRepository;
            _templateRoutingRuleSetRepository = templateRoutingRuleSetRepository;
            _envelopeRoutingRuleRepository = envelopeRoutingRuleRepository;
            _envelopeRoutingRuleSetRepository = envelopeRoutingRuleSetRepository;
            _templateRoutingConditionRepository = templateRoutingConditionRepository;
            _envelopeRoutingConditionRepository = envelopeRoutingConditionRepository;
            _eventRepository = eventRepository;
            _statusHistoryRepository = statusHistoryRepository;
            _fieldValueRepository = fieldValueRepository;
            _signatureRepository = signatureRepository;
            _stampRepository = stampRepository;
            _appliedSignatureRepository = appliedSignatureRepository;
            _envelopeRoutingRule = envelopeRoutingRule;
            _envelopeRoutingRuleSet = envelopeRoutingRuleSet;
            _envelopeRoutingCondition = envelopeRoutingCondition;
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthentificated) throw new UnauthorizedAccessException("Authentification is required");
        }
        //envelope
        public async Task<Guid> CreateEnvelopeAsync(EnvelopeRequest request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();

            if (string.IsNullOrWhiteSpace(request.Subject)) throw new ArgumentException("Envelope subject is required.");

            if (request.Subject.Trim().Length > 500) throw new ArgumentException("Envelope subject cannot exceed 500 characters.");

            if (request.Message?.Length > 4000) throw new ArgumentException("Envelope message cannot exceed 4000 characters.");

            if (request.ReminderIntervalDays.HasValue && request.ReminderIntervalDays.Value <= 0)
            {
                throw new ArgumentException("Reminder interval must be greater than zero.");
            }
            var envelope = new Envelope()
            {
                Id = Guid.NewGuid(),
                Subject = request.Subject.Trim(),
                Message = request.Message,
                CreatedByUserId = _currentUser.UserId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow,
                OrganizationId = _currentUser.OrganizationId,
                IsDeleted = false,
                SourceTemplateVersionId = null,
                CurrentRoutingOrder = null,
                Status = EnvelopeStatus.Draft,
                Type = request.Type,
                ReminderIntervalDays =request.ReminderIntervalDays,

            };

            await _envelopeRepository.AddAsync(envelope);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return envelope.Id;
        }

        public async Task<IReadOnlyList<EnvelopeListItemDto>> GetAllAsync(CancellationToken cancellationToken =
          default)
        {
            EnsureAuthenticated();
            var envelopes = await _envelopeRepository.FindAsync(x => x.OrganizationId == _currentUser.OrganizationId && !x.IsDeleted, cancellationToken);

            var envelopeIDs = envelopes.
            Select(x => x.Id)
              .ToHashSet();

            var recipients = await _envelopeRecipient.FindAsync(x => envelopeIDs.Contains(x.EnvelopeId), cancellationToken);
            var documents = await _envelopeDocument.FindAsync(x => envelopeIDs.Contains(x.EnvelopeId), cancellationToken);
            var senderIDs = envelopes.Select(x => x.CreatedByUserId).Distinct().ToHashSet();

            var senders = await _userRepository.FindAsync(x => senderIDs.Contains(x.Id), cancellationToken);
            var senderMap = senders.ToDictionary(x => x.Id);

            return envelopes.OrderByDescending(x => x.UpdatedAt)
              .Select(envelope => {
                  var envelopeRecipients = recipients
              .Where(x => x.EnvelopeId == envelope.Id)
              .OrderBy(x => x.RoutingOrder)
              .ToList();

                  var envelopeDocuments = documents
              .Where(x => x.EnvelopeId == envelope.Id)
              .ToList();

                  senderMap.TryGetValue(envelope.CreatedByUserId, out
              var sender);

                  return new EnvelopeListItemDto
                  {
                      Id = envelope.Id,
                      DocumentId = $"LVS-{envelope.Id}".ToUpperInvariant(),
                      Title = envelope.Subject,
                      SenderId = envelope.CreatedByUserId,
                      SenderName = sender?.DisplayName ?? string.Empty,
                      Status = GetAgreementStatus(envelope.Status),
                      UpdatedAt = envelope.UpdatedAt,
                      DueAt = envelope.ExpiresAt,
                      Pages = GetTotalPages(envelopeDocuments),
                      Signers = envelopeRecipients.Select(x => new EnvelopeSignerDto
                      {
                          RecipientId = x.Id,
                          PersonId = x.UserId,
                          Name = x.Name,
                          State = GetSignerState(x.Status),
                          Date = GetSignerDate(x)
                      }).ToList()
                  };

              }).ToList();
        }
        private static string GetAgreementStatus(EnvelopeStatus status)
        {
            return status
            switch
            {
                EnvelopeStatus.Draft => "draft",

                EnvelopeStatus.Sent =>
                "awaiting",

                EnvelopeStatus.InProgress =>
                "awaiting",

                EnvelopeStatus.Completed =>
                "completed",

                EnvelopeStatus.Declined =>
                "declined",

                EnvelopeStatus.Voided =>
                "voided",

                EnvelopeStatus.Expired =>
                "expired",

                _ => "draft"
            };
        }

        private static string GetSignerState(
          RecipientStatus status)
        {
            return status
            switch
            {
                RecipientStatus.Created =>
                  "notSent",

                RecipientStatus.Pending =>
                "pending",

                RecipientStatus.Active =>
                "current",

                RecipientStatus.Viewed =>
                "current",

                RecipientStatus.Completed =>
                "signed",

                RecipientStatus.Declined =>
                "declined",

                RecipientStatus.Skipped =>
                "skipped",

                RecipientStatus.Canceled =>
                "canceled",

                RecipientStatus.Delegated =>
                "delegated",

                _ => "pending"
            };
        }

        private static DateTimeOffset? GetSignerDate(
          Recipients recipient)
        {
            return recipient.Status
            switch
            {
                RecipientStatus.Completed =>
                  recipient.CompletedAt,

                RecipientStatus.Declined =>
                recipient.DeclinedAt,

                RecipientStatus.Viewed =>
                recipient.ViewedAt,

                RecipientStatus.Active =>
                recipient.ActivatedAt,

                _ => null
            };
        }

        private static int? GetTotalPages(
          IReadOnlyList<Documents> documents)
        {
            if (documents.Count == 0)
                return 0;

            if (documents.Any(x => !x.PageCount.HasValue))
                return null;

            return documents.Sum(
              x => x.PageCount!.Value);
        }

        public async Task<EnvelopeDetailsDto> GetByIdAsync(Guid envelopeId, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            if (envelopeId == Guid.Empty)
                throw new ArgumentException("Envelope id is required.");

            var envelope = await _envelopeRepository.GetByIdAsync(envelopeId, cancellationToken);
            if (envelope is null || envelope.IsDeleted || envelope.OrganizationId != _currentUser.OrganizationId)
            {
                return null;
            }

            return new EnvelopeDetailsDto
            {
                CompletedAt = envelope.CompletedAt,
                Subject = envelope.Subject,
                Type = envelope.Type,
                Status = envelope.Status,
                CreatedAt = envelope.CreatedAt,
                SentAt = envelope.SentAt,
                ExpiresAt = envelope.ExpiresAt,
                Id = envelope.Id,
                SourceTemplateVersionId = envelope.SourceTemplateVersionId,
                CurrentRoutingOrder = envelope.CurrentRoutingOrder,
                VoidedAt = envelope.VoidedAt,
                Message = envelope.Message,
                CreatedByUserId = envelope.CreatedByUserId,
                UpdatedAt = envelope.UpdatedAt,
                ReminderIntervalDays =envelope.ReminderIntervalDays,

            };
        }

        public async Task DeleteEnvelopeAsync(Guid envelopeId, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            if (envelopeId == Guid.Empty)
                throw new ArgumentException("Envelope id is required.");
            var envelope = await _envelopeRepository.GetByIdAsync(envelopeId, cancellationToken);
            if (envelope is null || envelope.IsDeleted || envelope.OrganizationId != _currentUser.OrganizationId)
            {
                throw new ArgumentException("Envelope was not found.");
            }
            _envelopeRepository.Delete(envelope);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

        }

        public async Task<EnvelopeDetailsDto> UpdateEnvelopeAsync(Guid envelopeId, EnvelopeRequest request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            if (envelopeId == Guid.Empty)
                throw new ArgumentException("Envelope id is required.");

            var envelope = await _envelopeRepository.GetByIdAsync(envelopeId, cancellationToken);
            if (envelope is null || envelope.IsDeleted || envelope.OrganizationId != _currentUser.OrganizationId)
            {
                throw new ArgumentException("Envelope was not found.");
            }
            if (request.ReminderIntervalDays.HasValue &&request.ReminderIntervalDays.Value <= 0)
            {
                throw new ArgumentException("Reminder interval must be greater than zero.");
            }
            envelope.Subject = request.Subject;
            envelope.Message = request.Message;
            envelope.Type = request.Type;
            envelope.ExpiresAt = request.ExpiresAt;
            envelope.ReminderIntervalDays = request.ReminderIntervalDays;
            envelope.UpdatedAt = DateTimeOffset.UtcNow;

            _envelopeRepository.Update(envelope);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new EnvelopeDetailsDto
            {
                CompletedAt = envelope.CompletedAt,
                Subject = envelope.Subject,
                Type = envelope.Type,
                Status = envelope.Status,
                CreatedAt = envelope.CreatedAt,
                SentAt = envelope.SentAt,
                ExpiresAt = envelope.ExpiresAt,
                Id = envelope.Id,
                SourceTemplateVersionId = envelope.SourceTemplateVersionId,
                CurrentRoutingOrder = envelope.CurrentRoutingOrder,
                VoidedAt = envelope.VoidedAt,
                Message = envelope.Message,
                CreatedByUserId = envelope.CreatedByUserId,
                UpdatedAt = envelope.UpdatedAt,
                ReminderIntervalDays =envelope.ReminderIntervalDays

            };
        }
        //document
        public async Task<EnvelopeDocumentDto> UploadDocumentAsync(Guid envelopeId, UploadEnvelopeDocumentRequest request, CancellationToken cancellationToken =
          default)
        {
            if (envelopeId == Guid.Empty)
                throw new ArgumentException("Envelope id is required.");

            if (request.Stream == Stream.Null || !request.Stream.CanRead)
                throw new ArgumentException("A valid document stream is required.");

            if (request.FileSize <= 0)
                throw new ArgumentException("The document is empty.");

            var envelope = await _envelopeRepository.GetByIdAsync(envelopeId, cancellationToken);
            if (envelope is null || envelope.IsDeleted || envelope.OrganizationId != _currentUser.OrganizationId || envelope.CreatedByUserId != _currentUser.UserId)
            {
                throw new ArgumentException("Envelope was not found.");
            }
            var existingDocuments = await _envelopeDocument.FindAsync(x => x.EnvelopeId == envelopeId, cancellationToken);
            var nextOrder = existingDocuments.Count == 0 ? 1 : existingDocuments.Max(x => x.Order) + 1;

            var folder = $"envelopes/{envelope.Id}/documents";

            string? blobPath = null;

            try
            {
                blobPath = await _fileStorage.UploadAsync(
                  request.Stream,
                  request.FileName,
                  "application/pdf",
                  folder,
                  cancellationToken);

                var now = DateTimeOffset.UtcNow;
                var document = new Documents
                {
                    Id = Guid.NewGuid(),
                    EnvelopeId = envelope.Id,
                    SourceTemplateDocumentId = null,
                    OriginalBlobPath = blobPath,
                    FinalBlobPath = null,
                    ContentType = "application/pdf",
                    FileSize = request.FileSize,
                    OriginalHash = null,
                    FinalHash = null,
                    Order = nextOrder,
                    PageCount = null,
                    CreatedAt = now,
                    OriginalFileName = request.FileName
                };
                await _envelopeDocument.AddAsync(document, cancellationToken);
                envelope.UpdatedAt = now;
                _envelopeRepository.Update(envelope);

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return new EnvelopeDocumentDto
                {
                    Id = document.Id,
                    EnvelopeId = document.EnvelopeId,
                    OriginalFileName = document.OriginalFileName,
                    ContentType = document.ContentType,
                    FileSize = document.FileSize,
                    Order = document.Order,
                    PageCount = document.PageCount
                };
            }
            catch
            {
                if (!string.IsNullOrWhiteSpace(blobPath))
                {
                    await _fileStorage.DeleteAsync(
                      blobPath,
                      cancellationToken);
                }

                throw;
            }

        }

        public async Task<IReadOnlyList<EnvelopeDocumentDto>> GetDocumentsAsync(Guid envelopeId, CancellationToken cancellationToken =
          default)
        {
            if (envelopeId == Guid.Empty)
                throw new ArgumentException("Envelope id is required.");

            var envelope = await _envelopeRepository.GetByIdAsync(envelopeId, cancellationToken);
            if (envelope is null || envelope.IsDeleted || envelope.OrganizationId != _currentUser.OrganizationId || envelope.CreatedByUserId != _currentUser.UserId)
            {
                throw new ArgumentException("Envelope was not found.");
            }
            var documents = await _envelopeDocument.FindAsync(x => x.EnvelopeId == envelopeId, cancellationToken);

            return documents
              .OrderBy(x => x.Order)
              .Select(x => new EnvelopeDocumentDto
              {
                  Id = x.Id,
                  EnvelopeId = x.EnvelopeId,
                  OriginalFileName = x.OriginalFileName,
                  ContentType = x.ContentType,
                  FileSize = x.FileSize,
                  Order = x.Order,
                  PageCount = x.PageCount,
              }).ToList();

        }

        public async Task<EnvelopeDocumentContentResultDto> GetDocumentContentAsync(Guid envelopeId, Guid documentId, CancellationToken cancellationToken =
          default)
        {
            if (envelopeId == Guid.Empty)
                throw new ArgumentException("Envelope id is required.");

            var envelope = await _envelopeRepository.GetByIdAsync(envelopeId, cancellationToken);
            if (envelope is null || envelope.IsDeleted || envelope.OrganizationId != _currentUser.OrganizationId || envelope.CreatedByUserId != _currentUser.UserId)
            {
                throw new ArgumentException("Envelope was not found.");
            }
            var document = await _envelopeDocument.GetByIdAsync(documentId, cancellationToken);

            if (document is null || document.EnvelopeId != envelopeId)
            {
                throw new KeyNotFoundException(
                  "Envelope document was not found.");
            }

            var stream = await _fileStorage.OpenReadAsync(document.OriginalBlobPath, cancellationToken);

            return new EnvelopeDocumentContentResultDto
            {
                Stream = stream,
                FileName = document.OriginalFileName,
                ContentType = document.ContentType
            };
        }

        public async Task DeleteDocumentAsync(Guid envelopeId, Guid documentId, CancellationToken cancellationToken =
          default)
        {
            if (envelopeId == Guid.Empty)
                throw new ArgumentException("Envelope id is required.");

            var envelope = await _envelopeRepository.GetByIdAsync(envelopeId, cancellationToken);
            if (envelope is null || envelope.IsDeleted || envelope.OrganizationId != _currentUser.OrganizationId || envelope.CreatedByUserId != _currentUser.UserId)
            {
                throw new ArgumentException("Envelope was not found.");
            }
            if (envelope.Status != EnvelopeStatus.Draft)
            {
                throw new InvalidOperationException(
                    "Documents can only be removed from draft envelopes.");
            }
            var document = await _envelopeDocument.GetByIdAsync(documentId, cancellationToken);

            if (document is null || document.EnvelopeId != envelopeId)
            {
                throw new KeyNotFoundException(
                  "Envelope document was not found.");
            }
            var fields = await _envelopeField.FindAsync(x =>
                x.EnvelopeId == envelopeId &&
                x.EnvelopeDocumentId == documentId,cancellationToken);

            var fieldIds =fields.Select(x => x.Id).ToList();

            if (fieldIds.Count > 0)
            {
                var conditions = await _envelopeRoutingCondition.FindAsync(
                        x =>
                            fieldIds.Contains(
                                x.SourceEnvelopeFieldId),
                        cancellationToken);

                foreach (var condition in conditions)
                {
                    _envelopeRoutingCondition.Delete(condition);
                }

                foreach (var field in fields)
                {
                    _envelopeField.Delete(field);
                }
            }

            var blobPath = document.OriginalBlobPath;

            _envelopeDocument.Delete(document);
            envelope.UpdatedAt =DateTimeOffset.UtcNow;

            _envelopeRepository.Update(envelope);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            try
            {
                await _fileStorage.DeleteAsync(
                  blobPath,
                  cancellationToken);
            }
            catch
            {

            }
        }
        //recipient

        private static string GetDefaultRoleName(RecipientType recipientType)
        {
            return recipientType
            switch
            {
                RecipientType.Signer => "Signer",
                RecipientType.Approver => "Approver",
                RecipientType.Viewer => "Viewer",
                RecipientType.CopyRecipient => "Copy Recipient",
                _ => "Recipient"
            };
        }
        public async Task<EnvelopeRecipientDto> AddEnvelopeRecipientAsync(Guid envelopeId, EnvelopeRecipientRequest request, CancellationToken cancellationToken =
          default)
        {
            if (envelopeId == Guid.Empty)
                throw new ArgumentException("Envelope id is required.");

            if (request.UserId == Guid.Empty)
                throw new ArgumentException("Recipient user is required.");

            if (request.RoutingOrder <= 0)
                throw new ArgumentException("Recipient order must be greater than zero.");

            var envelope = await _envelopeRepository.GetByIdAsync(envelopeId, cancellationToken);

            if (envelope is null ||
              envelope.IsDeleted ||
              envelope.OrganizationId != _currentUser.OrganizationId ||
              envelope.CreatedByUserId != _currentUser.UserId)
            {
                throw new KeyNotFoundException(
                  "Envelope was not found.");
            }

            var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

            if (user is null ||
              !user.IsActive ||
              user.OrganizationId != _currentUser.OrganizationId)
            {
                throw new ArgumentException(
                  "The selected recipient user is invalid.");
            }

            var roleName =
              string.IsNullOrWhiteSpace(request.RoleName) ?
              GetDefaultRoleName(request.RecipientType) :
              request.RoleName.Trim();

            var recipient = new Recipients
            {
                Id = Guid.NewGuid(),
                EnvelopeId = envelope.Id,
                UserId = user.Id,
                SourceTemplateRecipientRoleId = null,
                RoleName = roleName,
                Name = user.DisplayName,
                Email = user.Email,
                RecipientType = request.RecipientType,
                Status = RecipientStatus.Created,
                RoutingOrder = request.RoutingOrder,
                IsRequired = request.IsRequired,
                AllowSenderEditRecipient = request.AllowSenderEditRecipient,
                AllowSenderDeleteRecipient = request.AllowSenderDeleteRecipient,
                AllowSenderChangeRoutingOrder = request.AllowSenderChangeRoutingOrder
            };

            await _envelopeRecipient.AddAsync(recipient, cancellationToken);

            envelope.UpdatedAt = DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new EnvelopeRecipientDto
            {
                Id = recipient.Id,
                EnvelopeId = recipient.EnvelopeId,
                UserId = recipient.UserId,
                RoleName = recipient.RoleName,
                Name = recipient.Name,
                Email = recipient.Email,
                RecipientType = recipient.RecipientType,
                Status = recipient.Status,
                RoutingOrder = recipient.RoutingOrder,
                IsRequired = recipient.IsRequired,
                AllowSenderEditRecipient = recipient.AllowSenderEditRecipient,
                AllowSenderDeleteRecipient = recipient.AllowSenderDeleteRecipient,
                AllowSenderChangeRoutingOrder = recipient.AllowSenderChangeRoutingOrder
            };
        }

        public async Task<IReadOnlyList<EnvelopeRecipientDto>> GetAllEnvelopeRecipientsAsync(Guid envelopeId, CancellationToken cancellationToken =
          default)
        {
            var envelope = await _envelopeRepository.GetByIdAsync(envelopeId, cancellationToken);

            if (envelope is null ||
              envelope.IsDeleted ||
              envelope.OrganizationId != _currentUser.OrganizationId ||
              envelope.CreatedByUserId != _currentUser.UserId)
            {
                throw new KeyNotFoundException(
                  "Envelope was not found.");
            }
            var recipients = await _envelopeRecipient.FindAsync(x => x.EnvelopeId == envelopeId, cancellationToken);

            return recipients
              .OrderBy(x => x.RoutingOrder)
              .ThenBy(x => x.Name)
              .Select(x => new EnvelopeRecipientDto
              {
                  Id = x.Id,
                  EnvelopeId = x.EnvelopeId,
                  UserId = x.UserId,
                  RoleName = x.RoleName,
                  Name = x.Name,
                  Email = x.Email,
                  RecipientType = x.RecipientType,
                  Status = x.Status,
                  RoutingOrder = x.RoutingOrder,
                  IsRequired = x.IsRequired,
                  AllowSenderEditRecipient =
                  x.AllowSenderEditRecipient,
                  AllowSenderDeleteRecipient =
                  x.AllowSenderDeleteRecipient,
                  AllowSenderChangeRoutingOrder =
                  x.AllowSenderChangeRoutingOrder
              })
              .ToList();

        }

        public async Task DeleteEnvelopeRecipientAsync(Guid envelopeId, Guid recipientId, CancellationToken cancellationToken =
          default)
        {
            EnsureAuthenticated();

            var envelope =
              await _envelopeRepository.GetByIdAsync(
                envelopeId,
                cancellationToken);

            if (envelope is null ||
              envelope.IsDeleted ||
              envelope.OrganizationId != _currentUser.OrganizationId ||
              envelope.CreatedByUserId != _currentUser.UserId)
            {
                throw new KeyNotFoundException("Envelope was not found.");
            }

            if (envelope.Status != EnvelopeStatus.Draft)
            {
                throw new InvalidOperationException("Recipients can only be deleted while the envelope is a draft.");
            }

            var recipient =
              await _envelopeRecipient.GetByIdAsync(
                recipientId,
                cancellationToken);

            if (recipient is null ||
              recipient.EnvelopeId != envelopeId)
            {
                throw new KeyNotFoundException("Envelope recipient was not found.");
            }

            if (!recipient.AllowSenderDeleteRecipient)
            {
                throw new InvalidOperationException("This recipient cannot be deleted by the sender.");
            }

            var assignedFields =
              await _envelopeField.FindAsync(
                x => x.EnvelopeRecipientId == recipientId,
                cancellationToken);

            if (assignedFields.Count > 0)
            {
                throw new InvalidOperationException("This recipient cannot be deleted because one or more fields are assigned to it.");
            }

            _envelopeRecipient.Delete(recipient);

            envelope.UpdatedAt =
              DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        public async Task<EnvelopeRecipientDto> UpdateEnvelopeRecipientAsync(Guid envelopeId, Guid recipientId, EnvelopeRecipientRequest request, CancellationToken cancellationToken =
          default)
        {
            EnsureAuthenticated();

            if (request.UserId == Guid.Empty)
                throw new ArgumentException(
                  "Recipient user is required.");

            if (request.RoutingOrder <= 0)
                throw new ArgumentException(
                  "Routing order must be greater than zero.");

            var envelope =
              await _envelopeRepository.GetByIdAsync(
                envelopeId,
                cancellationToken);

            if (envelope is null ||
              envelope.IsDeleted ||
              envelope.OrganizationId != _currentUser.OrganizationId ||
              envelope.CreatedByUserId != _currentUser.UserId)
            {
                throw new KeyNotFoundException(
                  "Envelope was not found.");
            }

            if (envelope.Status != EnvelopeStatus.Draft)
            {
                throw new InvalidOperationException(
                  "Recipients can only be modified while the envelope is a draft.");
            }

            var recipient =
              await _envelopeRecipient.GetByIdAsync(
                recipientId,
                cancellationToken);

            if (recipient is null ||
              recipient.EnvelopeId != envelopeId)
            {
                throw new KeyNotFoundException(
                  "Envelope recipient was not found.");
            }

            var recipientDetailsChanged =
              recipient.UserId != request.UserId ||
              recipient.RoleName != request.RoleName?.Trim() ||
              recipient.RecipientType != request.RecipientType ||
              recipient.IsRequired != request.IsRequired;

            if (recipientDetailsChanged &&
              !recipient.AllowSenderEditRecipient)
            {
                throw new InvalidOperationException(
                  "This recipient cannot be edited by the sender.");
            }

            if (recipient.RoutingOrder != request.RoutingOrder &&
              !recipient.AllowSenderChangeRoutingOrder)
            {
                throw new InvalidOperationException(
                  "The routing order of this recipient cannot be changed.");
            }

            var user =
              await _userRepository.GetByIdAsync(
                request.UserId,
                cancellationToken);

            if (user is null ||
              !user.IsActive ||
              user.OrganizationId != _currentUser.OrganizationId)
            {
                throw new ArgumentException(
                  "The selected recipient user is invalid.");
            }

            recipient.UserId = user.Id;
            recipient.Name = user.DisplayName;
            recipient.Email = user.Email;

            recipient.RoleName =
              string.IsNullOrWhiteSpace(request.RoleName) ?
              GetDefaultRoleName(request.RecipientType) :
              request.RoleName.Trim();

            recipient.RecipientType =
              request.RecipientType;

            recipient.RoutingOrder =
              request.RoutingOrder;

            recipient.IsRequired =
              request.IsRequired;

            _envelopeRecipient.Update(recipient);

            envelope.UpdatedAt =
              DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new EnvelopeRecipientDto
            {
                Id = recipient.Id,
                EnvelopeId = recipient.EnvelopeId,
                UserId = recipient.UserId,
                RoleName = recipient.RoleName,
                Name = recipient.Name,
                Email = recipient.Email,
                RecipientType = recipient.RecipientType,
                Status = recipient.Status,
                RoutingOrder = recipient.RoutingOrder,
                IsRequired = recipient.IsRequired,
                AllowSenderEditRecipient =
                recipient.AllowSenderEditRecipient,
                AllowSenderDeleteRecipient =
                recipient.AllowSenderDeleteRecipient,
                AllowSenderChangeRoutingOrder =
                recipient.AllowSenderChangeRoutingOrder
            };
        }
        //field
        private static EnvelopeFieldDto MapField(
          Fields field)
        {
            return new EnvelopeFieldDto
            {
                Id = field.Id,
                EnvelopeId = field.EnvelopeId,
                EnvelopeDocumentId =
                field.EnvelopeDocumentId,
                EnvelopeRecipientId =
                field.EnvelopeRecipientId,
                SourceTemplateFieldId =
                field.SourceTemplateFieldId,
                OwnerType = field.OwnerType,
                FieldType = field.FieldType,
                PageNumber = field.PageNumber,
                X = field.X,
                Y = field.Y,
                Width = field.Width,
                Height = field.Height,
                IsRequired = field.IsRequired,
                Label = field.Label,
                Options =
                string.IsNullOrWhiteSpace(field.OptionJson) ?
                null :
                JsonSerializer.Deserialize<List<string>>(
                  field.OptionJson),

            };
        }
        private static void ValidateNormalizedCoordinates(
          decimal x,
          decimal y,
          decimal width,
          decimal height)
        {
            if (x < 0 || x > 1)
                throw new ArgumentException(
                  "X must be between 0 and 1.");

            if (y < 0 || y > 1)
                throw new ArgumentException(
                  "Y must be between 0 and 1.");

            if (width <= 0 || width > 1)
                throw new ArgumentException(
                  "Width must be greater than 0 and not exceed 1.");

            if (height <= 0 || height > 1)
                throw new ArgumentException(
                  "Height must be greater than 0 and not exceed 1.");

            if (x + width > 1)
                throw new ArgumentException(
                  "The field exceeds the page width.");

            if (y + height > 1)
                throw new ArgumentException(
                  "The field exceeds the page height.");
        }
        private static void ValidateFieldRequest(
          EnvelopeFieldRequest request)
        {
            if (request.EnvelopeDocumentId == Guid.Empty)
                throw new ArgumentException(
                  "Envelope document is required.");

            if (request.PageNumber <= 0)
                throw new ArgumentException(
                  "Page number must be greater than zero.");

            ValidateNormalizedCoordinates(
              request.X,
              request.Y,
              request.Width,
              request.Height);
        }
        private async Task ValidateFieldOwnerAsync(
          Guid envelopeId,
          EnvelopeFieldRequest request,
          CancellationToken cancellationToken)
        {
            if (request.OwnerType == FieldOwnerType.Recipient)
            {
                if (!request.EnvelopeRecipientId.HasValue)
                {
                    throw new ArgumentException(
                      "Recipient is required for recipient-owned fields.");
                }

                var recipient =
                  await _envelopeRecipient.GetByIdAsync(
                    request.EnvelopeRecipientId.Value,
                    cancellationToken);

                if (recipient is null ||
                  recipient.EnvelopeId != envelopeId)
                {
                    throw new ArgumentException(
                      "The selected recipient does not belong to this envelope.");
                }

                return;
            }

            if (request.EnvelopeRecipientId.HasValue)
            {
                throw new ArgumentException(
                  "Only recipient-owned fields can have a recipient.");
            }
        }
        private async Task<Envelope> GetEditableEnvelopeAsync(
          Guid envelopeId,
          CancellationToken cancellationToken)
        {
            var envelope =
              await _envelopeRepository.GetByIdAsync(
                envelopeId,
                cancellationToken);

            if (envelope is null ||
              envelope.IsDeleted ||
              envelope.OrganizationId != _currentUser.OrganizationId ||
              envelope.CreatedByUserId != _currentUser.UserId)
            {
                throw new KeyNotFoundException(
                  "Envelope was not found.");
            }

            if (envelope.Status != EnvelopeStatus.Draft)
            {
                throw new InvalidOperationException(
                  "Fields can only be modified while the envelope is a draft.");
            }

            return envelope;
        }
        public async Task<EnvelopeFieldDto> AddEnvelopeFieldAsync(Guid envelopeId, EnvelopeFieldRequest request, CancellationToken cancellationToken =
          default)
        {
            EnsureAuthenticated();

            var envelope =
              await GetEditableEnvelopeAsync(
                envelopeId,
                cancellationToken);

            ValidateFieldRequest(request);

            var document =
              await _envelopeDocument.GetByIdAsync(
                request.EnvelopeDocumentId,
                cancellationToken);

            if (document is null ||
              document.EnvelopeId != envelopeId)
            {
                throw new ArgumentException(
                  "The selected document does not belong to this envelope.");
            }

            if (document.PageCount.HasValue &&
              request.PageNumber > document.PageCount.Value)
            {
                throw new ArgumentException(
                  "The selected page does not exist in the document.");
            }

            await ValidateFieldOwnerAsync(
              envelopeId,
              request,
              cancellationToken);

            var field = new Fields
            {
                Id = Guid.NewGuid(),

                EnvelopeId = envelope.Id,

                EnvelopeDocumentId =
                request.EnvelopeDocumentId,

                EnvelopeRecipientId =
                request.EnvelopeRecipientId,

                SourceTemplateFieldId = null,

                OwnerType =
                request.OwnerType,

                FieldType =
                request.FieldType,

                PageNumber =
                request.PageNumber,

                X = request.X,
                Y = request.Y,
                Width = request.Width,
                Height = request.Height,

                IsRequired =
                request.IsRequired,
                Label = string.IsNullOrWhiteSpace(request.Label) ? null : request.Label.Trim(),
                OptionJson = request.FieldType is FieldType.RadioButton or FieldType.Dropdown ?
                JsonSerializer.Serialize(
                  request.Options?
                  .Where(x => !string.IsNullOrWhiteSpace(x))
                  .Select(x => x.Trim())
                  .ToList() ??
                  new List<string>()) :
                null,

            };

            await _envelopeField.AddAsync(
              field,
              cancellationToken);

            envelope.UpdatedAt =
              DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync(
              cancellationToken);

            return MapField(field);
        }

        public async Task<IReadOnlyList<EnvelopeFieldDto>> GetAllEnvelopeFieldsAsync(Guid envelopeId, CancellationToken cancellationToken =
          default)
        {
            EnsureAuthenticated();

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

            var fields =
              await _envelopeField.FindAsync(
                x => x.EnvelopeId == envelopeId,
                cancellationToken);

            return fields
              .OrderBy(x => x.EnvelopeDocumentId)
              .ThenBy(x => x.PageNumber)
              .ThenBy(x => x.Y)
              .ThenBy(x => x.X)
              .Select(MapField)
              .ToList();
        }

        public async Task DeleteEnvelopeFieldAsync(Guid envelopeId, Guid fieldId, CancellationToken cancellationToken =
          default)
        {
            EnsureAuthenticated();

            var envelope =
              await GetEditableEnvelopeAsync(
                envelopeId,
                cancellationToken);

            var field =
              await _envelopeField.GetByIdAsync(
                fieldId,
                cancellationToken);

            if (field is null ||
              field.EnvelopeId != envelopeId)
            {
                throw new KeyNotFoundException(
                  "Envelope field was not found.");
            }

            _envelopeField.Delete(field);

            envelope.UpdatedAt =
              DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync(
              cancellationToken);
        }

        public async Task<EnvelopeFieldDto> UpdateEnvelopeFieldAsync(Guid envelopeId, Guid fieldId, EnvelopeFieldRequest request, CancellationToken cancellationToken =
          default)
        {
            EnsureAuthenticated();

            var envelope =
              await GetEditableEnvelopeAsync(
                envelopeId,
                cancellationToken);

            ValidateFieldRequest(request);

            var field =
              await _envelopeField.GetByIdAsync(
                fieldId,
                cancellationToken);

            if (field is null ||
              field.EnvelopeId != envelopeId)
            {
                throw new KeyNotFoundException(
                  "Envelope field was not found.");
            }

            var document =
              await _envelopeDocument.GetByIdAsync(
                request.EnvelopeDocumentId,
                cancellationToken);

            if (document is null ||
              document.EnvelopeId != envelopeId)
            {
                throw new ArgumentException(
                  "The selected document does not belong to this envelope.");
            }

            if (document.PageCount.HasValue &&
              request.PageNumber > document.PageCount.Value)
            {
                throw new ArgumentException(
                  "The selected page does not exist in the document.");
            }

            await ValidateFieldOwnerAsync(
              envelopeId,
              request,
              cancellationToken);

            field.EnvelopeDocumentId =
              request.EnvelopeDocumentId;

            field.EnvelopeRecipientId =
              request.EnvelopeRecipientId;

            field.OwnerType =
              request.OwnerType;

            field.FieldType =
              request.FieldType;

            field.PageNumber =
              request.PageNumber;

            field.X = request.X;
            field.Y = request.Y;
            field.Width = request.Width;
            field.Height = request.Height;
            field.OptionJson =
              request.FieldType is FieldType.RadioButton or FieldType.Dropdown ?
              JsonSerializer.Serialize(
                request.Options?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .ToList() ??
                new List<string>()) :
              null;

            field.IsRequired =
              request.IsRequired;
            field.Label = string.IsNullOrWhiteSpace(request.Label) ?
              null :
              request.Label.Trim();

            _envelopeField.Update(field);

            envelope.UpdatedAt =
              DateTimeOffset.UtcNow;

            await _unitOfWork.SaveChangesAsync(
              cancellationToken);

            return MapField(field);
        }
        private async Task EnsureTemplateCanBeUsedAsync(
          Template template,
          CancellationToken cancellationToken)
        {
            if (template.OwnerUserId == _currentUser.UserId)
                return;

            var currentUser =
              await _userRepository.GetByIdAsync(
                _currentUser.UserId,
                cancellationToken);

            if (currentUser is null ||
              !currentUser.IsActive)
            {
                throw new KeyNotFoundException(
                  "Template was not found.");
            }

            var shares =
              await _templateShareRepository.FindAsync(
                x => x.TemplateId == template.Id,
                cancellationToken);

            var canUse =
              shares.Any(x =>
                x.AccessLevel >= TemplateAccessLevel.Use &&
                (
                  x.SharedWithUserId ==
                  _currentUser.UserId ||
                  (
                    currentUser.DepartmentId.HasValue &&
                    x.SharedWithDepartmentId ==
                    currentUser.DepartmentId
                  )
                ));

            if (!canUse)
            {
                throw new KeyNotFoundException(
                  "Template was not found.");
            }
        }
        private async Task SnapshotTemplateDocumentsAsync(
          Envelope envelope,
          Guid templateVersionId,
          List<string> uploadedPaths,
          CancellationToken cancellationToken)
        {
            var templateDocuments =
              await _templateDocumentRepository.FindAsync(
                x => x.TemplateVersionId == templateVersionId,
                cancellationToken);

            foreach (var templateDocument in
              templateDocuments.OrderBy(x => x.Order))
            {
                await using
                var sourceStream =
                  await _fileStorage.OpenReadAsync(
                    templateDocument.BlobPath,
                    cancellationToken);

                var folder =$"envelopes/{envelope.Id}/documents";

                var blobPath =
                  await _fileStorage.UploadAsync(
                    sourceStream,
                    templateDocument.OriginalFileName,
                    templateDocument.ContentType,
                    folder,
                    cancellationToken);

                uploadedPaths.Add(blobPath);

                var document = new Documents
                {
                    Id = Guid.NewGuid(),

                    EnvelopeId = envelope.Id,

                    SourceTemplateDocumentId =
                    templateDocument.Id,

                    OriginalFileName =
                    templateDocument.OriginalFileName,

                    OriginalBlobPath =
                    blobPath,

                    FinalBlobPath = null,

                    ContentType =
                    templateDocument.ContentType,

                    FileSize =
                    templateDocument.FileSize,

                    OriginalHash =
                    templateDocument.FileHash,

                    FinalHash = null,

                    Order =
                    templateDocument.Order,

                    PageCount =
                    templateDocument.PageCount,

                    CreatedAt =
                    DateTimeOffset.UtcNow
                };

                await _envelopeDocument.AddAsync(
                  document,
                  cancellationToken);
            }
        }
        private async Task<Dictionary<Guid, Guid>>
          SnapshotTemplateRecipientsAsync(
            Envelope envelope,
            Guid templateVersionId,
            CancellationToken cancellationToken)
        {
            var roles =
              await _templateRecipientRoleRepository.FindAsync(
                x => x.TemplateVersionId == templateVersionId,
                cancellationToken);

            var defaultUserIds =
              roles
              .Where(x => x.DefaultUserId.HasValue)
              .Select(x => x.DefaultUserId!.Value)
              .Distinct()
              .ToHashSet();

            var users =
              defaultUserIds.Count == 0 ?
              Array.Empty<User>() :
              await _userRepository.FindAsync(
                x =>
                defaultUserIds.Contains(x.Id) &&
                x.OrganizationId ==
                _currentUser.OrganizationId &&
                x.IsActive,
                cancellationToken);

            var usersById =
              users.ToDictionary(x => x.Id);

            var map =
              new Dictionary<Guid,
              Guid>();

            foreach (var role in
              roles.OrderBy(x => x.RoutingOrder))
            {
                User? resolvedUser = null;

                if (role.DefaultUserId.HasValue)
                {
                    usersById.TryGetValue(
                      role.DefaultUserId.Value,
                      out resolvedUser);
                }

                var recipient = new Recipients
                {
                    Id = Guid.NewGuid(),

                    EnvelopeId = envelope.Id,

                    SourceTemplateRecipientRoleId =
                    role.Id,

                    UserId =
                    resolvedUser?.Id,

                    RoleName =
                    role.Name,

                    Name =
                    resolvedUser?.DisplayName ??
                    role.Name,

                    Email =
                    resolvedUser?.Email ??
                    string.Empty,

                    RecipientType =
                    role.RecipientType,

                    Status =
                    RecipientStatus.Created,

                    RoutingOrder =
                    role.RoutingOrder,

                    IsRequired =
                    role.IsRequired,

                    AllowSenderEditRecipient =
                    role.AllowSenderEditRecipient,

                    AllowSenderDeleteRecipient =
                    role.AllowSenderDeleteRecipient,

                    AllowSenderChangeRoutingOrder =
                    role.AllowSenderChangeRoutingOrder
                };

                await _envelopeRecipient.AddAsync(
                  recipient,
                  cancellationToken);

                map[role.Id] = recipient.Id;
            }

            return map;
        }

        private async Task<Dictionary<Guid, Guid>> SnapshotTemplateFieldsAsync(
          Envelope envelope,
          Guid templateVersionId,
          Dictionary<Guid, Guid> recipientMap,
          CancellationToken cancellationToken)
        {
            var fieldMap = new Dictionary<Guid,
              Guid>();

            var templateDocuments =
              await _templateDocumentRepository.FindAsync(
                x => x.TemplateVersionId == templateVersionId,
                cancellationToken);

            var templateDocumentIds =
              templateDocuments
              .Select(x => x.Id)
              .ToHashSet();

            var templateFields =
              await _templateFieldRepository.FindAsync(
                x => templateDocumentIds.Contains(
                  x.TemplateDocumentId),
                cancellationToken);

            var envelopeDocuments =
              await _envelopeDocument.FindAsync(
                x => x.EnvelopeId == envelope.Id,
                cancellationToken);

            var documentMap =
              envelopeDocuments
              .Where(x =>
                x.SourceTemplateDocumentId.HasValue)
              .ToDictionary(
                x => x.SourceTemplateDocumentId!.Value,
                x => x.Id);

            foreach (var templateField in templateFields)
            {
                if (!documentMap.TryGetValue(
                    templateField.TemplateDocumentId,
                    out
                    var envelopeDocumentId))
                {
                    throw new InvalidOperationException(
                      "Template document snapshot is incomplete.");
                }

                Guid? envelopeRecipientId = null;

                if (templateField.OwnerType ==
                  FieldOwnerType.Recipient)
                {
                    if (!templateField.RecipientRoleId.HasValue ||
                      !recipientMap.TryGetValue(
                        templateField.RecipientRoleId.Value,
                        out
                        var mappedRecipientId))
                    {
                        throw new InvalidOperationException(
                          "Template recipient field could not be mapped.");
                    }

                    envelopeRecipientId =
                      mappedRecipientId;
                }

                var field = new Fields
                {
                    Id = Guid.NewGuid(),

                    EnvelopeId =
                    envelope.Id,

                    EnvelopeDocumentId =
                    envelopeDocumentId,

                    EnvelopeRecipientId =
                    envelopeRecipientId,

                    SourceTemplateFieldId =
                    templateField.Id,

                    OwnerType =
                    templateField.OwnerType,

                    FieldType =
                    templateField.FieldType,

                    PageNumber =
                    templateField.PageNumber,

                    X = templateField.X,
                    Y = templateField.Y,
                    Width = templateField.Width,
                    Height = templateField.Height,

                    IsRequired =
                    templateField.IsRequired
                };
                fieldMap[templateField.Id] = field.Id;

                await _envelopeField.AddAsync(
                  field,
                  cancellationToken);
            }
            return fieldMap;
        }

        private async Task SnapshotTemplateRoutingAsync(
          Envelope envelope,
          Guid templateVersionId,
          Dictionary<Guid, Guid> recipientMap,
          Dictionary<Guid, Guid> fieldMap,
          CancellationToken cancellationToken)
        {
            var templateRuleSets =
              await _templateRoutingRuleSetRepository.FindAsync(
                x => x.TemplateVersionId == templateVersionId,
                cancellationToken);

            if (templateRuleSets.Count == 0)
                return;

            foreach (var templateRuleSet in templateRuleSets)
            {
                var envelopeRuleSet =
                  new EnvelopeRoutingRuleSet
                  {
                      Id = Guid.NewGuid(),

                      EnvelopeId = envelope.Id,

                      Name = templateRuleSet.Name,

                      IsActive = templateRuleSet.IsActive
                  };

                await _envelopeRoutingRuleSetRepository.AddAsync(
                  envelopeRuleSet,
                  cancellationToken);

                var templateRules =
                  await _templateRoutingRuleRepository.FindAsync(
                    x =>
                    x.TemplateRoutingRuleSetId ==
                    templateRuleSet.Id,
                    cancellationToken);

                foreach (var templateRule in
                  templateRules.OrderBy(x => x.Priority))
                {
                    if (!recipientMap.TryGetValue(
                        templateRule.TargetRecipientRoleId,
                        out
                        var targetRecipientId))
                    {
                        throw new InvalidOperationException(
                          "Template routing target recipient could not be mapped.");
                    }

                    var envelopeRule =
                      new EnvelopeRoutingRule
                      {
                          Id = Guid.NewGuid(),

                          EnvelopeRoutingRuleSetId =
                          envelopeRuleSet.Id,

                          TargetRecipientId =
                          targetRecipientId,

                          Name =
                          templateRule.Name,

                          MatchType =
                          templateRule.MatchType,

                          Priority =
                          templateRule.Priority,

                          IsActive =
                          templateRule.IsActive
                      };

                    await _envelopeRoutingRuleRepository.AddAsync(
                      envelopeRule,
                      cancellationToken);

                    var templateConditions =
                      await _templateRoutingConditionRepository.FindAsync(
                        x =>
                        x.TemplateRoutingRuleId ==
                        templateRule.Id,
                        cancellationToken);

                    foreach (var templateCondition in templateConditions)
                    {
                        if (!fieldMap.TryGetValue(
                            templateCondition.SourceTemplateFieldId,
                            out
                            var sourceEnvelopeFieldId))
                        {
                            throw new InvalidOperationException(
                              "Template routing source field could not be mapped.");
                        }

                        var envelopeCondition =
                          new EnvelopeRoutingCondition
                          {
                              Id = Guid.NewGuid(),

                              EnvelopeRoutingRuleId =
                              envelopeRule.Id,

                              SourceEnvelopeFieldId =
                              sourceEnvelopeFieldId,

                              Operator =
                              templateCondition.Operator,

                              ComparisonValue =
                              templateCondition.ComparisonValue
                          };

                        await _envelopeRoutingConditionRepository.AddAsync(
                          envelopeCondition,
                          cancellationToken);
                    }
                }
            }
        }
        public async Task<Guid> CreateFromTemplateAsync(Guid templateId, EnvelopeFromTemplateRequest request, CancellationToken cancellationToken =
          default)
        {
            EnsureAuthenticated();

            if (templateId == Guid.Empty)
                throw new ArgumentException(
                  "Template id is required.");

            var template =
              await _templateRepository.GetByIdForOrganizationAsync(
                templateId,
                _currentUser.OrganizationId,
                cancellationToken);

            if (template is null)
                throw new KeyNotFoundException(
                  "Template was not found.");

            await EnsureTemplateCanBeUsedAsync(
              template,
              cancellationToken);

            var publishedVersion =
              template.Versions
              .Where(x => x.IsPublished)
              .OrderByDescending(x => x.VersionNumber)
              .FirstOrDefault();

            if (publishedVersion is null)
            {
                throw new InvalidOperationException(
                  "The template does not have a published version.");
            }

            var subject =
              string.IsNullOrWhiteSpace(request.Subject) ?
              template.Name :
              request.Subject.Trim();

            if (subject.Length > 500)
                throw new ArgumentException(
                  "Envelope subject cannot exceed 500 characters.");

            if (request.Message?.Length > 4000)
                throw new ArgumentException(
                  "Envelope message cannot exceed 4000 characters.");

            var now = DateTimeOffset.UtcNow;

            var envelope = new Envelope
            {
                Id = Guid.NewGuid(),

                Subject = subject,

                Message = string.IsNullOrWhiteSpace(request.Message) ?
                null :
                request.Message.Trim(),

                Type = EnvelopeType.Standart,

                Status = EnvelopeStatus.Draft,

                OrganizationId =
                _currentUser.OrganizationId,

                CreatedByUserId =
                _currentUser.UserId,

                SourceTemplateVersionId =
                publishedVersion.Id,

                CurrentRoutingOrder = null,

                CreatedAt = now,

                UpdatedAt = now,

                IsDeleted = false
            };

            await _envelopeRepository.AddAsync(
              envelope,
              cancellationToken);

            var uploadedPaths = new List<string>();

            try
            {
                await SnapshotTemplateDocumentsAsync(
                  envelope,
                  publishedVersion.Id,
                  uploadedPaths,
                  cancellationToken);

                var recipientMap =
                  await SnapshotTemplateRecipientsAsync(
                    envelope,
                    publishedVersion.Id,
                    cancellationToken);

                var fieldMap =
                  await SnapshotTemplateFieldsAsync(
                    envelope,
                    publishedVersion.Id,
                    recipientMap,
                    cancellationToken);
                await SnapshotTemplateRoutingAsync(
                  envelope,
                  publishedVersion.Id,
                  recipientMap,
                  fieldMap,
                  cancellationToken);

                await _unitOfWork.SaveChangesAsync(
                  cancellationToken);

                return envelope.Id;
            }
            catch
            {
                foreach (var path in uploadedPaths)
                {
                    try
                    {
                        await _fileStorage.DeleteAsync(
                          path,
                          cancellationToken);
                    }
                    catch
                    {

                    }
                }

                throw;
            }
        }

        public async Task SendAsync(Guid envelopeId, CancellationToken cancellationToken =
          default)
        {
            EnsureAuthenticated();

            var envelope =
              await _envelopeRepository.GetByIdAsync(
                envelopeId,
                cancellationToken);

            if (envelope is null ||
              envelope.IsDeleted ||
              envelope.OrganizationId != _currentUser.OrganizationId ||
              envelope.CreatedByUserId != _currentUser.UserId)
            {
                throw new KeyNotFoundException(
                  "Envelope was not found.");
            }

            if (envelope.Status != EnvelopeStatus.Draft)
            {
                throw new InvalidOperationException(
                  "Only draft envelopes can be sent.");
            }

            var documents =
              await _envelopeDocument.FindAsync(
                x => x.EnvelopeId == envelopeId,
                cancellationToken);

            if (documents.Count == 0)
            {
                throw new InvalidOperationException(
                  "The envelope must contain at least one document.");
            }

            var recipients =
              await _envelopeRecipient.FindAsync(
                x => x.EnvelopeId == envelopeId,
                cancellationToken);

            if (recipients.Count == 0)
            {
                throw new InvalidOperationException(
                  "The envelope must contain at least one recipient.");
            }

            var unresolvedRecipient =
              recipients.FirstOrDefault(x =>
                !x.UserId.HasValue ||
                string.IsNullOrWhiteSpace(x.Email));

            if (unresolvedRecipient is not null)
            {
                throw new InvalidOperationException(
          $"Recipient '{unresolvedRecipient.RoleName}' must be resolved before the envelope can be sent.");
            }

            if (recipients.Any(x => x.RoutingOrder <= 0))
            {
                throw new InvalidOperationException(
                  "All recipients must have a valid routing order.");
            }

            var now = DateTimeOffset.UtcNow;

            foreach (var recipient in recipients)
            {
                recipient.Status =
                  RecipientStatus.Pending;

                _envelopeRecipient.Update(recipient);
            }

            envelope.Status =
              EnvelopeStatus.Sent;

            envelope.SentAt = now;

            envelope.UpdatedAt = now;

            envelope.CurrentRoutingOrder = null;

            await ValidateRoutingDependenciesAsync(
              envelope.Id,
              recipients,
              cancellationToken);

            await ActivateNextRoutingStepAsync(
              envelope,
              recipients,
              cancellationToken);

            var statusHistory =
              new StatusHistory
              {
                  Id = Guid.NewGuid(),

                  EnvelopeId = envelope.Id,

                  PreviousStatus =
                  EnvelopeStatus.Draft,

                  NewStatus =
                  EnvelopeStatus.Sent,

                  ChangedByUserId =
                  _currentUser.UserId,

                  Reason = "Envelope sent.",

                  ChangedAt = now
              };

            var envelopeEvent =
              new Events
              {
                  Id = Guid.NewGuid(),

                  EnvelopeId = envelope.Id,

                  EventType =
                  EnvelopeEventType.Sent,

                  UserId =
                  _currentUser.UserId,

                  RecipientId = null,

                  OccurredAt = now
              };

            await _statusHistoryRepository.AddAsync(
              statusHistory,
              cancellationToken);

            await _eventRepository.AddAsync(
              envelopeEvent,
              cancellationToken);

            await _unitOfWork.SaveChangesAsync(
              cancellationToken);
        }

        private async Task ValidateRoutingDependenciesAsync(
          Guid envelopeId,
          IReadOnlyList<Recipients> recipients,
          CancellationToken cancellationToken)
        {
            var ruleSets =
              await _envelopeRoutingRuleSetRepository.FindAsync(
                x =>
                x.EnvelopeId == envelopeId &&
                x.IsActive,
                cancellationToken);

            if (ruleSets.Count == 0)
                return;

            var ruleSetIds =
              ruleSets
              .Select(x => x.Id)
              .ToHashSet();

            var rules =
              await _envelopeRoutingRuleRepository.FindAsync(
                x =>
                ruleSetIds.Contains(
                  x.EnvelopeRoutingRuleSetId) &&
                x.IsActive,
                cancellationToken);

            if (rules.Count == 0)
                return;

            var ruleIds =
              rules
              .Select(x => x.Id)
              .ToHashSet();

            var conditions =
              await _envelopeRoutingConditionRepository.FindAsync(
                x =>
                ruleIds.Contains(
                  x.EnvelopeRoutingRuleId),
                cancellationToken);

            var fields =
              await _envelopeField.FindAsync(
                x => x.EnvelopeId == envelopeId,
                cancellationToken);

            var recipientMap =
              recipients.ToDictionary(
                x => x.Id);

            var fieldMap =
              fields.ToDictionary(
                x => x.Id);

            foreach (var rule in rules)
            {
                if (!recipientMap.TryGetValue(
                    rule.TargetRecipientId,
                    out
                    var targetRecipient))
                {
                    throw new InvalidOperationException(
                      "Routing target recipient was not found.");
                }

                var ruleConditions =
                  conditions.Where(
                    x => x.EnvelopeRoutingRuleId == rule.Id);

                foreach (var condition in ruleConditions)
                {
                    if (!fieldMap.TryGetValue(
                        condition.SourceEnvelopeFieldId,
                        out
                        var sourceField))
                    {
                        throw new InvalidOperationException(
                          "Routing source field was not found.");
                    }

                    if (sourceField.OwnerType !=
                      FieldOwnerType.Recipient)
                    {
                        continue;
                    }

                    if (!sourceField.EnvelopeRecipientId.HasValue ||
                      !recipientMap.TryGetValue(
                        sourceField.EnvelopeRecipientId.Value,
                        out
                        var sourceRecipient))
                    {
                        throw new InvalidOperationException(
                          "Routing source recipient was not found.");
                    }

                    if (sourceRecipient.RoutingOrder >=
                      targetRecipient.RoutingOrder)
                    {
                        throw new InvalidOperationException(
                          "A conditional routing field must belong to an earlier recipient.");
                    }
                }
            }
        }

        private async Task ActivateNextRoutingStepAsync(
          Envelope envelope,
          IReadOnlyList<Recipients> recipients,
          CancellationToken cancellationToken,
          IReadOnlyCollection<EnvelopeFieldValue>? pendingValues = null)
        {

            if (envelope.CurrentRoutingOrder.HasValue)
            {
                var currentOrder =
                  envelope.CurrentRoutingOrder.Value;

                var blockingRecipients =
                  recipients.Where(x =>
                    x.RoutingOrder == currentOrder &&
                    x.IsRequired &&
                    x.Status != RecipientStatus.Completed &&
                    x.Status != RecipientStatus.Skipped)
                  .ToList();

                if (blockingRecipients.Count > 0)
                    return;

                foreach (var optionalRecipient in recipients.Where(x =>
                  x.RoutingOrder == currentOrder &&
                  !x.IsRequired &&
                  x.Status != RecipientStatus.Completed &&
                  x.Status != RecipientStatus.Skipped))
                {
                    optionalRecipient.Status =
                      RecipientStatus.Skipped;

                    _envelopeRecipient.Update(
                      optionalRecipient);
                }
            }
            var pendingRecipients =
              recipients
              .Where(x =>
                x.Status == RecipientStatus.Pending)
              .OrderBy(x => x.RoutingOrder)
              .ToList();

            if (pendingRecipients.Count == 0)
            {
                envelope.CurrentRoutingOrder = null;
                return;
            }

            var ruleSets =
              await _envelopeRoutingRuleSetRepository.FindAsync(
                x =>
                x.EnvelopeId == envelope.Id &&
                x.IsActive,
                cancellationToken);

            var ruleSetIds =
              ruleSets
              .Select(x => x.Id)
              .ToHashSet();

            var rules =
              ruleSetIds.Count == 0 ?
              Array.Empty<EnvelopeRoutingRule>() :
              await _envelopeRoutingRuleRepository.FindAsync(
                x =>
                ruleSetIds.Contains(
                  x.EnvelopeRoutingRuleSetId) &&
                x.IsActive,
                cancellationToken);

            var ruleIds =
              rules
              .Select(x => x.Id)
              .ToHashSet();

            var conditions =
              ruleIds.Count == 0 ?
              Array.Empty<EnvelopeRoutingCondition>() :
              await _envelopeRoutingConditionRepository.FindAsync(
                x =>
                ruleIds.Contains(
                  x.EnvelopeRoutingRuleId),
                cancellationToken);

            var fields =
              await _envelopeField.FindAsync(
                x => x.EnvelopeId == envelope.Id,
                cancellationToken);

            var fieldIds =
              fields
              .Select(x => x.Id)
              .ToHashSet();

            var values =
              fieldIds.Count == 0 ?
              Array.Empty<EnvelopeFieldValue>() :
              await _fieldValueRepository.FindAsync(
                x =>
                fieldIds.Contains(
                  x.EnvelopeFieldId),
                cancellationToken);

            var fieldsById =
              fields.ToDictionary(x => x.Id);

            var valuesByFieldId =
              values.ToDictionary(
                x => x.EnvelopeFieldId);

            if (pendingValues is not null)
            {
                foreach (var pendingValue in pendingValues)
                {
                    valuesByFieldId[
                        pendingValue.EnvelopeFieldId] =
                      pendingValue;
                }
            }

            while (pendingRecipients.Count > 0)
            {
                var nextOrder =
                  pendingRecipients.Min(
                    x => x.RoutingOrder);

                var currentStep =
                  pendingRecipients
                  .Where(
                    x => x.RoutingOrder == nextOrder)
                  .ToList();

                var anyActivated = false;

                foreach (var recipient in currentStep)
                {
                    var recipientRules =
                      rules
                      .Where(
                        x =>
                        x.TargetRecipientId ==
                        recipient.Id)
                      .OrderBy(x => x.Priority)
                      .ToList();

                    var includeRecipient =
                      ShouldIncludeRecipient(
                        recipientRules,
                        conditions,
                        fieldsById,
                        valuesByFieldId);

                    if (includeRecipient)
                    {
                        recipient.Status =
                          RecipientStatus.Active;

                        recipient.ActivatedAt =
                          DateTimeOffset.UtcNow;

                        anyActivated = true;
                    }
                    else
                    {
                        recipient.Status =
                          RecipientStatus.Skipped;
                    }

                    _envelopeRecipient.Update(
                      recipient);
                }

                pendingRecipients.RemoveAll(
                  x => x.RoutingOrder == nextOrder);

                if (anyActivated)
                {
                    envelope.CurrentRoutingOrder =
                      nextOrder;

                    envelope.UpdatedAt =
                      DateTimeOffset.UtcNow;

                    return;
                }

                // həmin step-də hamı conditional olaraq
                // skipped oldusa növbəti order-a keçir.
            }

            envelope.CurrentRoutingOrder = null;
        }

        private static bool CompareBoolean(
          bool actual,
          string comparison,
          ConditionOperator op)
        {
            if (!bool.TryParse(
                comparison,
                out
                var expected))
            {
                return false;
            }

            return op
            switch
            {
                ConditionOperator.Equals =>
                  actual == expected,

                ConditionOperator.NotEquals =>
                actual != expected,

                _ => false
            };
        }

        private static bool CompareText(
          string actual,
          string expected,
          ConditionOperator op)
        {
            return op
            switch
            {
                ConditionOperator.Equals =>
                  string.Equals(
                    actual,
                    expected,
                    StringComparison.OrdinalIgnoreCase),

                ConditionOperator.NotEquals =>
                !string.Equals(
                  actual,
                  expected,
                  StringComparison.OrdinalIgnoreCase),

                ConditionOperator.Contains =>
                actual.Contains(
                  expected,
                  StringComparison.OrdinalIgnoreCase),

                ConditionOperator.DoesNotContain =>
                !actual.Contains(
                  expected,
                  StringComparison.OrdinalIgnoreCase),

                _ => false
            };
        }

        private static bool ShouldIncludeRecipient(
          IReadOnlyList<EnvelopeRoutingRule> rules,
          IReadOnlyList<EnvelopeRoutingCondition> conditions,
          IReadOnlyDictionary<Guid, Fields> fields,
          IReadOnlyDictionary<Guid, EnvelopeFieldValue> values)
        {
            // Heç bir conditional rule yoxdur:
            // recipient həmişə route-a daxildir.
            if (rules.Count == 0)
                return true;

            foreach (var rule in rules)
            {
                var ruleConditions =
                  conditions
                  .Where(
                    x =>
                    x.EnvelopeRoutingRuleId ==
                    rule.Id)
                  .ToList();

                // Boş rule-u match saymırıq.
                if (ruleConditions.Count == 0)
                    continue;

                var results =
                  ruleConditions
                  .Select(condition =>
                    EvaluateCondition(
                      condition,
                      fields,
                      values))
                  .ToList();

                var matched =
                  rule.MatchType ==
                  ConditionMatchType.All ?
                  results.All(x => x) :
                  results.Any(x => x);

                if (matched)
                    return true;
            }

            return false;
        }

        private static bool CompareDate(
          DateOnly actual,
          string comparison,
          ConditionOperator op)
        {
            if (!DateOnly.TryParse(
                comparison,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out
                var expected))
            {
                return false;
            }

            return op
            switch
            {
                ConditionOperator.Equals =>
                  actual == expected,

                ConditionOperator.NotEquals =>
                actual != expected,

                ConditionOperator.GreaterThan =>
                actual > expected,

                ConditionOperator.GreaterThanOrEqual =>
                actual >= expected,

                ConditionOperator.LessThan =>
                actual < expected,

                ConditionOperator.LessThanOrEqual =>
                actual <= expected,

                _ => false
            };
        }
        private static bool EvaluateCondition(
          EnvelopeRoutingCondition condition,
          IReadOnlyDictionary<Guid, Fields> fields,
          IReadOnlyDictionary<Guid, EnvelopeFieldValue> values)
        {
            if (!fields.TryGetValue(
                condition.SourceEnvelopeFieldId,
                out
                var field))
            {
                throw new InvalidOperationException(
                  "Routing source field was not found.");
            }

            values.TryGetValue(
              field.Id,
              out
              var value);

            var hasValue =
              HasFieldValue(
                field.FieldType,
                value);

            if (condition.Operator ==
              ConditionOperator.IsEmpty)
            {
                return !hasValue;
            }

            if (condition.Operator ==
              ConditionOperator.IsNotEmpty)
            {
                return hasValue;
            }

            // Claude-un rule-u:
            // empty value yalnız IsEmpty ilə match edir.
            if (!hasValue)
                return false;

            var comparison =
              condition.ComparisonValue;

            if (comparison is null)
                return false;

            return field.FieldType
            switch
            {
                FieldType.Number =>
                  CompareNumber(
                    value!.ValueNumber!.Value,
                    comparison,
                    condition.Operator),

                FieldType.DateSigned =>
                CompareDate(
                  value!.ValueDate!.Value,
                  comparison,
                  condition.Operator),

                FieldType.Checkbox =>
                CompareBoolean(
                  value!.ValueBoolean!.Value,
                  comparison,
                  condition.Operator),

                FieldType.Text or
              FieldType.Fullname or
              FieldType.Email or
              FieldType.Note or
              FieldType.RadioButton or
              FieldType.Dropdown =>
                CompareText(
                  value!.ValueText!,
                  comparison,
                  condition.Operator),

                _ => false
            };
        }

        private static bool HasFieldValue(
          FieldType fieldType,
          EnvelopeFieldValue? value)
        {
            if (value is null)
                return false;

            return fieldType
            switch
            {
                FieldType.Number =>
                  value.ValueNumber.HasValue,

                FieldType.DateSigned =>
                value.ValueDate.HasValue,

                FieldType.Checkbox =>
                value.ValueBoolean.HasValue,

                FieldType.Text or
              FieldType.Fullname or
              FieldType.Email or
              FieldType.Note or
              FieldType.RadioButton or
              FieldType.Dropdown =>
                !string.IsNullOrWhiteSpace(
                  value.ValueText),

                _ => false
            };
        }

        private static bool CompareNumber(
          decimal actual,
          string comparison,
          ConditionOperator op)
        {
            if (!decimal.TryParse(
                comparison,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out
                var expected))
            {
                return false;
            }

            return op
            switch
            {
                ConditionOperator.Equals =>
                  actual == expected,

                ConditionOperator.NotEquals =>
                actual != expected,

                ConditionOperator.GreaterThan =>
                actual > expected,

                ConditionOperator.GreaterThanOrEqual =>
                actual >= expected,

                ConditionOperator.LessThan =>
                actual < expected,

                ConditionOperator.LessThanOrEqual =>
                actual <= expected,

                _ => false
            };
        }

        public async Task FinishRecipientAsync(Guid envelopeId, Guid recipientId, FinishRecipientRequest request, CancellationToken cancellationToken =
          default)
        {
            EnsureAuthenticated();

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

            if (envelope.Status != EnvelopeStatus.Sent &&
              envelope.Status != EnvelopeStatus.InProgress)
            {
                throw new InvalidOperationException(
                  "This envelope is not available for signing.");
            }

            var recipient =
              await _envelopeRecipient.GetByIdAsync(
                recipientId,
                cancellationToken);

            if (recipient is null ||
              recipient.EnvelopeId != envelopeId ||
              recipient.UserId != _currentUser.UserId)
            {
                throw new KeyNotFoundException(
                  "Envelope recipient was not found.");
            }

            if (recipient.Status != RecipientStatus.Active &&
              recipient.Status != RecipientStatus.Viewed)
            {
                throw new InvalidOperationException(
                  "This recipient is not currently active.");
            }

            var fields =
              await _envelopeField.FindAsync(
                x =>
                x.EnvelopeId == envelopeId &&
                x.OwnerType == FieldOwnerType.Recipient &&
                x.EnvelopeRecipientId == recipientId,
                cancellationToken);

            ValidateFinishRequest(
              fields,
              request);

            var existingValues =
              await _fieldValueRepository.FindAsync(
                x => fields
                .Select(f => f.Id)
                .Contains(x.EnvelopeFieldId),
                cancellationToken);

            if (existingValues.Count > 0)
            {
                throw new InvalidOperationException(
                  "One or more recipient fields have already been completed.");
            }

            var existingMarks =
              await _appliedSignatureRepository.FindAsync(
                x =>
                x.EnvelopeId == envelopeId &&
                x.RecipientId == recipientId,
                cancellationToken);

            if (existingMarks.Count > 0)
            {
                throw new InvalidOperationException(
                  "This recipient has already applied signing evidence.");
            }

            var now = DateTimeOffset.UtcNow;

            var newValues =
              new List<EnvelopeFieldValue>();

            var uploadedMarkPaths =
              new List<string>();

            try
            {
                foreach (var field in fields)
                {
                    if (field.FieldType == FieldType.Signature ||
                      field.FieldType == FieldType.Initial ||
                      field.FieldType == FieldType.Stamp)
                    {
                        var markRequest =
                          request.AppliedMarks.FirstOrDefault(
                            x => x.FieldId == field.Id);

                        if (markRequest is null)
                        {
                            if (field.IsRequired)
                            {
                                throw new ArgumentException(
                  $"Field '{field.Id}' requires a signature or mark.");
                            }

                            continue;
                        }

                        var appliedMark =
                          await CreateAppliedMarkAsync(
                            envelope,
                            recipient,
                            field,
                            markRequest,
                            now,
                            uploadedMarkPaths,
                            cancellationToken);

                        await _appliedSignatureRepository.AddAsync(
                          appliedMark,
                          cancellationToken);

                        continue;
                    }

                    var input =
                      request.Values.FirstOrDefault(
                        x => x.FieldId == field.Id);

                    var value =
                      CreateFieldValue(
                        field,
                        recipient,
                        input,
                        now);

                    if (value is null)
                    {
                        if (field.IsRequired)
                        {
                            throw new ArgumentException(
                $"Required field '{field.Id}' has no value.");
                        }

                        continue;
                    }

                    newValues.Add(value);

                    await _fieldValueRepository.AddAsync(
                      value,
                      cancellationToken);
                }

                recipient.Status =
                  RecipientStatus.Completed;

                recipient.CompletedAt = now;

                _envelopeRecipient.Update(recipient);

                if (envelope.Status == EnvelopeStatus.Sent)
                {
                    envelope.Status =
                      EnvelopeStatus.InProgress;

                    await _statusHistoryRepository.AddAsync(
                      new StatusHistory
                      {
                          Id = Guid.NewGuid(),
                          EnvelopeId = envelope.Id,
                          PreviousStatus = EnvelopeStatus.Sent,
                          NewStatus = EnvelopeStatus.InProgress,
                          ChangedByUserId = _currentUser.UserId,
                          Reason = "Recipient completed an action.",
                          ChangedAt = now
                      },
                      cancellationToken);
                }

                var eventType =
                  recipient.RecipientType
                switch
                  {
                      RecipientType.Signer =>
                EnvelopeEventType.Signed,

                      RecipientType.Approver =>
              EnvelopeEventType.Approved,

                      _ =>
              EnvelopeEventType.Viewed
                  };

                await _eventRepository.AddAsync(
                  new Events
                  {
                      Id = Guid.NewGuid(),
                      EnvelopeId = envelope.Id,
                      RecipientId = recipient.Id,
                      UserId = _currentUser.UserId,
                      EventType = eventType,
                      OccurredAt = now
                  },
                  cancellationToken);

                var allRecipients =
                  await _envelopeRecipient.FindAsync(
                    x => x.EnvelopeId == envelopeId,
                    cancellationToken);

                var routingRecipients =
                  allRecipients.ToList();

                var currentRecipient =
                  routingRecipients.FirstOrDefault(
                    x => x.Id == recipient.Id);

                if (currentRecipient is not null)
                {
                    currentRecipient.Status =
                      RecipientStatus.Completed;

                    currentRecipient.CompletedAt = now;
                }
                await ActivateNextRoutingStepAsync(
                  envelope,
                  routingRecipients,
                  cancellationToken,
                  newValues);

                await TryCompleteEnvelopeAsync(
                  envelope,
                  routingRecipients,
                  now,
                  cancellationToken);

                envelope.UpdatedAt = now;

                await _unitOfWork.SaveChangesAsync(
                  cancellationToken);
            }
            catch
            {
                foreach (var path in uploadedMarkPaths)
                {
                    try
                    {
                        await _fileStorage.DeleteAsync(
                          path,
                          cancellationToken);
                    }
                    catch
                    {
                        // cleanup əsas xətanı gizlətməsin
                    }
                }

                throw;
            }
        }

        private async Task TryCompleteEnvelopeAsync(
          Envelope envelope,
          IReadOnlyList<Recipients> recipients,
          DateTimeOffset now,
          CancellationToken cancellationToken)
        {
            var hasIncompleteRequiredRecipient =
              recipients.Any(x =>
                x.IsRequired &&
                x.Status != RecipientStatus.Completed &&
                x.Status != RecipientStatus.Skipped);

            if (hasIncompleteRequiredRecipient)
                return;

            // Required recipient-lər bitibsə,
            // optional recipient-lər artıq flow-u saxlamamalıdır.
            foreach (var recipient in recipients.Where(x =>
              !x.IsRequired &&
              (
                x.Status == RecipientStatus.Pending ||
                x.Status == RecipientStatus.Active ||
                x.Status == RecipientStatus.Viewed
              )))
            {
                recipient.Status =
                  RecipientStatus.Skipped;

                _envelopeRecipient.Update(
                  recipient);
            }

            var previousStatus =
              envelope.Status;

            envelope.Status =
              EnvelopeStatus.Completed;

            envelope.CompletedAt =
              now;

            envelope.CurrentRoutingOrder =
              null;

            envelope.UpdatedAt =
              now;

            await _statusHistoryRepository.AddAsync(
              new StatusHistory
              {
                  Id = Guid.NewGuid(),

                  EnvelopeId =
                  envelope.Id,

                  PreviousStatus =
                  previousStatus,

                  NewStatus =
                  EnvelopeStatus.Completed,

                  ChangedByUserId =
                  _currentUser.UserId,

                  Reason =
                  "All required recipients completed their actions.",

                  ChangedAt =
                  now
              },
              cancellationToken);

            await _eventRepository.AddAsync(
              new Events
              {
                  Id = Guid.NewGuid(),

                  EnvelopeId =
                  envelope.Id,

                  EventType =
                  EnvelopeEventType.Completed,

                  UserId =
                  _currentUser.UserId,

                  RecipientId =
                  null,

                  OccurredAt =
                  now
              },
              cancellationToken);
        }

        private EnvelopeFieldValue? CreateFieldValue(
          Fields field,
          Recipients recipient,
          FinishFieldValueRequest? input,
          DateTimeOffset now)
        {
            var value = new EnvelopeFieldValue
            {
                Id = Guid.NewGuid(),
                EnvelopeFieldId = field.Id,
                SourceType = FieldOwnerType.Recipient,
                FilledByRecipientId = recipient.Id,
                FilledByUserId = _currentUser.UserId,
                FilledAt = now,

            };

            switch (field.FieldType)
            {
                case FieldType.DateSigned:
                    value.ValueDate =
                      DateOnly.FromDateTime(
                        now.UtcDateTime);

                    return value;

                case FieldType.Fullname:
                    value.ValueText =
                      string.IsNullOrWhiteSpace(
                        input?.ValueText) ?
                      recipient.Name :
                      input.ValueText.Trim();

                    return value;

                case FieldType.Email:
                    value.ValueText =
                      string.IsNullOrWhiteSpace(
                        input?.ValueText) ?
                      recipient.Email :
                      input.ValueText.Trim();

                    return value;

                case FieldType.Text:
                case FieldType.Note:
                case FieldType.RadioButton:
                case FieldType.Dropdown:

                    if (string.IsNullOrWhiteSpace(
                        input?.ValueText))
                        return null;

                    value.ValueText =
                      input.ValueText.Trim();

                    return value;

                case FieldType.Number:

                    if (!input?.ValueNumber.HasValue ?? true)
                        return null;

                    value.ValueNumber =
                      input!.ValueNumber;

                    return value;

                case FieldType.Checkbox:

                    if (!input?.ValueBoolean.HasValue ?? true)
                        return null;

                    value.ValueBoolean =
                      input!.ValueBoolean;

                    return value;

                default:
                    return null;
            }
        }

        private async Task<EnvelopeAppliedSignature> CreateAppliedMarkAsync(
          Envelope envelope,
          Recipients recipient,
          Fields field,
          FinishAppliedMarkRequest request,
          DateTimeOffset now,
          List<string> uploadedPaths,
          CancellationToken cancellationToken)
        {
            string sourcePath;
            Guid? sourceSignatureId = null;
            Guid? sourceStampId = null;
            AppliedMarkKind kind;

            if (field.FieldType == FieldType.Signature ||
              field.FieldType == FieldType.Initial)
            {
                if (!request.SignatureId.HasValue ||
                  request.StampId.HasValue)
                {
                    throw new ArgumentException(
                      "A valid signature is required.");
                }

                var signature =
                  await _signatureRepository.GetByIdAsync(
                    request.SignatureId.Value,
                    cancellationToken);

                if (signature is null ||
                  !signature.IsActive ||
                  signature.UserId != _currentUser.UserId)
                {
                    throw new ArgumentException(
                      "The selected signature is invalid.");
                }

                var expectedKind =
                  field.FieldType == FieldType.Signature ?
                  SignatureKind.Signature :
                  SignatureKind.Initials;

                if (signature.Kind != expectedKind)
                {
                    throw new ArgumentException(
                      "The selected signature kind does not match the field.");
                }

                sourcePath = signature.BlobPath;

                sourceSignatureId = signature.Id;

                kind =
                  field.FieldType == FieldType.Signature ?
                  AppliedMarkKind.Signature :
                  AppliedMarkKind.Initial;
            }
            else
            {
                if (!request.StampId.HasValue ||
                  request.SignatureId.HasValue)
                {
                    throw new ArgumentException(
                      "A valid stamp is required.");
                }

                var stamp =
                  await _stampRepository.GetByIdAsync(
                    request.StampId.Value,
                    cancellationToken);

                if (stamp is null ||
                  !stamp.IsActive)
                {
                    throw new ArgumentException(
                      "The selected stamp is invalid.");
                }

                sourcePath = stamp.BlobPath;

                sourceStampId = stamp.Id;

                kind = AppliedMarkKind.Stamp;
            }

            await using
            var sourceStream =
              await _fileStorage.OpenReadAsync(
                sourcePath,
                cancellationToken);

            var folder =
              $"envelopes/{envelope.Id}/applied-marks";

            var appliedPath =
              await _fileStorage.UploadAsync(
                sourceStream,
                Path.GetFileName(sourcePath),
                GetMarkContentType(sourcePath),
                folder,
                cancellationToken);

            uploadedPaths.Add(appliedPath);

            return new EnvelopeAppliedSignature
            {
                Id = Guid.NewGuid(),

                EnvelopeId = envelope.Id,

                EnvelopeFieldId = field.Id,

                RecipientId = recipient.Id,

                AppliedByUserId =
                _currentUser.UserId,

                Kind = kind,

                SourceSignatureId =
                sourceSignatureId,

                SourceStampId =
                sourceStampId,

                AppliedBlobPath =
                appliedPath,

                AppliedHash = null,

                AppliedAt = now
            };
        }

        private static string GetMarkContentType(
          string path)
        {
            return Path.GetExtension(path)
              .ToLowerInvariant() switch
            {
                ".jpg"
                or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                _ => "image/png"
            };
        }

        private static void ValidateFinishRequest(
          IReadOnlyList<Fields> fields,
          FinishRecipientRequest request)
        {
            var fieldIds =
              fields.Select(x => x.Id).ToHashSet();

            if (request.Values
              .GroupBy(x => x.FieldId)
              .Any(x => x.Count() > 1))
            {
                throw new ArgumentException(
                  "The same field value was supplied more than once.");
            }

            if (request.AppliedMarks
              .GroupBy(x => x.FieldId)
              .Any(x => x.Count() > 1))
            {
                throw new ArgumentException(
                  "The same applied mark was supplied more than once.");
            }

            if (request.Values.Any(
                x => !fieldIds.Contains(x.FieldId)) ||
              request.AppliedMarks.Any(
                x => !fieldIds.Contains(x.FieldId)))
            {
                throw new ArgumentException(
                  "One or more fields do not belong to this recipient.");
            }
        }

        public async Task DeclineRecipientAsync(Guid envelopeId, Guid recipientId, VoidAndDeclineEnvelopeRequest request, CancellationToken cancellationToken =
          default)
        {
            EnsureAuthenticated();

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new ArgumentException(
                  "Decline reason is required.");
            }

            var reason = request.Reason.Trim();

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

            if (envelope.Status != EnvelopeStatus.Sent &&
              envelope.Status != EnvelopeStatus.InProgress)
            {
                throw new InvalidOperationException(
                  "This envelope cannot be declined.");
            }

            var recipient =
              await _envelopeRecipient.GetByIdAsync(
                recipientId,
                cancellationToken);

            if (recipient is null ||
              recipient.EnvelopeId != envelopeId ||
              recipient.UserId != _currentUser.UserId)
            {
                throw new KeyNotFoundException(
                  "Envelope recipient was not found.");
            }

            if (recipient.Status != RecipientStatus.Active &&
              recipient.Status != RecipientStatus.Viewed)
            {
                throw new InvalidOperationException(
                  "Only an active recipient can decline the envelope.");
            }

            var now = DateTimeOffset.UtcNow;

            var previousStatus =
              envelope.Status;

            recipient.Status =
              RecipientStatus.Declined;

            recipient.DeclinedAt = now;

            recipient.DeclineReason = reason;

            _envelopeRecipient.Update(recipient);

            var recipients =
              await _envelopeRecipient.FindAsync(
                x => x.EnvelopeId == envelopeId,
                cancellationToken);

            foreach (var otherRecipient in recipients.Where(x =>
              x.Id != recipient.Id &&
              (
                x.Status == RecipientStatus.Pending ||
                x.Status == RecipientStatus.Active ||
                x.Status == RecipientStatus.Viewed ||
                x.Status == RecipientStatus.Created
              )))
            {
                otherRecipient.Status =
                  RecipientStatus.Canceled;

                otherRecipient.CanceledAt = now;

                _envelopeRecipient.Update(
                  otherRecipient);
            }

            envelope.Status =
              EnvelopeStatus.Declined;

            envelope.CurrentRoutingOrder = null;

            envelope.UpdatedAt = now;

            await _statusHistoryRepository.AddAsync(
              new StatusHistory
              {
                  Id = Guid.NewGuid(),

                  EnvelopeId = envelope.Id,

                  PreviousStatus =
                  previousStatus,

                  NewStatus =
                  EnvelopeStatus.Declined,

                  ChangedByUserId =
                  _currentUser.UserId,

                  Reason = reason,

                  ChangedAt = now
              },
              cancellationToken);

            await _eventRepository.AddAsync(
              new Events
              {
                  Id = Guid.NewGuid(),

                  EnvelopeId = envelope.Id,

                  RecipientId =
                  recipient.Id,

                  UserId =
                  _currentUser.UserId,

                  EventType =
                  EnvelopeEventType.Declined,

                  OccurredAt = now
              },
              cancellationToken);

            await _unitOfWork.SaveChangesAsync(
              cancellationToken);
        }

        public async Task VoidAsync(Guid envelopeId, VoidAndDeclineEnvelopeRequest request, CancellationToken cancellationToken =
          default)
        {
            EnsureAuthenticated();

            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                throw new ArgumentException(
                  "Void reason is required.");
            }

            var reason = request.Reason.Trim();

            if (reason.Length > 1000)
            {
                throw new ArgumentException(
                  "Void reason cannot exceed 1000 characters.");
            }

            var envelope =
              await _envelopeRepository.GetByIdAsync(
                envelopeId,
                cancellationToken);

            if (envelope is null ||
              envelope.IsDeleted ||
              envelope.OrganizationId != _currentUser.OrganizationId ||
              envelope.CreatedByUserId != _currentUser.UserId)
            {
                throw new KeyNotFoundException(
                  "Envelope was not found.");
            }

            if (envelope.Status != EnvelopeStatus.Sent &&
              envelope.Status != EnvelopeStatus.InProgress)
            {
                throw new InvalidOperationException(
                  "Only an active sent envelope can be voided.");
            }

            var now = DateTimeOffset.UtcNow;

            var previousStatus =
              envelope.Status;

            var recipients =
              await _envelopeRecipient.FindAsync(
                x => x.EnvelopeId == envelopeId,
                cancellationToken);

            foreach (var recipient in recipients.Where(x =>
              x.Status == RecipientStatus.Pending ||
              x.Status == RecipientStatus.Active ||
              x.Status == RecipientStatus.Viewed ||
              x.Status == RecipientStatus.Created))
            {
                recipient.Status =
                  RecipientStatus.Canceled;

                recipient.CanceledAt = now;

                _envelopeRecipient.Update(
                  recipient);
            }

            envelope.Status =
              EnvelopeStatus.Voided;

            envelope.VoidedAt = now;

            envelope.CurrentRoutingOrder = null;

            envelope.UpdatedAt = now;

            await _statusHistoryRepository.AddAsync(
              new StatusHistory
              {
                  Id = Guid.NewGuid(),

                  EnvelopeId =
                  envelope.Id,

                  PreviousStatus =
                  previousStatus,

                  NewStatus =
                  EnvelopeStatus.Voided,

                  ChangedByUserId =
                  _currentUser.UserId,

                  Reason = reason,

                  ChangedAt = now
              },
              cancellationToken);

            await _eventRepository.AddAsync(
              new Events
              {
                  Id = Guid.NewGuid(),

                  EnvelopeId =
                  envelope.Id,

                  RecipientId = null,

                  UserId =
                  _currentUser.UserId,

                  EventType =
                  EnvelopeEventType.Voided,

                  OccurredAt = now
              },
              cancellationToken);

            await _unitOfWork.SaveChangesAsync(
              cancellationToken);
        }

        public async Task UpdateEnvelopeDraftAsync(Guid envelopeId, EnvelopeDraftRequest request, CancellationToken cancellationToken =
          default)
        {
            if (envelopeId == Guid.Empty)
                throw new ArgumentException(
                  "Envelope id is required.");

            var envelope =
              await _envelopeRepository.GetByIdAsync(
                envelopeId,
                cancellationToken);

            if (
              envelope is null ||
              envelope.IsDeleted ||
              envelope.OrganizationId !=
              _currentUser.OrganizationId ||
              envelope.CreatedByUserId !=
              _currentUser.UserId)
            {
                throw new KeyNotFoundException(
                  "Envelope was not found.");
            }

            if (envelope.Status != EnvelopeStatus.Draft)
            {
                throw new InvalidOperationException(
                  "Only draft envelopes can be updated.");
            }

            if (string.IsNullOrWhiteSpace(
                request.Subject))
            {
                throw new ArgumentException(
                  "Subject is required.");
            }

            envelope.Subject =
              request.Subject.Trim();

            envelope.Message =
              string.IsNullOrWhiteSpace(
                request.Message) ?
              null :
              request.Message.Trim();

            envelope.ExpiresAt =
              request.ExpiresAt;

            envelope.UpdatedAt =
              DateTimeOffset.UtcNow;

            _envelopeRepository.Update(
              envelope);

            await _unitOfWork.SaveChangesAsync(
              cancellationToken);
        }

        public async Task ReOrderDocumentAsync(Guid envelopeId, ReorderEnvelopeDocumentsRequest request, CancellationToken cancellationToken =
          default)
        {
            var envelope =
              await _envelopeRepository.GetByIdAsync(
                envelopeId,
                cancellationToken);

            if (
              envelope is null ||
              envelope.IsDeleted ||
              envelope.OrganizationId !=
              _currentUser.OrganizationId ||
              envelope.CreatedByUserId !=
              _currentUser.UserId)
            {
                throw new KeyNotFoundException(
                  "Envelope was not found.");
            }

            if (envelope.Status != EnvelopeStatus.Draft)
            {
                throw new InvalidOperationException(
                  "Only draft envelope documents can be reordered.");
            }

            var documents =
              await _envelopeDocument.FindAsync(
                x => x.EnvelopeId == envelopeId,
                cancellationToken);

            if (
              request.DocumentIds.Count != documents.Count ||
              request.DocumentIds.Distinct().Count() !=
              request.DocumentIds.Count)
            {
                throw new ArgumentException(
                  "Invalid document order.");
            }

            var documentsById =
              documents.ToDictionary(x => x.Id);

            for (
              var index = 0; index < request.DocumentIds.Count; index++)
            {
                var documentId =
                  request.DocumentIds[index];

                if (!documentsById.TryGetValue(
                    documentId,
                    out
                    var document))
                {
                    throw new ArgumentException(
                      "One or more documents do not belong to this envelope.");
                }

                document.Order =
                  index + 1;

                _envelopeDocument.Update(
                  document);
            }

            envelope.UpdatedAt =
              DateTimeOffset.UtcNow;

            _envelopeRepository.Update(
              envelope);

            await _unitOfWork.SaveChangesAsync(
              cancellationToken);
        }

        public async Task<EnvelopeRoutingRuleDto> CreateRoutingRuleAsync(Guid envelopeId, EnvelopeRoutingRuleRequest request, CancellationToken cancellationToken =
          default)
        {
            var envelope = await _envelopeRepository.GetByIdAsync(
              envelopeId,
              cancellationToken);

            if (
              envelope is null ||
              envelope.IsDeleted ||
              envelope.OrganizationId !=
              _currentUser.OrganizationId ||
              envelope.CreatedByUserId !=
              _currentUser.UserId)
            {
                throw new KeyNotFoundException(
                  "Envelope was not found.");
            }

            if (request.TargetRecipientId == Guid.Empty)
            {
                throw new ArgumentException(
                  "Target recipient is required.");
            }

            if (request.Conditions.Count == 0)
            {
                throw new ArgumentException(
                  "At least one condition is required.");
            }

            var targetRecipient =
              await _envelopeRecipient.GetByIdAsync(
                request.TargetRecipientId,
                cancellationToken);

            if (
              targetRecipient is null ||
              targetRecipient.EnvelopeId != envelopeId)
            {
                throw new ArgumentException(
                  "Target recipient does not belong to this envelope.");
            }

            var conditionFields =
              new Dictionary<Guid,
              Fields>();

            foreach (var conditionRequest in request.Conditions)
            {
                var field =
                  await _envelopeField.GetByIdAsync(
                    conditionRequest.SourceEnvelopeFieldId,
                    cancellationToken);

                if (
                  field is null ||
                  field.EnvelopeId != envelopeId)
                {
                    throw new ArgumentException(
                      "Condition field does not belong to this envelope.");
                }
                var sourceRecipient =
    await _envelopeRecipient.GetByIdAsync(
        field.EnvelopeRecipientId,
        cancellationToken);

                if (
                    sourceRecipient is null ||
                    sourceRecipient.EnvelopeId != envelopeId)
                {
                    throw new ArgumentException(
                        "Condition field recipient is invalid.");
                }
                if (
    sourceRecipient.RoutingOrder >=
    targetRecipient.RoutingOrder)
                {
                    throw new ArgumentException(
                        "A routing condition must use a field from an earlier signing step.");
                }
                if (!IsOperatorAllowed(
    field.FieldType,
    conditionRequest.Operator))
                {
                    throw new ArgumentException(
                        $"Operator '{conditionRequest.Operator}' " +
                        $"is not valid for field type '{field.FieldType}'.");
                }
                if (
    RequiresComparisonValue(
        conditionRequest.Operator) &&
    string.IsNullOrWhiteSpace(
        conditionRequest.ComparisonValue))
                {
                    throw new ArgumentException(
                        "Comparison value is required for this operator.");
                }
                if (
    field.FieldType == FieldType.Number &&
    RequiresComparisonValue(
        conditionRequest.Operator) &&
    !decimal.TryParse(
        conditionRequest.ComparisonValue,
        System.Globalization.NumberStyles.Number,
        System.Globalization.CultureInfo.InvariantCulture,
        out _))
                {
                    throw new ArgumentException(
                        "Number conditions require a valid numeric comparison value.");
                }
                conditionFields[field.Id] = field;
            }

            var ruleSets =
              await _envelopeRoutingRuleSet.FindAsync(
                x => x.EnvelopeId == envelopeId,
                cancellationToken);

            var ruleSet =
              ruleSets.FirstOrDefault(
                x => x.Name == "Envelope routing");

            if (ruleSet is null)
            {
                ruleSet =
                  new EnvelopeRoutingRuleSet
                  {
                      Id = Guid.NewGuid(),

                      EnvelopeId =
                      envelopeId,

                      Name =
                      "Envelope routing",

                      IsActive =
                      true
                  };

                await _envelopeRoutingRuleSet.AddAsync(
                  ruleSet,
                  cancellationToken);
            }
            var existingRules =
    await _envelopeRoutingRule.FindAsync(
        x =>
            x.EnvelopeRoutingRuleSetId ==
            ruleSet.Id,
        cancellationToken);

            var nextPriority =
                existingRules.Count == 0
                    ? 1
                    : existingRules.Max(
                        x => x.Priority) + 1;

            var rule =
              new EnvelopeRoutingRule
              {
                  Id = Guid.NewGuid(),

                  EnvelopeRoutingRuleSetId =
                  ruleSet.Id,

                  TargetRecipientId =
                  request.TargetRecipientId,

                  Name =
                  string.IsNullOrWhiteSpace(
                    request.Name) ?
                  null :
                  request.Name.Trim(),

                  MatchType =
                  request.MatchType,

                  Priority =nextPriority,

                  IsActive =
                  true
              };

            await _envelopeRoutingRule.AddAsync(
              rule,
              cancellationToken);
            var createdConditions =
              new List<EnvelopeRoutingCondition>();

            foreach (var conditionRequest in request.Conditions)
            {
                var condition =
                  new EnvelopeRoutingCondition
                  {
                      Id = Guid.NewGuid(),

                      EnvelopeRoutingRuleId =
                      rule.Id,

                      SourceEnvelopeFieldId =
                      conditionRequest
                      .SourceEnvelopeFieldId,

                      Operator =
                      conditionRequest.Operator,

                      ComparisonValue =
                      string.IsNullOrWhiteSpace(
                        conditionRequest.ComparisonValue) ?
                      null :
                      conditionRequest
                      .ComparisonValue
                      .Trim()
                  };

                await _envelopeRoutingCondition.AddAsync(
                  condition,
                  cancellationToken);

                createdConditions.Add(
                  condition);
            }

            envelope.UpdatedAt =
              DateTimeOffset.UtcNow;

            _envelopeRepository.Update(
              envelope);

            await _unitOfWork.SaveChangesAsync(
              cancellationToken);
            return new EnvelopeRoutingRuleDto
            {
                Id =
                rule.Id,

                RuleSetId =
                ruleSet.Id,

                Name =
                rule.Name,

                TargetRecipientId =
                rule.TargetRecipientId,

                MatchType =
                rule.MatchType,

                Priority =
                rule.Priority,

                IsActive =
                rule.IsActive,

                Conditions =
                createdConditions
                .Select(x =>
                  new EnvelopeRoutingConditionDto
                  {
                      Id =
                      x.Id,

                      SourceEnvelopeFieldId =
                      x.SourceEnvelopeFieldId,

                      Operator = x.Operator,

                      ComparisonValue =x.ComparisonValue
                  })
                .ToList()
            };

        }


        public async Task<IReadOnlyList<EnvelopeRoutingRuleDto>> GetRoutingRulesAsync(
Guid envelopeId, CancellationToken cancellationToken =
default)
        {
            if (envelopeId == Guid.Empty)
            {
                throw new ArgumentException("Envelope id is required.");
            }

            var envelope = await _envelopeRepository.GetByIdAsync(
            envelopeId, cancellationToken);

            if (envelope is null || envelope.IsDeleted || envelope.OrganizationId != _currentUser.OrganizationId || envelope.CreatedByUserId != _currentUser.UserId)
            {
                throw new KeyNotFoundException("Envelope was not found.");
            }

            var ruleSets = await _envelopeRoutingRuleSet.FindAsync(
            x => x.EnvelopeId == envelopeId, cancellationToken);

            if (ruleSets.Count == 0)
            {
                return Array.Empty<EnvelopeRoutingRuleDto>();
            }

            var ruleSetIds = ruleSets.Select(x => x.Id).ToList();

            var rules = await _envelopeRoutingRule.FindAsync(
            x => ruleSetIds.Contains(
            x.EnvelopeRoutingRuleSetId), cancellationToken);

            if (rules.Count == 0)
            {
                return Array.Empty<EnvelopeRoutingRuleDto>();
            }

            var ruleIds = rules.Select(x => x.Id).ToList();

            var conditions = await _envelopeRoutingCondition.FindAsync(
            x => ruleIds.Contains(
            x.EnvelopeRoutingRuleId), cancellationToken);

            var result = rules.OrderBy(x => x.Priority).ThenBy(x => x.Name).Select(rule => new EnvelopeRoutingRuleDto
            {
                Id = rule.Id,

                RuleSetId = rule.EnvelopeRoutingRuleSetId,

                Name = rule.Name,

                TargetRecipientId = rule.TargetRecipientId,

                MatchType = rule.MatchType,

                Priority = rule.Priority,

                IsActive = rule.IsActive,

                Conditions = conditions.Where(x => x.EnvelopeRoutingRuleId == rule.Id).Select(x => new EnvelopeRoutingConditionDto
                {
                    Id = x.Id,

                    SourceEnvelopeFieldId = x.SourceEnvelopeFieldId,

                    Operator = x.Operator,

                    ComparisonValue = x.ComparisonValue
                }).ToList()
            }).ToList();

            return result;
        }

        public async Task DeleteRoutingRuleAsync(
    Guid envelopeId,
    Guid ruleId,
    CancellationToken cancellationToken = default)
        {
            if (envelopeId == Guid.Empty)
                throw new ArgumentException(
                    "Envelope id is required.");

            if (ruleId == Guid.Empty)
                throw new ArgumentException(
                    "Routing rule id is required.");

            var envelope =
                await _envelopeRepository.GetByIdAsync(
                    envelopeId,
                    cancellationToken);

            if (
                envelope is null ||
                envelope.IsDeleted ||
                envelope.OrganizationId !=
                    _currentUser.OrganizationId ||
                envelope.CreatedByUserId !=
                    _currentUser.UserId)
            {
                throw new KeyNotFoundException(
                    "Envelope was not found.");
            }

            if (envelope.Status != EnvelopeStatus.Draft)
            {
                throw new InvalidOperationException(
                    "Routing rules can only be changed for draft envelopes.");
            }

            var rule =
                await _envelopeRoutingRule.GetByIdAsync(
                    ruleId,
                    cancellationToken);

            if (rule is null)
            {
                throw new KeyNotFoundException(
                    "Routing rule was not found.");
            }

            var ruleSet =
                await _envelopeRoutingRuleSet.GetByIdAsync(
                    rule.EnvelopeRoutingRuleSetId,
                    cancellationToken);

            if (
                ruleSet is null ||
                ruleSet.EnvelopeId != envelopeId)
            {
                throw new KeyNotFoundException(
                    "Routing rule was not found.");
            }

            _envelopeRoutingRule.Delete(rule);

            envelope.UpdatedAt =
                DateTimeOffset.UtcNow;

            _envelopeRepository.Update(
                envelope);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task<EnvelopeRoutingRuleDto>UpdateRoutingRuleAsync(
        Guid envelopeId,
        Guid ruleId,
        EnvelopeRoutingRuleRequest request,
        CancellationToken cancellationToken = default)
        {
            var envelope =
                await _envelopeRepository.GetByIdAsync(
                    envelopeId,
                    cancellationToken);

            if (
                envelope is null ||
                envelope.IsDeleted ||
                envelope.OrganizationId !=
                    _currentUser.OrganizationId ||
                envelope.CreatedByUserId !=
                    _currentUser.UserId)
            {
                throw new KeyNotFoundException(
                    "Envelope was not found.");
            }

            if (envelope.Status != EnvelopeStatus.Draft)
            {
                throw new InvalidOperationException(
                    "Routing rules can only be changed for draft envelopes.");
            }

            var rule =
                await _envelopeRoutingRule.GetByIdAsync(
                    ruleId,
                    cancellationToken);

            if (rule is null)
            {
                throw new KeyNotFoundException(
                    "Routing rule was not found.");
            }

            var ruleSet =
                await _envelopeRoutingRuleSet.GetByIdAsync(
                    rule.EnvelopeRoutingRuleSetId,
                    cancellationToken);

            if (
                ruleSet is null ||
                ruleSet.EnvelopeId != envelopeId)
            {
                throw new KeyNotFoundException(
                    "Routing rule was not found.");
            }

            if (request.TargetRecipientId == Guid.Empty)
            {
                throw new ArgumentException(
                    "Target recipient is required.");
            }

            if (request.Conditions.Count == 0)
            {
                throw new ArgumentException(
                    "At least one condition is required.");
            }

            var targetRecipient =
    await _envelopeRecipient.GetByIdAsync(
        request.TargetRecipientId,
        cancellationToken);

            if (
                targetRecipient is null ||
                targetRecipient.EnvelopeId != envelopeId)
            {
                throw new ArgumentException(
                    "Target recipient does not belong to this envelope.");
            }

            foreach (var item in request.Conditions)
            {
                var field =
                    await _envelopeField.GetByIdAsync(
                        item.SourceEnvelopeFieldId,
                        cancellationToken);

                if (
                    field is null ||
                    field.EnvelopeId != envelopeId)
                {
                    throw new ArgumentException(
                        "Condition field does not belong to this envelope.");
                }
            }
            rule.Name =
    string.IsNullOrWhiteSpace(request.Name)
        ? null
        : request.Name.Trim();

            rule.TargetRecipientId =
                request.TargetRecipientId;

            rule.MatchType =
                request.MatchType;


          

            var existingConditions =
    await _envelopeRoutingCondition.FindAsync(
        x =>
            x.EnvelopeRoutingRuleId ==
            rule.Id,
        cancellationToken); _envelopeRoutingRule.Update(rule);

            foreach (var condition in existingConditions)
            {
                _envelopeRoutingCondition.Delete(
                    condition);
            }

            var newConditions =
                new List<EnvelopeRoutingCondition>();

            foreach (var item in request.Conditions)
            {
                var condition =
                    new EnvelopeRoutingCondition
                    {
                        Id =
                            Guid.NewGuid(),

                        EnvelopeRoutingRuleId =
                            rule.Id,

                        SourceEnvelopeFieldId =
                            item.SourceEnvelopeFieldId,

                        Operator =
                            item.Operator,

                        ComparisonValue =
                            string.IsNullOrWhiteSpace(
                                item.ComparisonValue)
                                ? null
                                : item.ComparisonValue.Trim()
                    };

                await _envelopeRoutingCondition.AddAsync(
                    condition,
                    cancellationToken);

                newConditions.Add(condition);
            }

            envelope.UpdatedAt =
                DateTimeOffset.UtcNow;

            _envelopeRepository.Update(
                envelope);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
            return new EnvelopeRoutingRuleDto
            {
                Id =
        rule.Id,

                RuleSetId =
        rule.EnvelopeRoutingRuleSetId,

                Name =
        rule.Name,

                TargetRecipientId =
        rule.TargetRecipientId,

                MatchType =
        rule.MatchType,

                Priority =
        rule.Priority,

                IsActive =
        rule.IsActive,

                Conditions =
        newConditions
            .Select(x =>
                new EnvelopeRoutingConditionDto
                {
                    Id =
                        x.Id,

                    SourceEnvelopeFieldId =
                        x.SourceEnvelopeFieldId,

                    Operator =
                        x.Operator,

                    ComparisonValue =
                        x.ComparisonValue
                })
            .ToList()
            };
        }

        private static bool RequiresComparisonValue(
    ConditionOperator op)
        {
            return op is not ConditionOperator.IsEmpty
                and not ConditionOperator.IsNotEmpty;
        }
        private static bool IsOperatorAllowed(
    FieldType fieldType,
    ConditionOperator op)
        {
            return fieldType switch
            {
                FieldType.Number =>
                    op is
                        ConditionOperator.Equals or
                        ConditionOperator.NotEquals or
                        ConditionOperator.GreaterThan or
                        ConditionOperator.GreaterThanOrEqual or
                        ConditionOperator.LessThan or
                        ConditionOperator.LessThanOrEqual,

                FieldType.Checkbox or
                FieldType.RadioButton or
                FieldType.Dropdown =>
                    op is
                        ConditionOperator.Equals or
                        ConditionOperator.NotEquals or
                        ConditionOperator.IsEmpty or
                        ConditionOperator.IsNotEmpty,

                FieldType.Text or
                FieldType.Fullname or
                FieldType.Email =>
                    op is
                        ConditionOperator.Equals or
                        ConditionOperator.NotEquals or
                        ConditionOperator.Contains or
                        ConditionOperator.DoesNotContain or
                        ConditionOperator.IsEmpty or
                        ConditionOperator.IsNotEmpty,

                _ => false
            };
        }
    }
}