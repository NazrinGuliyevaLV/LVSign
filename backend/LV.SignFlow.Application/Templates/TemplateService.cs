using LV.SignFlow.Application.Common.Interfaces;
using LV.SignFlow.Application.Templates.Dtos;
using LV.SignFlow.Application.Templates.Requests;
using LV.SignFlow.Domain.Entities.Envelopes;
using LV.SignFlow.Domain.Entities.Templates;
using LV.SignFlow.Domain.Entities.Users;
using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection.Emit;
using System.Text;
using System.Text.Json;
using static LV.SignFlow.Domain.Authorization.PermissionCodes;

namespace LV.SignFlow.Application.Templates
{
    public class TemplateService : ITemplateService
    {
        private readonly ITemplateRepository _templateRepository;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorage _fileStorage;
        private readonly IRepository<TemplateRecipientRole> _recipientRoleRepository;
        private readonly IRepository<TemplateDocument> _templateDocumentRepository;
        private readonly IRepository<TemplateField> _templateFieldRepository;
        private readonly IRepository<TemplateRoutingRuleSet> _templateRoutingRuleSetRepository;
        private readonly IRepository<TemplateRoutingRule> _templateRoutingRuleRepository;
        private readonly IRepository<TemplateRoutingCondition> _templateRoutingConditionRepository;
        private readonly IRepository<TemplateShare> _templateShareRepository;
        private readonly IRepository<TemplateVersion> _templateVersionRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<TemplateRoutingAction> _templateRoutingActionRepository;
        public TemplateService(ITemplateRepository templateRepository, ICurrentUser curentUser, IUnitOfWork unitOfWork, IFileStorage fileStorage, IRepository<TemplateRecipientRole> recipientRoleRepository, IRepository<TemplateDocument> templateDocumentRepository, IRepository<User> userRepository, IRepository<TemplateField> templateFieldRepository, IRepository<TemplateRoutingRuleSet> templateRoutingRuleSetRepository = null, IRepository<TemplateRoutingRule> templateRoutingRuleRepository = null, IRepository<TemplateRoutingCondition> templateRoutingConditionRepository = null, IRepository<TemplateShare> templateShareRepository = null, IRepository<TemplateVersion> templateVersionRepository = null, IRepository<TemplateRoutingAction> templateRoutingActionRepository = null)
        {
            _templateRepository = templateRepository;
            _currentUser = curentUser;
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
            _recipientRoleRepository = recipientRoleRepository;
            _templateDocumentRepository = templateDocumentRepository;
            _userRepository = userRepository;
            _templateFieldRepository = templateFieldRepository;
            _templateRoutingRuleSetRepository = templateRoutingRuleSetRepository;
            _templateRoutingRuleRepository = templateRoutingRuleRepository;
            _templateRoutingConditionRepository = templateRoutingConditionRepository;
            _templateShareRepository = templateShareRepository;
            _templateVersionRepository = templateVersionRepository;
            _templateRoutingActionRepository = templateRoutingActionRepository;
        }

        private void EnsureAuthenticated()
        {
            if (!_currentUser.IsAuthentificated)
            {
                throw new UnauthorizedAccessException("Authentification is required");
            }
        }
        public async Task<Guid> CreateAsync(TemplateRequest request, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException("Template name is required", nameof(request));
            }
           

            var now = DateTimeOffset.UtcNow;

            var template = new Template
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                OrganizationId = _currentUser.OrganizationId,
                OwnerUserId = _currentUser.UserId,
                Status = TemplateStatus.Draft,
                Category= request.Category,
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now,

               
            };
            var version = new TemplateVersion
            {
                Id = Guid.NewGuid(),
                CreatedByUserId = _currentUser.UserId,
                TemplateId = template.Id,
                Template = template,
                CreatedAt = now,
                IsPublished = false,
                VersionNumber = 1,
                ExpirationDays = request.ExpirationDays,
                ReminderIntervalDays = request.ReminderIntervalDays
            };

            template.Versions.Add(version);
            await _templateRepository.AddAsync(template, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return template.Id;
        }

        public async Task<IReadOnlyList<TemplateListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            var departmentId =
           await GetCurrentDepartmentIdAsync(
               cancellationToken);

            var templates =
                await _templateRepository.GetAccessibleAsync(
                    _currentUser.OrganizationId,
                    _currentUser.UserId,
                    departmentId,
                    cancellationToken);

            return templates
                .Select(x =>
                    MapTemplateListItem(
                        x,
                        departmentId))
                .ToList();
        }


        public async Task<TemplateDetailsDto?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var template =
                await _templateRepository.GetByIdForOrganizationAsync(
                    id,
                    _currentUser.OrganizationId,
                    cancellationToken);

            if (template is null)
                return null;

            var latestVersion = template.Versions
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefault();

            if (latestVersion is null)
            {
                throw new InvalidOperationException(
                    "The template does not contain a version.");
            }

            var dto = MapTemplateDetails(template, latestVersion);

            dto.IsOwner = template.OwnerUserId == _currentUser.UserId;

            return dto;
        }


        public async Task<IReadOnlyList<TemplateRecipientRoleDto>> GetRecipientRoleAsync(Guid templateId, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            var templates = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (templates is null) throw new KeyNotFoundException("template was not found");
            var latestVersion = templates.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();

            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var roles = await _recipientRoleRepository.FindAsync(x => x.TemplateVersionId == latestVersion.Id, cancellationToken);

            return roles.OrderBy(x => x.RoutingOrder).Select(x => new TemplateRecipientRoleDto
            {
                Id = x.Id,
                Name = x.Name,
                TemplateVersionId = x.TemplateVersionId,
                RecipientType = x.RecipientType,
                RoutingOrder = x.RoutingOrder,
                IsRequired = x.IsRequired,
                DefaultUserId = x.DefaultUserId,
                AllowSenderEditRecipient = x.AllowSenderEditRecipient,
                AllowSenderDeleteRecipient = x.AllowSenderDeleteRecipient,
                AllowSenderChangeRoutingOrder = x.AllowSenderChangeRoutingOrder
            }).ToList();
        }

        public async Task<TemplateRecipientRoleDto> AddRecipientRoleAsync(Guid templateId, TemplateRecipientRoleRequest request, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException(
                    "Recipient role name is required.");

            if (request.RoutingOrder <= 0)
                throw new ArgumentException(
                    "Routing order must be greater than zero.");

            if (!request.AllowSenderEditRecipient &&
                request.DefaultUserId is null)
            {
                throw new ArgumentException(
                    "A locked recipient must have a default user.");
            }

            var template =
      await _templateRepository.GetByIdForOrganizationAsync(
          templateId,
          _currentUser.OrganizationId,
          cancellationToken);

            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            if (template.Status != TemplateStatus.Draft)
                throw new InvalidOperationException(
                    "Only draft templates can be modified.");

            var latestVersion = template.Versions
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefault();

            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var existingRoles =
                await _recipientRoleRepository.FindAsync(
                    x => x.TemplateVersionId == latestVersion.Id,
                    cancellationToken);

            //if (existingRoles.Any(
            //    x => x.RoutingOrder == request.RoutingOrder))
            //{
            //    throw new InvalidOperationException(
            //        "Another recipient already uses this routing order.");
            //}

            if (request.DefaultUserId.HasValue)
            {
                var defaultUser =
                    await _userRepository.GetByIdAsync(
                        request.DefaultUserId.Value,
                        cancellationToken);

                if (defaultUser is null ||
                    defaultUser.OrganizationId !=
                        _currentUser.OrganizationId)
                {
                    throw new ArgumentException(
                        "The selected default user is invalid.");
                }
            }

            var role = new TemplateRecipientRole
            {
                Id = Guid.NewGuid(),

                TemplateVersionId = latestVersion.Id,

                Name = request.Name.Trim(),

                RecipientType = request.RecipientType,

                RoutingOrder = request.RoutingOrder,

                IsRequired = request.IsRequired,

                DefaultUserId = request.DefaultUserId,

                AllowSenderEditRecipient =
                    request.AllowSenderEditRecipient,

                AllowSenderDeleteRecipient =
                    request.AllowSenderDeleteRecipient,

                AllowSenderChangeRoutingOrder =
                    request.AllowSenderChangeRoutingOrder
            };

            await _recipientRoleRepository.AddAsync(
                role,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return new TemplateRecipientRoleDto
            {
                Id = role.Id,
                TemplateVersionId = role.TemplateVersionId,
                Name = role.Name,
                RecipientType = role.RecipientType,
                RoutingOrder = role.RoutingOrder,
                IsRequired = role.IsRequired,
                DefaultUserId = role.DefaultUserId,
                AllowSenderEditRecipient =
                    role.AllowSenderEditRecipient,
                AllowSenderDeleteRecipient =
                    role.AllowSenderDeleteRecipient,
                AllowSenderChangeRoutingOrder =
                    role.AllowSenderChangeRoutingOrder
            };
        }

        public async Task<TemplateDocumentDto> UploadDocumentAsync(Guid templateId, UploadTemplateDocumentRequest request, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            if (request.Stream == Stream.Null || !request.Stream.CanRead)
                throw new ArgumentException("A valid document stream is required.");

            if (request.FileSize <= 0) throw new ArgumentException("The document is empty");

            if (!string.Equals(
                Path.GetExtension(request.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Only PDF documents are currently supported");
            }
            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template == null) throw new KeyNotFoundException("Template was not found");

            var latestVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (latestVersion == null) throw new InvalidOperationException("The template does not contain a version.");

            var existingDocuments = await _templateDocumentRepository.FindAsync(x => x.TemplateVersionId == latestVersion.Id, cancellationToken);
            var nextOrder = existingDocuments.Count == 0 ? 1 : existingDocuments.Max(x => x.Order) + 1;

            var folder = $"templates/{template.Id}/{latestVersion.Id}/documents";

            string? blobPath = null;

            try
            {
                blobPath = await _fileStorage.UploadAsync(
                    request.Stream,
                    request.FileName,
                    "application/pdf",
                    folder,
                    cancellationToken);

                var document = new TemplateDocument
                {
                    Id = Guid.NewGuid(),

                    TemplateVersionId = latestVersion.Id,

                    OriginalFileName = request.FileName,

                    BlobPath = blobPath,

                    ContentType = "application/pdf",

                    FileSize = request.FileSize,

                    FileHash = null,

                    PageCount = null,

                    Order = nextOrder
                };

                await _templateDocumentRepository.AddAsync(
                    document,
                    cancellationToken);

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);

                return new TemplateDocumentDto
                {
                    Id = document.Id,
                    TemplateVersionId = document.TemplateVersionId,
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

        public async Task<IReadOnlyList<TemplateFieldDto>> GetFieldsAsync(Guid templateId, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId,_currentUser.OrganizationId, cancellationToken);
            if (template == null) throw new KeyNotFoundException("Template was not found");

            var latestVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (latestVersion == null) throw new InvalidOperationException("The template does not contain a version");

            var documents = await _templateDocumentRepository.FindAsync(x => x.TemplateVersionId == latestVersion.Id, cancellationToken);
            if (documents.Count == 0) return Array.Empty<TemplateFieldDto>();


            var documentIds = documents
                .Select(x => x.Id)
                .ToHashSet();

            var fields =
                await _templateFieldRepository.FindAsync(
                    x => documentIds.Contains(x.TemplateDocumentId),
                    cancellationToken);

            return fields
                .OrderBy(x => x.PageNumber)
                .ThenBy(x => x.Y)
                .ThenBy(x => x.X)
                .Select(x => new TemplateFieldDto
                {
                    Id = x.Id,
                    TemplateDocumentId = x.TemplateDocumentId,
                    RecipientRoleId = x.RecipientRoleId,
                    FieldType = x.FieldType,
                    PageNumber = x.PageNumber,
                    X = x.X,
                    Y = x.Y,
                    Width = x.Width,
                    Height = x.Height,
                    IsRequired = x.IsRequired,
                    Label = x.Label,
                    OwnerType=x.OwnerType,
                    Options =
    string.IsNullOrWhiteSpace(x.OptionJson)
        ? null
        : JsonSerializer.Deserialize<List<string>>(x.OptionJson),
                })
                .ToList();
        }

        public async Task<TemplateFieldDto> AddFieldsAsync(
      Guid templateId,
      TemplateFieldRequest templateFieldRequest,
      CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            if (templateFieldRequest.TemplateDocumentId == Guid.Empty)
                throw new ArgumentException(
                    "Template document is required.");

            if (templateFieldRequest.PageNumber <= 0)
                throw new ArgumentException(
                    "Page number must be greater than zero.");

            if (templateFieldRequest.OwnerType == FieldOwnerType.Recipient &&
                !templateFieldRequest.RecipientRoleId.HasValue)
            {
                throw new ArgumentException(
                    "Recipient role is required for recipient-owned fields.");
            }

            if (templateFieldRequest.OwnerType != FieldOwnerType.Recipient &&
                templateFieldRequest.RecipientRoleId.HasValue)
            {
                throw new ArgumentException(
                    "Only recipient-owned fields can have a recipient role.");
            }

            ValidateNormalizedCoordinates(
                templateFieldRequest);

            var template =
                await _templateRepository.GetByIdForOrganizationAsync(
                    templateId,
                    _currentUser.OrganizationId,
                    cancellationToken);

            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            if (template.Status != TemplateStatus.Draft)
                throw new InvalidOperationException(
                    "Only draft templates can be modified.");

            var latestVersion =
                template.Versions
                    .OrderByDescending(x => x.VersionNumber)
                    .FirstOrDefault();

            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var document =
                await _templateDocumentRepository.GetByIdAsync(
                    templateFieldRequest.TemplateDocumentId,
                    cancellationToken);

            if (document is null ||
                document.TemplateVersionId != latestVersion.Id)
            {
                throw new ArgumentException(
                    "The selected document does not belong to this template version.");
            }

            TemplateRecipientRole? recipientRole = null;

            if (templateFieldRequest.OwnerType ==
                FieldOwnerType.Recipient)
            {
                recipientRole =
                    await _recipientRoleRepository.GetByIdAsync(
                        templateFieldRequest.RecipientRoleId!.Value,
                        cancellationToken);

                if (recipientRole is null ||
                    recipientRole.TemplateVersionId != latestVersion.Id)
                {
                    throw new ArgumentException(
                        "The selected recipient role does not belong to this template version.");
                }
            }

            if (document.PageCount.HasValue &&
                templateFieldRequest.PageNumber >
                document.PageCount.Value)
            {
                throw new ArgumentException(
                    "The selected page does not exist in the document.");
            }

            var field =
                new TemplateField
                {
                    Id = Guid.NewGuid(),

                    TemplateDocumentId =
                        document.Id,

                    RecipientRoleId =
                        recipientRole?.Id,

                    OwnerType =
                        templateFieldRequest.OwnerType,

                    FieldType =
                        templateFieldRequest.FieldType,

                    Label =
                        string.IsNullOrWhiteSpace(
                            templateFieldRequest.Label)
                            ? null
                            : templateFieldRequest.Label.Trim(),

                    OptionJson =
                        templateFieldRequest.Options is { Count: > 0 }
                            ? JsonSerializer.Serialize(
                                templateFieldRequest.Options)
                            : null,

                    PageNumber =
                        templateFieldRequest.PageNumber,

                    X =
                        templateFieldRequest.X,

                    Y =
                        templateFieldRequest.Y,

                    Width =
                        templateFieldRequest.Width,

                    Height =
                        templateFieldRequest.Height,

                    IsRequired =
                        templateFieldRequest.IsRequired
                };

            await _templateFieldRepository.AddAsync(
                field,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return new TemplateFieldDto
            {
                Id =
                    field.Id,

                TemplateDocumentId =
                    field.TemplateDocumentId,

                RecipientRoleId =
                    field.RecipientRoleId,
                 

                FieldType =
                    field.FieldType,

                Label =
                    field.Label,

                Options =
                    string.IsNullOrWhiteSpace(
                        field.OptionJson)
                        ? null
                        : JsonSerializer.Deserialize<List<string>>(
                            field.OptionJson),

                PageNumber =
                    field.PageNumber,

                X =
                    field.X,

                Y =
                    field.Y,

                Width =
                    field.Width,

                Height =
                    field.Height,

                IsRequired =
                    field.IsRequired,
                OwnerType = field.OwnerType,
            };
        }
        private static void ValidateNormalizedCoordinates(
    TemplateFieldRequest request)
        {
            if (request.X < 0 || request.X > 1)
                throw new ArgumentException(
                    "X must be between 0 and 1.");

            if (request.Y < 0 || request.Y > 1)
                throw new ArgumentException(
                    "Y must be between 0 and 1.");

            if (request.Width <= 0 || request.Width > 1)
                throw new ArgumentException(
                    "Width must be greater than 0 and not exceed 1.");

            if (request.Height <= 0 || request.Height > 1)
                throw new ArgumentException(
                    "Height must be greater than 0 and not exceed 1.");

            if (request.X + request.Width > 1)
                throw new ArgumentException(
                    "The field exceeds the page width.");

            if (request.Y + request.Height > 1)
                throw new ArgumentException(
                    "The field exceeds the page height.");
        }


        public async Task<TemplateFieldDto> UpdateTemplateFieldAsync(Guid templateId, Guid fieldId, TemplateFieldRequest request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();

            if (fieldId == Guid.Empty)
                throw new ArgumentException("Field id is required.");

            if (request.TemplateDocumentId == Guid.Empty)
                throw new ArgumentException(
                    "Template document is required.");

            if (request.OwnerType == FieldOwnerType.Recipient &&
    !request.RecipientRoleId.HasValue)
            {
                throw new ArgumentException(
                    "Recipient role is required for recipient-owned fields.");
            }

            if (request.OwnerType != FieldOwnerType.Recipient &&
                request.RecipientRoleId.HasValue)
            {
                throw new ArgumentException(
                    "Only recipient-owned fields can have a recipient role.");
            }

            if (request.PageNumber <= 0)
                throw new ArgumentException(
                    "Page number must be greater than zero.");

            ValidateNormalizedCoordinates(
                request.X,
                request.Y,
                request.Width,
                request.Height);


            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template is null) throw new KeyNotFoundException("Template was not found.");


            var latestVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (latestVersion is null) throw new InvalidOperationException("The template does not contain a version.");

            var field = await _templateFieldRepository.GetByIdAsync(fieldId, cancellationToken);
            if (field is null) throw new KeyNotFoundException("Template field was not found.");

            var currentDocument = await _templateDocumentRepository.GetByIdAsync(field.TemplateDocumentId, cancellationToken);
            if (currentDocument is null || currentDocument.TemplateVersionId!=latestVersion.Id) throw new KeyNotFoundException("Template document was not found");

            var targetDocument = await _templateDocumentRepository.GetByIdAsync(field.TemplateDocumentId, cancellationToken);
            if(targetDocument is null || targetDocument.TemplateVersionId != latestVersion.Id)
            {
                throw new ArgumentException(
        "The selected document does not belong to this template version.");
            }
            TemplateRecipientRole? recipientRole = null;

            if (request.OwnerType == FieldOwnerType.Recipient)
            {
                recipientRole =
                    await _recipientRoleRepository.GetByIdAsync(
                        request.RecipientRoleId!.Value,
                        cancellationToken);

                if (recipientRole is null ||
                    recipientRole.TemplateVersionId != latestVersion.Id)
                {
                    throw new ArgumentException(
                        "The selected recipient role does not belong to this template version.");
                }
            }



            if (targetDocument.PageCount.HasValue &&
                request.PageNumber > targetDocument.PageCount.Value)
            {
                throw new ArgumentException(
                    "The selected page does not exist in the document.");
            }

            field.TemplateDocumentId =
                request.TemplateDocumentId;

            field.RecipientRoleId =
                request.RecipientRoleId;
            field.OwnerType = request.OwnerType;

            field.FieldType =
                request.FieldType;

            field.PageNumber =
                request.PageNumber;

            field.X = request.X;
            field.Y = request.Y;
            field.Width = request.Width;
            field.Height = request.Height;

            field.IsRequired =
                request.IsRequired;
            field.Label =
    string.IsNullOrWhiteSpace(request.Label)
        ? null
        : request.Label.Trim();

            field.OptionJson =
                request.Options is { Count: > 0 }
                    ? JsonSerializer.Serialize(request.Options)
                    : null;

            _templateFieldRepository.Update(field);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return new TemplateFieldDto
            {
                Id = field.Id,
                TemplateDocumentId =
                    field.TemplateDocumentId,
                RecipientRoleId =
                    field.RecipientRoleId,
                OwnerType=field.OwnerType,
                FieldType =
                    field.FieldType,
                PageNumber =
                    field.PageNumber,
                X = field.X,
                Y = field.Y,
                Width = field.Width,
                Height = field.Height,
                IsRequired =
                    field.IsRequired,
                Label =
        field.Label,

                Options =
        string.IsNullOrWhiteSpace(field.OptionJson)
            ? null
            : JsonSerializer.Deserialize<List<string>>(
                field.OptionJson),
            };
        }

        public async Task DeleteTemplateFieldAsync(Guid templateId, Guid fieldId, CancellationToken cancellationToken)
        {
            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template is null) throw new KeyNotFoundException("Template was not found.");


            var latestVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (latestVersion is null) throw new InvalidOperationException("The template does not contain a version.");

            var field = await _templateFieldRepository.GetByIdAsync(fieldId, cancellationToken);
            if (field is null) throw new KeyNotFoundException("Template field was not found.");

            var currentDocument = await _templateDocumentRepository.GetByIdAsync(field.TemplateDocumentId, cancellationToken);
            if (currentDocument is null || currentDocument.TemplateVersionId != latestVersion.Id) throw new KeyNotFoundException("Template document was not found");

            _templateFieldRepository.Delete(field);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
             
            }

        public async Task<TemplateRecipientRoleDto> UpdateTemplateRecipientRoledAsync(Guid templateId, Guid roleId, TemplateRecipientRoleRequest request, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();

            if (roleId == Guid.Empty)
                throw new ArgumentException(
                    "Recipient role id is required.");

            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException(
                    "Recipient role name is required.");

            if (request.RoutingOrder <= 0)
                throw new ArgumentException(
                    "Routing order must be greater than zero.");

            if (!request.AllowSenderEditRecipient &&
                request.DefaultUserId is null)
            {
                throw new ArgumentException(
                    "A locked recipient must have a default user.");
            }

            var template =
                await _templateRepository.GetByIdForOrganizationAsync(
                    templateId,
                    _currentUser.OrganizationId,
                    cancellationToken);

            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            if (template.Status != TemplateStatus.Draft)
                throw new InvalidOperationException(
                    "Only draft templates can be modified.");

            var latestVersion = template.Versions
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefault();

            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var role =
                await _recipientRoleRepository.GetByIdAsync(
                    roleId,
                    cancellationToken);

            if (role is null ||
                role.TemplateVersionId != latestVersion.Id)
            {
                throw new KeyNotFoundException(
                    "Recipient role was not found.");
            }

            var existingRoles =
                await _recipientRoleRepository.FindAsync(
                    x =>
                        x.TemplateVersionId == latestVersion.Id &&
                        x.Id != roleId,
                    cancellationToken);

            //if (existingRoles.Any(x => x.RoutingOrder == request.RoutingOrder))
            //{
            //    throw new InvalidOperationException(
            //        "Another recipient already uses this routing order.");
            //}

            if (request.DefaultUserId.HasValue)
            {
                var defaultUser =
                    await _userRepository.GetByIdAsync(
                        request.DefaultUserId.Value,
                        cancellationToken);

                if (defaultUser is null ||
                    !defaultUser.IsActive ||
                    defaultUser.OrganizationId !=
                        _currentUser.OrganizationId)
                {
                    throw new ArgumentException(
                        "The selected default user is invalid.");
                }
            }

            role.Name = request.Name.Trim();

            role.RecipientType =request.RecipientType;

            role.RoutingOrder = request.RoutingOrder;

            role.IsRequired =request.IsRequired;

            role.DefaultUserId =request.DefaultUserId;

            role.AllowSenderEditRecipient =request.AllowSenderEditRecipient;

            role.AllowSenderDeleteRecipient =request.AllowSenderDeleteRecipient;

            role.AllowSenderChangeRoutingOrder =request.AllowSenderChangeRoutingOrder;

            _recipientRoleRepository.Update(role);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new TemplateRecipientRoleDto
            {
                Id = role.Id,
                TemplateVersionId =
                    role.TemplateVersionId,
                Name = role.Name,
                RecipientType =
                    role.RecipientType,
                RoutingOrder =
                    role.RoutingOrder,
                IsRequired =
                    role.IsRequired,
                DefaultUserId =
                    role.DefaultUserId,
                AllowSenderEditRecipient =
                    role.AllowSenderEditRecipient,
                AllowSenderDeleteRecipient =
                    role.AllowSenderDeleteRecipient,
                AllowSenderChangeRoutingOrder =
                    role.AllowSenderChangeRoutingOrder
            };
        }

        public async Task DeleteTemplateRecipientRoleAsync(Guid templateId, Guid roleId, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template is null) throw new KeyNotFoundException("Template was not found.");


            var latestVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (latestVersion is null) throw new InvalidOperationException("The template does not contain a version.");
            var recipientRole = await _recipientRoleRepository.GetByIdAsync(roleId, cancellationToken);

            if (recipientRole is null ||
        recipientRole.TemplateVersionId != latestVersion.Id)
            {
                throw new KeyNotFoundException(
                    "Recipient role was not found.");
            }

            var assignedFields =
                await _templateFieldRepository.FindAsync(
                    x => x.RecipientRoleId == roleId,
                    cancellationToken);

            if (assignedFields.Count > 0)
            {
                throw new InvalidOperationException(
                    "This recipient cannot be deleted because one or more fields are assigned to it.");
            }
            _recipientRoleRepository.Delete(recipientRole);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        }

        private static void ValidateNormalizedCoordinates(decimal x,decimal y, decimal width,decimal height)
        {
            if (x < 0 || x > 1)
                throw new ArgumentException( "X must be between 0 and 1.");

            if (y < 0 || y > 1)
                throw new ArgumentException("Y must be between 0 and 1.");

            if (width <= 0 || width > 1)
                throw new ArgumentException("Width must be greater than 0 and not exceed 1.");

            if (height <= 0 || height > 1)
                throw new ArgumentException("Height must be greater than 0 and not exceed 1.");

            if (x + width > 1)
                throw new ArgumentException("The field exceeds the page width.");

            if (y + height > 1)
                throw new ArgumentException("The field exceeds the page height.");
        }

        public async Task<IReadOnlyList<TemplateDocumentDto>> GetDocumentAsync(Guid templateId, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            var template=await _templateRepository.GetByIdForOrganizationAsync(templateId,_currentUser.OrganizationId,cancellationToken);
            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            var latestVersion= template.Versions
    .OrderByDescending(x => x.VersionNumber)
    .FirstOrDefault();
            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var document = await _templateDocumentRepository.FindAsync(x => x.TemplateVersionId == latestVersion.Id);
            return document.OrderBy(x => x.Order).Select(x => new TemplateDocumentDto
            {
                Id = x.Id,
                OriginalFileName = x.OriginalFileName,
                TemplateVersionId = x.TemplateVersionId,
                FileSize = x.FileSize,
                ContentType = x.ContentType,
                Order = x.Order,
                PageCount = x.PageCount
            }).ToList();
        }

        public async Task<TemplateDocumentContentDto> GetDocumentContentAsync(Guid templateId, Guid documentId, CancellationToken cancellationToken)
        {
            EnsureAuthenticated();
            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            var latestVersion = template.Versions
    .OrderByDescending(x => x.VersionNumber)
    .FirstOrDefault();
            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var document = await _templateDocumentRepository.GetByIdAsync(documentId,cancellationToken);
            if (document is null ||
      document.TemplateVersionId != latestVersion.Id)
            {
                throw new KeyNotFoundException(
                    "Template document was not found.");
            }
            var stream= await _fileStorage.OpenReadAsync(document.BlobPath, cancellationToken);


            return new TemplateDocumentContentDto
            {
                Stream=stream,
                ContentType = document.ContentType,
                FileName = document.OriginalFileName
            };
        }

        public async Task DeleteDocumentAsync(Guid templateId, Guid documentId, CancellationToken cancellationToken)
        {
            EnsureAuthenticated() ;
            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            var latestVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var document = await _templateDocumentRepository.GetByIdAsync(documentId, cancellationToken);
            if (document is null ||
      document.TemplateVersionId != latestVersion.Id)
            {
                throw new KeyNotFoundException(
                    "Template document was not found.");
            }

            var fields=await _templateFieldRepository.FindAsync(x=>x.TemplateDocumentId==documentId, cancellationToken);
            if (fields.Count > 0)
            {
                throw new InvalidOperationException(
                    "This document cannot be deleted because one or more fields are placed on it.");
            }
            var blobPath = document.BlobPath;

            _templateDocumentRepository.Delete(document);
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

        public async Task<IReadOnlyList<TemplateRoutingRuleSetDto>> GetRoutingRuleSetAsync(Guid templateId, CancellationToken cancellationToken = default)
        {
            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            var latestVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");
            var route=await _templateRoutingRuleSetRepository.FindAsync(x=>x.TemplateVersionId==latestVersion.Id, cancellationToken);
            return route.OrderBy(x => x.CreatedAt).Select(x => new TemplateRoutingRuleSetDto
            {
                Id = x.Id,
                TemplateVersionId = x.TemplateVersionId,
                Name = x.Name,
                IsActive = x.IsActive,
                CreatedAt = x.CreatedAt,   
                UpdatedAt = x.UpdatedAt

            }).ToList();

        }

        public async Task<TemplateRoutingRuleSetDto> AddRoutingRuleSetAsync(Guid templateId, TemplateRoutingRuleSetRequest request, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            if (string.IsNullOrWhiteSpace(request.Name))
                throw new ArgumentException(
                    "Routing rule set name is required.");

            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            var latestVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var existingRuleSets =
                await _templateRoutingRuleSetRepository.FindAsync(
                    x => x.TemplateVersionId == latestVersion.Id,
                    cancellationToken);

            var normalizedName = request.Name.Trim();

            if (existingRuleSets.Any(
                x => x.Name.Equals(
                    normalizedName,
                    StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    "A routing rule set with this name already exists.");
            }

            var now = DateTime.UtcNow;

            var ruleSet = new TemplateRoutingRuleSet
            {
                Id = Guid.NewGuid(),

                TemplateVersionId = latestVersion.Id,

                Name = normalizedName,

                IsActive = request.IsActive,

                CreatedAt = now,

                UpdatedAt = now
            };

            await _templateRoutingRuleSetRepository.AddAsync(
                ruleSet,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return new TemplateRoutingRuleSetDto
            {
                Id = ruleSet.Id,
                TemplateVersionId =
                    ruleSet.TemplateVersionId,
                Name = ruleSet.Name,
                IsActive = ruleSet.IsActive,
                CreatedAt = ruleSet.CreatedAt,
                UpdatedAt = ruleSet.UpdatedAt
            };
        }

        public async Task<IReadOnlyList<TemplateRoutingRuleDto>> GetRoutingRuleAsync(Guid templateId, Guid ruleSetId, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            var latestVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var ruleSet = await _templateRoutingRuleSetRepository.GetByIdAsync(ruleSetId,cancellationToken);

            if (ruleSet is null || ruleSet.TemplateVersionId != latestVersion.Id)  throw new KeyNotFoundException( "Routing rule set was not found.");
           

            var rules =await _templateRoutingRuleRepository.FindAsync(x => x.TemplateRoutingRuleSetId == ruleSetId,cancellationToken);

            return rules
                .OrderBy(x => x.Priority)
                .Select(x => new TemplateRoutingRuleDto
                {
                    Id = x.Id,

                    TemplateRoutingRuleSetId =
                        x.TemplateRoutingRuleSetId,

                    TargetRecipientRoleId =
                        x.TargetRecipientRoleId,

                    Name = x.Name,

                    MatchType = x.MatchType,

                    Priority = x.Priority,

                    IsActive = x.IsActive
                })
                .ToList();
        }

        public async Task<TemplateRoutingRuleDto> AddRoutingRuleAsync(Guid templateId, Guid ruleSetId, TemplateRoutingRuleRequest request, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            if (request.TargetRecipientRoleId == Guid.Empty)
                throw new ArgumentException(
                    "Target recipient role is required.");

            if (request.Priority <= 0)
                throw new ArgumentException(
                    "Priority must be greater than zero.");

            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            var latestVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var ruleSet =
                await _templateRoutingRuleSetRepository.GetByIdAsync(
                    ruleSetId,
                    cancellationToken);

            if (ruleSet is null ||
                ruleSet.TemplateVersionId != latestVersion.Id)
            {
                throw new KeyNotFoundException(
                    "Routing rule set was not found.");
            }

            var targetRecipientRole =
                await _recipientRoleRepository.GetByIdAsync(
                    request.TargetRecipientRoleId,
                    cancellationToken);

            if (targetRecipientRole is null ||
                targetRecipientRole.TemplateVersionId != latestVersion.Id)
            {
                throw new ArgumentException(
                    "The target recipient role does not belong to this template version.");
            }

            var existingRules =
                await _templateRoutingRuleRepository.FindAsync(
                    x => x.TemplateRoutingRuleSetId == ruleSetId,
                    cancellationToken);

            if (existingRules.Any(
                x => x.Priority == request.Priority))
            {
                throw new InvalidOperationException(
                    "Another routing rule already uses this priority.");
            }

            var rule = new TemplateRoutingRule
            {
                Id = Guid.NewGuid(),

                TemplateRoutingRuleSetId = ruleSet.Id,

                TargetRecipientRoleId =
                    targetRecipientRole.Id,

                Name = string.IsNullOrWhiteSpace(request.Name)
                    ? null
                    : request.Name.Trim(),

                MatchType = request.MatchType,

                Priority = request.Priority,

                IsActive = request.IsActive
            };

            await _templateRoutingRuleRepository.AddAsync(
                rule,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return new TemplateRoutingRuleDto
            {
                Id = rule.Id,

                TemplateRoutingRuleSetId =
                    rule.TemplateRoutingRuleSetId,

                TargetRecipientRoleId =
                    rule.TargetRecipientRoleId,

                Name = rule.Name,

                MatchType = rule.MatchType,

                Priority = rule.Priority,

                IsActive = rule.IsActive
            };
        }

        public async Task<IReadOnlyList<TemplateRoutingConditionDto>> GetRoutingConditionsAsync(Guid templateId, Guid ruleSetId, Guid ruleId, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            var latestVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");
            var ruleSet = await _templateRoutingRuleSetRepository.GetByIdAsync( ruleSetId,cancellationToken);

            if (ruleSet is null || ruleSet.TemplateVersionId != latestVersion.Id)
            {
                throw new KeyNotFoundException("Routing rule set was not found.");
            }

            var rule=await _templateRoutingRuleRepository.GetByIdAsync(ruleId,cancellationToken);
            if (rule is null || rule.TemplateRoutingRuleSetId != ruleSet.Id)
            {
                throw new KeyNotFoundException("Routing rule was not found.");
            }

             var condition=await _templateRoutingConditionRepository.FindAsync(x=>x.TemplateRoutingRuleId == ruleId,cancellationToken);

            return condition.Select(x => new TemplateRoutingConditionDto
            {
                Id =x.Id,
                TemplateRoutingRuleId = x.TemplateRoutingRuleId,
                SourceTemplateFieldId = x.SourceTemplateFieldId,
                Operator =x.Operator,
                ComparisonValue = x.ComparisonValue,
            }).ToList();
        }

        public async Task<TemplateRoutingConditionDto> AddRoutingConditionsAsync(Guid templateId, Guid ruleSetId, Guid ruleId, TemplateRoutingConditionRequest request, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template is null) throw new KeyNotFoundException( "Template was not found.");

            var latestVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (latestVersion is null) throw new InvalidOperationException( "The template does not contain a version.");

            var ruleSet = await _templateRoutingRuleSetRepository.GetByIdAsync(ruleSetId, cancellationToken);
            if (ruleSet is null || ruleSet.TemplateVersionId != latestVersion.Id) throw new KeyNotFoundException("Routing rule set was not found.");
           

            var rule = await _templateRoutingRuleRepository.GetByIdAsync(ruleId, cancellationToken);
            if (rule is null || rule.TemplateRoutingRuleSetId != ruleSet.Id)  throw new KeyNotFoundException("Routing rule was not found.");
        
            var sourceField=await _templateFieldRepository.GetByIdAsync(request.SourceTemplateFieldId, cancellationToken);
            if (sourceField is null)  throw new ArgumentException("The selected source field is invalid.");

            var sourceDocument=await _templateDocumentRepository.GetByIdAsync(sourceField.TemplateDocumentId, cancellationToken);
            if (sourceDocument is null || sourceDocument.TemplateVersionId != latestVersion.Id) throw new ArgumentException("The selected source field does not belong to this template version.");


            var condition = new TemplateRoutingCondition
            {
                Id = Guid.NewGuid(),
                TemplateRoutingRuleId = rule.Id,
                SourceTemplateFieldId = sourceField.Id,
                Operator = request.Operator,
                ComparisonValue = request.ComparisonValue
            };

           await _templateRoutingConditionRepository.AddAsync(condition, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new TemplateRoutingConditionDto
            {
                Id = condition.Id,
                TemplateRoutingRuleId = condition.TemplateRoutingRuleId,
                SourceTemplateFieldId = condition.SourceTemplateFieldId,
                ComparisonValue = condition.ComparisonValue,
                Operator = condition.Operator
            };
        }

        public async Task<TemplateDetailsDto?> UpdateTemplate(Guid Id, TemplateRequest request, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                throw new ArgumentException(
                    "Template name is required.");
            }

            var template =await _templateRepository.GetByIdForOrganizationAsync(Id,_currentUser.OrganizationId, cancellationToken);
            if (template is null) throw new KeyNotFoundException("Template was not found.");

            template.Name = request.Name.Trim();

            template.Description =
                string.IsNullOrWhiteSpace(request.Description)
                    ? null
                    : request.Description.Trim();
            template.Category = request.Category;

            template.UpdatedAt = DateTime.UtcNow;
            _templateRepository.Update(template);
            await _unitOfWork.SaveChangesAsync(cancellationToken);


            var latestVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (latestVersion is null) throw new InvalidOperationException("The template does not contain a version.");
            if (latestVersion.IsPublished)
            {
                throw new InvalidOperationException(
                    "Published template versions cannot be edited.");
            }

            latestVersion.ExpirationDays =
                request.ExpirationDays;

            latestVersion.ReminderIntervalDays =
                request.ReminderIntervalDays;

            _templateVersionRepository.Update(latestVersion);
            var latestPublishedVersionNumber =
        template.Versions
            .Where(x => x.IsPublished)
            .OrderByDescending(x => x.VersionNumber)
            .Select(x => (int?)x.VersionNumber)
            .FirstOrDefault();

            return new TemplateDetailsDto
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                Status = template.Status,
                OwnerUserId = template.OwnerUserId,

                LatestVersionId =
                    latestVersion.Id,

                LatestVersionNumber =
                    latestVersion.VersionNumber,

                LatestVersionIsPublished =
                    latestVersion.IsPublished,

                HasDraftVersion =
                    template.Versions.Any(x => !x.IsPublished),

                LatestPublishedVersionNumber =
                    latestPublishedVersionNumber,

                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                Category = template.Category,
                ExpirationDays =
    latestVersion.ExpirationDays,

                ReminderIntervalDays =
    latestVersion.ReminderIntervalDays,
            };



        }

        public async Task DeleteTemplate(Guid Id, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var template = await _templateRepository.GetByIdForOrganizationAsync(Id, _currentUser.OrganizationId, cancellationToken);
            if (template is null) throw new KeyNotFoundException("Template was not found.");

            template.IsDeleted = true;
            template.DeletedAt = DateTime.UtcNow;
            template.UpdatedAt = DateTime.UtcNow;
            _templateRepository.Update(template);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        }

        public async Task<TemplateRoutingRuleSetDto> UpdateRoutingRuleSetAsync(Guid templateId, Guid ruleSetId, TemplateRoutingRuleSetRequest request, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            var latestVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");
            var normalizedName = request.Name.Trim();

            var ruleSet = await _templateRoutingRuleSetRepository.GetByIdAsync(ruleSetId, cancellationToken);

            if (ruleSet is null || ruleSet.TemplateVersionId != latestVersion.Id) throw new KeyNotFoundException("Routing rule set was not found.");


            var existingRuleSets =
                await _templateRoutingRuleSetRepository.FindAsync(
                    x =>
                        x.TemplateVersionId == latestVersion.Id &&
                        x.Id != ruleSetId,
                    cancellationToken);

            if (existingRuleSets.Any(
                x => x.Name.Equals(
                    normalizedName,
                    StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException(
                    "A routing rule set with this name already exists.");
            }

            ruleSet.Name = normalizedName;
            ruleSet.IsActive = request.IsActive;
            ruleSet.UpdatedAt = DateTime.UtcNow;

            _templateRoutingRuleSetRepository.Update(ruleSet);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return new TemplateRoutingRuleSetDto
            {
                Id = ruleSet.Id,
                TemplateVersionId = ruleSet.TemplateVersionId,
                Name = ruleSet.Name,
                IsActive = ruleSet.IsActive,
                CreatedAt = ruleSet.CreatedAt,
                UpdatedAt = ruleSet.UpdatedAt
            };


        }

        public async Task DeleteRoutingRuleSetAsync(Guid templateId, Guid ruleSetId, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var template =
                await _templateRepository.GetByIdForOrganizationAsync(
                    templateId,
                    _currentUser.OrganizationId,
                    cancellationToken);

            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            if (template.Status != TemplateStatus.Draft)
                throw new InvalidOperationException(
                    "Only draft templates can be modified.");

            var latestVersion = template.Versions
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefault();

            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");
            if (latestVersion.IsPublished)
            {
                throw new InvalidOperationException(
                    "Published template versions cannot be edited.");
            }

            var ruleSet =
                await _templateRoutingRuleSetRepository.GetByIdAsync(
                    ruleSetId,
                    cancellationToken);

            if (ruleSet is null ||
                ruleSet.TemplateVersionId != latestVersion.Id)
            {
                throw new KeyNotFoundException(
                    "Routing rule set was not found.");
            }

            //var rules =
            //    await _templateRoutingRuleRepository.FindAsync(
            //        x => x.TemplateRoutingRuleSetId == ruleSetId,
            //        cancellationToken);

            //if (rules.Count > 0)
            //{
            //    throw new InvalidOperationException(
            //        "This routing rule set cannot be deleted because it contains routing rules.");
            //}

            _templateRoutingRuleSetRepository.Delete(ruleSet);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task<TemplateRoutingRuleDto> UpdateRoutingRuleAsync(Guid templateId, Guid ruleSetId, Guid ruleId, TemplateRoutingRuleRequest request, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            if (request.TargetRecipientRoleId == Guid.Empty)
                throw new ArgumentException(
                    "Target recipient role is required.");

            if (request.Priority <= 0)
                throw new ArgumentException(
                    "Priority must be greater than zero.");

            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            var latestVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var ruleSet = await _templateRoutingRuleSetRepository.GetByIdAsync(ruleSetId, cancellationToken);

            if (ruleSet is null || ruleSet.TemplateVersionId != latestVersion.Id) throw new KeyNotFoundException("Routing rule set was not found.");


            var rule = await _templateRoutingRuleRepository.GetByIdAsync(ruleId, cancellationToken);
            if (rule is null ||
       rule.TemplateRoutingRuleSetId != ruleSetId)
            {
                throw new KeyNotFoundException(
                    "Routing rule was not found.");
            }

            var recipientRole =
                await _recipientRoleRepository.GetByIdAsync(
                    request.TargetRecipientRoleId,
                    cancellationToken);

            if (recipientRole is null ||
                recipientRole.TemplateVersionId != latestVersion.Id)
            {
                throw new ArgumentException(
                    "The target recipient role does not belong to this template version.");
            }

            var otherRules = await _templateRoutingRuleRepository.FindAsync(
                    x =>
                        x.TemplateRoutingRuleSetId == ruleSetId &&
                        x.Id != ruleId,
                    cancellationToken);

            if (otherRules.Any(
                x => x.Priority == request.Priority))
            {
                throw new InvalidOperationException(
                    "Another routing rule already uses this priority.");
            }

            rule.TargetRecipientRoleId =
                request.TargetRecipientRoleId;

            rule.Name =
                string.IsNullOrWhiteSpace(request.Name)
                    ? null
                    : request.Name.Trim();

            rule.MatchType = request.MatchType;  
            rule.Priority = request.Priority;
            rule.IsActive = request.IsActive;

            _templateRoutingRuleRepository.Update(rule);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return new TemplateRoutingRuleDto
            {
                Id = rule.Id,
                TemplateRoutingRuleSetId =
                    rule.TemplateRoutingRuleSetId,
                TargetRecipientRoleId =
                    rule.TargetRecipientRoleId,
                Name = rule.Name,
                MatchType = rule.MatchType,
                Priority = rule.Priority,
                IsActive = rule.IsActive
            };
        }

        public async Task DeleteRoutingRuleAsync(Guid templateId, Guid ruleSetId, Guid ruleId, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var template =
                await  _templateRepository.GetByIdForOrganizationAsync(
                    templateId,
                    _currentUser.OrganizationId,
                    cancellationToken);

            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            if (template.Status != TemplateStatus.Draft)
                throw new InvalidOperationException(
                    "Only draft templates can be modified.");

            var latestVersion = template.Versions
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefault();

            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var ruleSet =
                await _templateRoutingRuleSetRepository.GetByIdAsync(
                    ruleSetId,
                    cancellationToken);

            if (ruleSet is null ||
                ruleSet.TemplateVersionId != latestVersion.Id)
            {
                throw new KeyNotFoundException(
                    "Routing rule set was not found.");
            }

            var rule =
                await _templateRoutingRuleRepository.GetByIdAsync(
                    ruleId,
                    cancellationToken);

            if (rule is null ||
        rule.TemplateRoutingRuleSetId != ruleSetId)
            {
                throw new KeyNotFoundException(
                    "Routing rule was not found.");
            }


            var conditions =
                await _templateRoutingConditionRepository.FindAsync(
                    x => x.TemplateRoutingRuleId == ruleId,
                    cancellationToken);

            if (conditions.Count > 0)
            {
                throw new InvalidOperationException(
                    "This routing rule cannot be deleted because it contains routing conditions.");
            }

            _templateRoutingRuleRepository.Delete(rule);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        public async Task<TemplateRoutingConditionDto> UpdateRoutingConditionsAsync(Guid templateId, Guid ruleSetId, Guid ruleId, Guid conditionId, TemplateRoutingConditionRequest request, CancellationToken cancellationToken = default)
        {
            var template =
        await _templateRepository.GetByIdForOrganizationAsync(
            templateId,
            _currentUser.OrganizationId,
            cancellationToken);

            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            if (template.Status != TemplateStatus.Draft)
                throw new InvalidOperationException(
                    "Only draft templates can be modified.");

            var latestVersion = template.Versions
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefault();

            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var ruleSet =
                await _templateRoutingRuleSetRepository.GetByIdAsync(
                    ruleSetId,
                    cancellationToken);

            if (ruleSet is null ||
                ruleSet.TemplateVersionId != latestVersion.Id)
            {
                throw new KeyNotFoundException(
                    "Routing rule set was not found.");
            }

            var rule =
                await _templateRoutingRuleRepository.GetByIdAsync(
                    ruleId,
                    cancellationToken);

            if (rule is null ||
                rule.TemplateRoutingRuleSetId != ruleSetId)
            {
                throw new KeyNotFoundException(
                    "Routing rule was not found.");
            }

            var condition =
                await _templateRoutingConditionRepository.GetByIdAsync(
                    conditionId,
                    cancellationToken);

            if (condition is null ||
                condition.TemplateRoutingRuleId != ruleId)
            {
                throw new KeyNotFoundException(
                    "Routing condition was not found.");
            }

            var sourceField =
                await _templateFieldRepository.GetByIdAsync(
                    request.SourceTemplateFieldId,
                    cancellationToken);

            if (sourceField is null)
                throw new ArgumentException(
                    "The selected source field is invalid.");

            var sourceDocument =
                await _templateDocumentRepository.GetByIdAsync(
                    sourceField.TemplateDocumentId,
                    cancellationToken);

            if (sourceDocument is null ||
                sourceDocument.TemplateVersionId != latestVersion.Id)
            {
                throw new ArgumentException(
                    "The selected source field does not belong to this template version.");
            }

            condition.SourceTemplateFieldId =
                sourceField.Id;

            condition.Operator =
                request.Operator;

            condition.ComparisonValue =
                request.Operator == ConditionOperator.IsEmpty ||
                request.Operator == ConditionOperator.IsNotEmpty
                    ? null
                    : request.ComparisonValue?.Trim();

            _templateRoutingConditionRepository.Update(condition);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return new TemplateRoutingConditionDto
            {
                Id = condition.Id,
                TemplateRoutingRuleId =
                    condition.TemplateRoutingRuleId,
                SourceTemplateFieldId =
                    condition.SourceTemplateFieldId,
                Operator = condition.Operator,
                ComparisonValue =
                    condition.ComparisonValue
            };
        }

        public async Task DeleteRoutingConditionsAsync(Guid templateId, Guid ruleSetId, Guid ruleId, Guid conditionId, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var template =
                await _templateRepository.GetByIdForOrganizationAsync(
                    templateId,
                    _currentUser.OrganizationId,
                    cancellationToken);

            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            if (template.Status != TemplateStatus.Draft)
                throw new InvalidOperationException(
                    "Only draft templates can be modified.");

            var latestVersion = template.Versions
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefault();

            if (latestVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var ruleSet =
                await _templateRoutingRuleSetRepository.GetByIdAsync(
                    ruleSetId,
                    cancellationToken);

            if (ruleSet is null ||
                ruleSet.TemplateVersionId != latestVersion.Id)
            {
                throw new KeyNotFoundException(
                    "Routing rule set was not found.");
            }

            var rule =
                await _templateRoutingRuleRepository.GetByIdAsync(
                    ruleId,
                    cancellationToken);

            if (rule is null ||
                rule.TemplateRoutingRuleSetId != ruleSetId)
            {
                throw new KeyNotFoundException(
                    "Routing rule was not found.");
            }

            var condition =
                await _templateRoutingConditionRepository.GetByIdAsync(
                    conditionId,
                    cancellationToken);

            if (condition is null ||
                condition.TemplateRoutingRuleId != ruleId)
            {
                throw new KeyNotFoundException(
                    "Routing condition was not found.");
            }

            _templateRoutingConditionRepository.Delete(condition);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }


        private static TemplateAccessLevel? CalculateEffectiveAccessLevel( Template template, Guid currentUserId, Guid? departmentId)
        {
            if (template.OwnerUserId == currentUserId)
            {
                return TemplateAccessLevel.Manage;
            }

            var accessLevels = template.Shares
                .Where(x =>x.SharedWithUserId == currentUserId || (departmentId.HasValue &&  x.SharedWithDepartmentId == departmentId.Value)).Select(x => (int)x.AccessLevel).ToList();

            if (accessLevels.Count == 0) return null;

            return (TemplateAccessLevel)accessLevels.Max();
        }

        private async Task<Guid?> GetCurrentDepartmentIdAsync(CancellationToken cancellationToken)
        {
            var user = await _userRepository.GetByIdAsync(_currentUser.UserId, cancellationToken);
            if (user == null) throw new InvalidOperationException("Current user was not found");
            return user.DepartmentId;
        }

        private TemplateListItemDto MapTemplateListItem(Template template,Guid? departmentId)
        {
            var latestVersion=template.Versions.OrderByDescending(x=>x.VersionNumber).First();

            var effectiveAccess = CalculateEffectiveAccessLevel(template, _currentUser.UserId, departmentId);
            if(!effectiveAccess.HasValue) throw new InvalidOperationException(
            "Accessible template has no effective access.");

            return new TemplateListItemDto
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                Status = template.Status,
                LatestVersionNumber =latestVersion.VersionNumber,

                LatestVersionIsPublished =latestVersion.IsPublished,

                HasDraftVersion =
            template.Versions.Any(
                x => !x.IsPublished),

                IsOwner =template.OwnerUserId ==  _currentUser.UserId,

                EffectiveAccessLevel =effectiveAccess.Value,

                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                Category=template.Category,
            };

        }
        public async Task<IReadOnlyList<TemplateListItemDto>> GetSharedWithMeAsync(CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var departmentId =
                await GetCurrentDepartmentIdAsync(
                    cancellationToken);

            var templates =
                await _templateRepository.GetSharedByUserAsync(
                    _currentUser.OrganizationId,
                    _currentUser.UserId,
                    departmentId,
                    cancellationToken);

            return templates
                .Select(x =>
                    MapTemplateListItem(
                        x,
                        departmentId))
                .ToList();
        }

        public async Task<IReadOnlyList<TemplateListItemDto>> GetMyTemplatesAsync(CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var templates =
                await _templateRepository.GetOwnedByUserAsync(
                    _currentUser.OrganizationId,
                    _currentUser.UserId,
                    cancellationToken);

            return templates
                .Select(x =>
                    MapTemplateListItem(
                        x,
                        null))
                .ToList();
        }

        public async Task<IReadOnlyList<TemplateShareDto>> GetSharesAsync(Guid templateId, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var template =
                await _templateRepository.GetByIdForOrganizationAsync(templateId,_currentUser.OrganizationId,cancellationToken);

            if (template is null)
                throw new KeyNotFoundException();
            var shares =await _templateShareRepository.FindAsync(x => x.TemplateId == templateId,cancellationToken);
            return shares
       .Select(x => new TemplateShareDto
       {
           Id = x.Id,
           TemplateId = x.TemplateId,
           SharedWithUserId = x.SharedWithUserId,
           SharedWithDepartmentId =
               x.SharedWithDepartmentId,
           AccessLevel = x.AccessLevel,
           SharedByUserId = x.SharedByUserId,
           SharedAt = x.SharedAt
       })
       .ToList();
        }

        public async Task<TemplateShareDto> AddShareAsync(Guid templateId, TemplateShareRequest request, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var template =
                await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);

            if (template is null)
                throw new KeyNotFoundException();
            var existingShares =
      await _templateShareRepository.FindAsync(
          x =>
              x.TemplateId == templateId &&
              (
                  x.SharedWithUserId ==
                      request.SharedWithUserId ||
                  x.SharedWithDepartmentId ==
                      request.SharedWithDepartmentId
              ),
          cancellationToken);

            if (existingShares.Count > 0)
                throw new InvalidOperationException(
                    "This template is already shared with this target.");

            var share = new TemplateShare
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                SharedWithUserId =
                    request.SharedWithUserId,
                SharedWithDepartmentId =
                    request.SharedWithDepartmentId,
                AccessLevel = request.AccessLevel,
                SharedByUserId = _currentUser.UserId,
                SharedAt = DateTimeOffset.UtcNow
            };

            await _templateShareRepository.AddAsync(
                share,
                cancellationToken);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return new TemplateShareDto
            {
                Id = share.Id,
                TemplateId = share.TemplateId,
                SharedWithUserId =
                    share.SharedWithUserId,
                SharedWithDepartmentId =
                    share.SharedWithDepartmentId,
                AccessLevel = share.AccessLevel,
                SharedByUserId =
                    share.SharedByUserId,
                SharedAt = share.SharedAt
            };
        }

        public async Task<TemplateShareDto> UpdateShareAsync(Guid templateId, Guid shareId, TemplateShareRequest request, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var template =
                await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);

            if (template is null)
                throw new KeyNotFoundException();
            var share =
           await _templateShareRepository.GetByIdAsync(
               shareId,
               cancellationToken);

            if (share is null ||
                share.TemplateId != templateId)
            {
                throw new KeyNotFoundException();
            }

            share.SharedWithUserId =
                request.SharedWithUserId;

            share.SharedWithDepartmentId =
                request.SharedWithDepartmentId;

            share.AccessLevel =
                request.AccessLevel;

            _templateShareRepository.Update(share);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return new TemplateShareDto
            {
                Id = share.Id,
                TemplateId = share.TemplateId,
                SharedWithUserId =
                    share.SharedWithUserId,
                SharedWithDepartmentId =
                    share.SharedWithDepartmentId,
                AccessLevel = share.AccessLevel,
                SharedByUserId =
                    share.SharedByUserId,
                SharedAt = share.SharedAt
            };
        }

        public async Task DeleteShareAsync(Guid templateId, Guid shareId, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var share =
                await _templateShareRepository.GetByIdAsync(
                    shareId,
                    cancellationToken);

            if (share is null ||
                share.TemplateId != templateId)
            {
                throw new KeyNotFoundException();
            }

            _templateShareRepository.Delete(share);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);
        }

        private async Task ValidateTemplateRoutingForPublishAsync(
            Guid templateVersionId,
            CancellationToken cancellationToken)
        {
            var ruleSets =
                await _templateRoutingRuleSetRepository.FindAsync(
                    x => x.TemplateVersionId == templateVersionId &&
                         x.IsActive,
                    cancellationToken);

            if (ruleSets.Count == 0)
                return;

            var versionRoles =
                await _recipientRoleRepository.FindAsync(
                    x => x.TemplateVersionId == templateVersionId,
                    cancellationToken);

            var roleIds = versionRoles
                .Select(x => x.Id)
                .ToHashSet();

            var documents =
                await _templateDocumentRepository.FindAsync(
                    x => x.TemplateVersionId == templateVersionId,
                    cancellationToken);

            var documentIds = documents
                .Select(x => x.Id)
                .ToHashSet();

            var fields =
                await _templateFieldRepository.FindAsync(
                    x => documentIds.Contains(x.TemplateDocumentId),
                    cancellationToken);

            var fieldIds = fields
                .Select(x => x.Id)
                .ToHashSet();

            foreach (var ruleSet in ruleSets)
            {
                var allRules =
                    await _templateRoutingRuleRepository.FindAsync(
                        x => x.TemplateRoutingRuleSetId == ruleSet.Id,
                        cancellationToken);

                var rules = allRules
                    .Where(x => x.IsActive)
                    .ToList();

                if (rules.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Routing rule set '{ruleSet.Name}' has no active rules.");
                }

                if (rules.Select(x => x.Priority).Distinct().Count()
                    != rules.Count)
                {
                    throw new InvalidOperationException(
                        $"Routing rule set '{ruleSet.Name}' has duplicate priorities.");
                }

                foreach (var rule in rules)
                {
                    if (!roleIds.Contains(rule.TargetRecipientRoleId))
                    {
                        throw new InvalidOperationException(
                            $"Routing rule '{rule.Name}' has an invalid target recipient.");
                    }

                    if (rule.Priority <= 0)
                    {
                        throw new InvalidOperationException(
                            $"Routing rule '{rule.Name}' has an invalid priority.");
                    }

                    var conditions =
                        await _templateRoutingConditionRepository.FindAsync(
                            x => x.TemplateRoutingRuleId == rule.Id,
                            cancellationToken);

                    if (conditions.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"Routing rule '{rule.Name}' has no conditions.");
                    }

                    foreach (var condition in conditions)
                    {
                        if (!fieldIds.Contains(condition.SourceTemplateFieldId))
                        {
                            throw new InvalidOperationException(
                                $"Routing rule '{rule.Name}' references an invalid field.");
                        }

                        if (!Enum.IsDefined(condition.Operator))
                        {
                            throw new InvalidOperationException(
                                $"Routing rule '{rule.Name}' has an invalid operator.");
                        }

                        var requiresValue =
                            condition.Operator != ConditionOperator.IsEmpty &&
                            condition.Operator != ConditionOperator.IsNotEmpty;

                        if (requiresValue &&
                            string.IsNullOrWhiteSpace(condition.ComparisonValue))
                        {
                            throw new InvalidOperationException(
                                $"Routing rule '{rule.Name}' has a missing comparison value.");
                        }
                    }

                    var actions =
                        await _templateRoutingActionRepository.FindAsync(
                            x => x.TemplateRoutingRuleId == rule.Id,
                            cancellationToken);

                    if (actions.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"Routing rule '{rule.Name}' has no recipient actions.");
                    }

                    if (actions.Any(x => !roleIds.Contains(x.RecipientRoleId)))
                    {
                        throw new InvalidOperationException(
                            $"Routing rule '{rule.Name}' references an invalid action recipient.");
                    }

                    if (actions.Any(x =>
                            x.Order <= 0 ||
                            !Enum.IsDefined(x.ActionType)))
                    {
                        throw new InvalidOperationException(
                            $"Routing rule '{rule.Name}' has invalid action settings.");
                    }

                    if (actions.Select(x => x.RecipientRoleId).Distinct().Count()
                        != actions.Count)
                    {
                        throw new InvalidOperationException(
                            $"Routing rule '{rule.Name}' contains duplicate recipient actions.");
                    }

                    if (actions.Select(x => x.Order).Distinct().Count()
                        != actions.Count)
                    {
                        throw new InvalidOperationException(
                            $"Routing rule '{rule.Name}' contains duplicate action orders.");
                    }
                }
            }
        }


        private static bool IsRoutingOperatorAllowed(
            FieldType fieldType,
            ConditionOperator op)
        {
            return fieldType switch
            {
                FieldType.Number =>
                    op is
                        ConditionOperator.Equals or
                        ConditionOperator.NotEquals or
                        ConditionOperator.IsEmpty or
                        ConditionOperator.IsNotEmpty or
                        ConditionOperator.GreaterThan or
                        ConditionOperator.GreaterThanOrEqual or
                        ConditionOperator.LessThan or
                        ConditionOperator.LessThanOrEqual,

                FieldType.Text =>
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

        private static ConditionOperator GetInverseOperator(
            ConditionOperator op)
        {
            return op switch
            {
                ConditionOperator.Equals =>
                    ConditionOperator.NotEquals,

                ConditionOperator.NotEquals =>
                    ConditionOperator.Equals,

                ConditionOperator.Contains =>
                    ConditionOperator.DoesNotContain,

                ConditionOperator.DoesNotContain =>
                    ConditionOperator.Contains,

                ConditionOperator.IsEmpty =>
                    ConditionOperator.IsNotEmpty,

                ConditionOperator.IsNotEmpty =>
                    ConditionOperator.IsEmpty,

                ConditionOperator.GreaterThan =>
                    ConditionOperator.LessThanOrEqual,

                ConditionOperator.GreaterThanOrEqual =>
                    ConditionOperator.LessThan,

                ConditionOperator.LessThan =>
                    ConditionOperator.GreaterThanOrEqual,

                ConditionOperator.LessThanOrEqual =>
                    ConditionOperator.GreaterThan,

                _ => throw new InvalidOperationException(
                    "Unsupported routing operator.")
            };
        }


        private async Task ValidateFieldRoutingBranchesAsync(
            Guid templateVersionId,
            CancellationToken cancellationToken)
        {
            const string prefix = "Field routing: ";

            var ruleSets =
                await _templateRoutingRuleSetRepository.FindAsync(
                    x => x.TemplateVersionId == templateVersionId,
                    cancellationToken);

            var documents =
                await _templateDocumentRepository.FindAsync(
                    x => x.TemplateVersionId == templateVersionId,
                    cancellationToken);

            var documentIds = documents.Select(x => x.Id).ToHashSet();

            var fields =
                await _templateFieldRepository.FindAsync(
                    x => documentIds.Contains(x.TemplateDocumentId),
                    cancellationToken);

            var fieldsById = fields.ToDictionary(x => x.Id);

            var roles =
                await _recipientRoleRepository.FindAsync(
                    x => x.TemplateVersionId == templateVersionId,
                    cancellationToken);

            var roleIds = roles.Select(x => x.Id).ToHashSet();

            foreach (var ruleSet in ruleSets.Where(
                x => x.IsActive && x.Name.StartsWith(
                    prefix, StringComparison.Ordinal)))
            {
                if (!Guid.TryParse(
                        ruleSet.Name[prefix.Length..],
                        out var sourceFieldId) ||
                    !fieldsById.TryGetValue(sourceFieldId, out var sourceField))
                {
                    throw new InvalidOperationException(
                        $"Routing rule set '{ruleSet.Name}' has an invalid source field.");
                }

                var rules =
                    await _templateRoutingRuleRepository.FindAsync(
                        x => x.TemplateRoutingRuleSetId == ruleSet.Id,
                        cancellationToken);

                if (rules.Count != 2 ||
                    rules.Any(x => !x.IsActive) ||
                    rules.Count(x => x.Priority == 1) != 1 ||
                    rules.Count(x => x.Priority == 2) != 1)
                {
                    throw new InvalidOperationException(
                        $"Routing '{ruleSet.Name}' must contain exactly two active branches.");
                }

                var thenRule = rules.Single(x => x.Priority == 1);
                var elseRule = rules.Single(x => x.Priority == 2);

                var thenConditions =
                    await _templateRoutingConditionRepository.FindAsync(
                        x => x.TemplateRoutingRuleId == thenRule.Id,
                        cancellationToken);

                var elseConditions =
                    await _templateRoutingConditionRepository.FindAsync(
                        x => x.TemplateRoutingRuleId == elseRule.Id,
                        cancellationToken);

                if (thenConditions.Count != 1 || elseConditions.Count != 1)
                {
                    throw new InvalidOperationException(
                        "IF and ELSE must each contain exactly one condition.");
                }

                var ifCondition = thenConditions.Single();
                var elseCondition = elseConditions.Single();

                if (ifCondition.SourceTemplateFieldId != sourceFieldId ||
                    elseCondition.SourceTemplateFieldId != sourceFieldId)
                {
                    throw new InvalidOperationException(
                        "IF and ELSE must reference the configured source field.");
                }

                if (!IsRoutingOperatorAllowed(
                        sourceField.FieldType, ifCondition.Operator) ||
                    !IsRoutingOperatorAllowed(
                        sourceField.FieldType, elseCondition.Operator))
                {
                    throw new InvalidOperationException(
                        "Routing operator is not supported for the selected field type.");
                }

                if (elseCondition.Operator !=
                    GetInverseOperator(ifCondition.Operator))
                {
                    throw new InvalidOperationException(
                        "ELSE condition must be the inverse of IF.");
                }

                var isEmptyOperator =
                    ifCondition.Operator is
                        ConditionOperator.IsEmpty or
                        ConditionOperator.IsNotEmpty;

                if (!isEmptyOperator &&
                    !string.Equals(
                        ifCondition.ComparisonValue,
                        elseCondition.ComparisonValue,
                        StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "IF and ELSE must use the same comparison value.");
                }

                if (sourceField.FieldType == FieldType.Number &&
                    !isEmptyOperator &&
                    !decimal.TryParse(
                        ifCondition.ComparisonValue,
                        System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out _))
                {
                    throw new InvalidOperationException(
                        "Number routing requires a valid numeric comparison value.");
                }

                foreach (var rule in new[] { thenRule, elseRule })
                {
                    var actions =
                        await _templateRoutingActionRepository.FindAsync(
                            x => x.TemplateRoutingRuleId == rule.Id,
                            cancellationToken);

                    var actionRoleIds =
                        actions.Select(x => x.RecipientRoleId).ToHashSet();

                    if (actions.Count != roleIds.Count ||
                        !actionRoleIds.SetEquals(roleIds))
                    {
                        throw new InvalidOperationException(
                            $"Routing branch '{rule.Name}' must define one action for every recipient.");
                    }

                    if (actions.All(x =>
                        x.ActionType == TemplateRoutingActionType.SkipRecipient))
                    {
                        throw new InvalidOperationException(
                            $"Routing branch '{rule.Name}' cannot skip every recipient.");
                    }
                }
            }
        }


        public async Task<TemplateDetailsDto> PublishAsync(
    Guid templateId,
    CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var template =
                await _templateRepository.GetByIdForOrganizationAsync(
                    templateId,
                    _currentUser.OrganizationId,
                    cancellationToken);

            if (template is null)
            {
                throw new KeyNotFoundException(
                    "Template was not found.");
            }

            if (template.OwnerUserId != _currentUser.UserId)
            {
                throw new UnauthorizedAccessException(
                    "Only the template owner can publish this template.");
            }

            var latestVersion =
                template.Versions
                    .OrderByDescending(x => x.VersionNumber)
                    .FirstOrDefault();

            if (latestVersion is null)
            {
                throw new InvalidOperationException(
                    "The template does not contain a version.");
            }

            if (latestVersion.IsPublished)
            {
                throw new InvalidOperationException(
                    "Published template versions cannot be edited.");
            }

            var documents =
                await _templateDocumentRepository.FindAsync(
                    x =>
                        x.TemplateVersionId ==
                        latestVersion.Id,
                    cancellationToken);

            if (documents.Count == 0)
            {
                throw new InvalidOperationException(
                    "A template must contain at least one document before publishing.");
            }

            var recipientRoles =
                await _recipientRoleRepository.FindAsync(
                    x =>
                        x.TemplateVersionId ==
                        latestVersion.Id,
                    cancellationToken);

            if (recipientRoles.Count == 0)
            {
                throw new InvalidOperationException(
                    "A template must contain at least one recipient role before publishing.");
            }

            /*
             * Basic routing validation.
             */
            if (recipientRoles.Any(x =>
                    x.RoutingOrder <= 0))
            {
                throw new InvalidOperationException(
                    "All recipient roles must have a valid routing order.");
            }
           

            await ValidateTemplateRoutingForPublishAsync(
    latestVersion.Id,
    cancellationToken);

            await ValidateFieldRoutingBranchesAsync(
                latestVersion.Id,
                cancellationToken);
            latestVersion.IsPublished = true;

            _templateVersionRepository.Update(
                latestVersion);

            template.Status =
                TemplateStatus.Published;

            template.UpdatedAt =
                DateTimeOffset.UtcNow;

            _templateRepository.Update(
                template);

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            return MapTemplateDetails(
                template,
                latestVersion);
        }

        private static TemplateDetailsDto MapTemplateDetails(
    Template template,
    TemplateVersion latestVersion)
        {
            var latestPublishedVersionNumber =
                template.Versions
                    .Where(x => x.IsPublished)
                    .OrderByDescending(
                        x => x.VersionNumber)
                    .Select(
                        x => (int?)x.VersionNumber)
                    .FirstOrDefault();

            /*
             * Current latestVersion dəyişmiş ola bilər,
             * ona görə onun vəziyyətini də nəzərə alırıq.
             */
            if (latestVersion.IsPublished)
            {
                latestPublishedVersionNumber =
                    latestVersion.VersionNumber;
            }

            return new TemplateDetailsDto
            {
                Id = template.Id,

                Name = template.Name,

                Description =
                    template.Description,

                Status =
                    template.Status,

                Category =
                    template.Category,

                OwnerUserId =
                    template.OwnerUserId,

                LatestVersionId =
                    latestVersion.Id,

                LatestVersionNumber =
                    latestVersion.VersionNumber,

                LatestVersionIsPublished =
                    latestVersion.IsPublished,

                HasDraftVersion =
                    !latestVersion.IsPublished,

                LatestPublishedVersionNumber =
                    latestPublishedVersionNumber,

                CreatedAt =
                    template.CreatedAt,

                UpdatedAt =
                    template.UpdatedAt,

               // IsOwner = true
            };
        }

        public async Task<TemplateDetailsDto> CreateNewVersionAsync(
    Guid templateId,
    CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var template =
                await _templateRepository.GetByIdForOrganizationAsync(
                    templateId,
                    _currentUser.OrganizationId,
                    cancellationToken);

            if (template is null)
            {
                throw new KeyNotFoundException(
                    "Template was not found.");
            }

            if (template.OwnerUserId !=
                _currentUser.UserId)
            {
                throw new UnauthorizedAccessException(
                    "Only the template owner can create a new version.");
            }

            /*
             * Eyni anda yalnız bir draft version olsun.
             */
            var existingDraft =
                template.Versions
                    .FirstOrDefault(
                        x => !x.IsPublished);

            if (existingDraft is not null)
            {
                throw new InvalidOperationException(
                    "This template already has a draft version.");
            }

            var sourceVersion =
                template.Versions
                    .Where(x => x.IsPublished)
                    .OrderByDescending(x => x.VersionNumber)
                    .FirstOrDefault();

            if (sourceVersion is null)
            {
                throw new InvalidOperationException(
                    "A new version can only be created from a published template.");
            }

            var newVersion =
                new TemplateVersion
                {
                    Id = Guid.NewGuid(),

                    TemplateId =
                        template.Id,

                    VersionNumber =
                        template.Versions.Max(
                            x => x.VersionNumber) + 1,

                    IsPublished = false,

                    CreatedAt =
                        DateTimeOffset.UtcNow,

                    CreatedByUserId =
                        _currentUser.UserId,

                    ExpirationDays =
    sourceVersion.ExpirationDays,

                    ReminderIntervalDays =
    sourceVersion.ReminderIntervalDays,
                };

            /*
             * OldId → NewId maps.
             */
            var roleIdMap =
                new Dictionary<Guid, Guid>();

            var documentIdMap =
                new Dictionary<Guid, Guid>();

            var fieldIdMap =
                new Dictionary<Guid, Guid>();

            var ruleSetIdMap =
                new Dictionary<Guid, Guid>();

            var ruleIdMap =
                new Dictionary<Guid, Guid>();

            var uploadedPaths =
                new List<string>();

            try
            {
                await _templateVersionRepository.AddAsync(
                    newVersion,
                    cancellationToken);

                /*
                 * 1. Recipient Roles
                 */
                var sourceRoles =
                    await _recipientRoleRepository.FindAsync(
                        x =>
                            x.TemplateVersionId ==
                            sourceVersion.Id,
                        cancellationToken);

                foreach (var sourceRole in sourceRoles)
                {
                    var newRoleId =
                        Guid.NewGuid();

                    roleIdMap[
                        sourceRole.Id] =
                        newRoleId;

                    var newRole =
                        new TemplateRecipientRole
                        {
                            Id =
                                newRoleId,

                            TemplateVersionId =
                                newVersion.Id,

                            Name =
                                sourceRole.Name,

                            RecipientType =
                                sourceRole.RecipientType,

                            RoutingOrder =
                                sourceRole.RoutingOrder,

                            IsRequired =
                                sourceRole.IsRequired,

                            DefaultUserId =
                                sourceRole.DefaultUserId,

                            AllowSenderEditRecipient =
                                sourceRole.AllowSenderEditRecipient,

                            AllowSenderDeleteRecipient =
                                sourceRole.AllowSenderDeleteRecipient,

                            AllowSenderChangeRoutingOrder =
                                sourceRole.AllowSenderChangeRoutingOrder
                        };

                    await _recipientRoleRepository.AddAsync(
                        newRole,
                        cancellationToken);
                }

                /*
                 * 2. Documents
                 *
                 * Published document BlobPath-ını reuse etmirik.
                 */
                var sourceDocuments =
                    await _templateDocumentRepository.FindAsync(
                        x =>
                            x.TemplateVersionId ==
                            sourceVersion.Id,
                        cancellationToken);

                foreach (var sourceDocument in
                         sourceDocuments.OrderBy(x => x.Order))
                {
                    var newDocumentId =
                        Guid.NewGuid();

                    documentIdMap[
                        sourceDocument.Id] =
                        newDocumentId;

                    await using var sourceStream =
                        await _fileStorage.OpenReadAsync(
                            sourceDocument.BlobPath,
                            cancellationToken);

                    var folder =
                        $"templates/{template.Id}/{newVersion.Id}/documents";

                    var newBlobPath =
                        await _fileStorage.UploadAsync(
                            sourceStream,
                            sourceDocument.OriginalFileName,
                            sourceDocument.ContentType,
                            folder,
                            cancellationToken);

                    uploadedPaths.Add(
                        newBlobPath);

                    var newDocument =
                        new TemplateDocument
                        {
                            Id =
                                newDocumentId,

                            TemplateVersionId =
                                newVersion.Id,

                            OriginalFileName =
                                sourceDocument.OriginalFileName,

                            BlobPath =
                                newBlobPath,

                            ContentType =
                                sourceDocument.ContentType,

                            FileSize =
                                sourceDocument.FileSize,

                            FileHash =
                                sourceDocument.FileHash,

                            PageCount =
                                sourceDocument.PageCount,

                            Order =
                                sourceDocument.Order
                        };

                    await _templateDocumentRepository.AddAsync(
                        newDocument,
                        cancellationToken);
                }

                /*
                 * 3. Fields
                 */
                var sourceDocumentIds =
                    sourceDocuments
                        .Select(x => x.Id)
                        .ToHashSet();

                var sourceFields =
                    sourceDocumentIds.Count == 0
                        ? Array.Empty<TemplateField>()
                        : await _templateFieldRepository.FindAsync(
                            x =>
                                sourceDocumentIds.Contains(
                                    x.TemplateDocumentId),
                            cancellationToken);

                foreach (var sourceField in sourceFields)
                {
                    var newFieldId =
                        Guid.NewGuid();

                    fieldIdMap[
                        sourceField.Id] =
                        newFieldId;

                    Guid? newRecipientRoleId =
                        null;

                    if (sourceField.RecipientRoleId.HasValue)
                    {
                        if (!roleIdMap.TryGetValue(
                                sourceField.RecipientRoleId.Value,
                                out
                                var mappedRoleId))
                        {
                            throw new InvalidOperationException(
                                "A template field references an invalid recipient role.");
                        }

                        newRecipientRoleId =
                            mappedRoleId;
                    }

                    if (!documentIdMap.TryGetValue(
                            sourceField.TemplateDocumentId,
                            out
                            var mappedDocumentId))
                    {
                        throw new InvalidOperationException(
                            "A template field references an invalid document.");
                    }

                    var newField =
                        new TemplateField
                        {
                            Id =
                                newFieldId,

                            TemplateDocumentId =
                                mappedDocumentId,

                            RecipientRoleId =
                                newRecipientRoleId,

                            OwnerType =
                                sourceField.OwnerType,

                            FieldType =
                                sourceField.FieldType,

                            PageNumber =
                                sourceField.PageNumber,

                            X =
                                sourceField.X,

                            Y =
                                sourceField.Y,

                            Width =
                                sourceField.Width,

                            Height =
                                sourceField.Height,

                            IsRequired =
                                sourceField.IsRequired,
                            Label = sourceField.Label,
                            OptionJson = sourceField.OptionJson,
                        };

                    await _templateFieldRepository.AddAsync(
                        newField,
                        cancellationToken);
                }

                /*
                 * 4. Routing Rule Sets
                 */
                var sourceRuleSets =
                    await _templateRoutingRuleSetRepository.FindAsync(
                        x =>
                            x.TemplateVersionId ==
                            sourceVersion.Id,
                        cancellationToken);

                foreach (var sourceRuleSet in sourceRuleSets)
                {
                    var newRuleSetId =
                        Guid.NewGuid();

                    ruleSetIdMap[
                        sourceRuleSet.Id] =
                        newRuleSetId;

                    await _templateRoutingRuleSetRepository.AddAsync(
                        new TemplateRoutingRuleSet
                        {
                            Id =
                                newRuleSetId,

                            TemplateVersionId =
                                newVersion.Id,


                            Name = sourceRuleSet.Name.StartsWith(
    "Field routing: ",
    StringComparison.Ordinal)
    ? fieldIdMap.TryGetValue(
        sourceRuleSet.Name["Field routing: ".Length..]
            is var fieldIdText &&
        Guid.TryParse(fieldIdText, out var oldFieldId)
            ? oldFieldId
            : Guid.Empty,
        out var newFieldId)
        ? $"Field routing: {newFieldId}"
        : throw new InvalidOperationException(
            "Routing rule set references an invalid field.")
    : sourceRuleSet.Name,


                            IsActive =
                                sourceRuleSet.IsActive,

                            CreatedAt =
                                DateTimeOffset.UtcNow,

                            UpdatedAt =
                                DateTimeOffset.UtcNow
                        },
                        cancellationToken);
                }

                /*
                 * 5. Routing Rules
                 */
                var sourceRuleSetIds =
                    sourceRuleSets
                        .Select(x => x.Id)
                        .ToHashSet();

                var sourceRules =
                    sourceRuleSetIds.Count == 0
                        ? Array.Empty<TemplateRoutingRule>()
                        : await _templateRoutingRuleRepository.FindAsync(
                            x =>
                                sourceRuleSetIds.Contains(
                                    x.TemplateRoutingRuleSetId),
                            cancellationToken);

                foreach (var sourceRule in sourceRules)
                {
                    if (!ruleSetIdMap.TryGetValue(
                            sourceRule.TemplateRoutingRuleSetId,
                            out
                            var mappedRuleSetId))
                    {
                        throw new InvalidOperationException(
                            "A routing rule references an invalid rule set.");
                    }

                    if (!roleIdMap.TryGetValue(
                            sourceRule.TargetRecipientRoleId,
                            out
                            var mappedTargetRoleId))
                    {
                        throw new InvalidOperationException(
                            "A routing rule references an invalid recipient role.");
                    }

                    var newRuleId =
                        Guid.NewGuid();

                    ruleIdMap[
                        sourceRule.Id] =
                        newRuleId;

                    await _templateRoutingRuleRepository.AddAsync(
                        new TemplateRoutingRule
                        {
                            Id =
                                newRuleId,

                            TemplateRoutingRuleSetId =
                                mappedRuleSetId,

                            TargetRecipientRoleId =
                                mappedTargetRoleId,

                            Name =
                                sourceRule.Name,

                            MatchType =
                                sourceRule.MatchType,

                            Priority =
                                sourceRule.Priority,

                            IsActive =
                                sourceRule.IsActive
                        },
                        cancellationToken);
                }

                /*
                 * 6. Routing Conditions
                 */
                var sourceRuleIds =
                    sourceRules
                        .Select(x => x.Id)
                        .ToHashSet();

                var sourceConditions =
                    sourceRuleIds.Count == 0
                        ? Array.Empty<TemplateRoutingCondition>()
                        : await _templateRoutingConditionRepository.FindAsync(
                            x =>
                                sourceRuleIds.Contains(
                                    x.TemplateRoutingRuleId),
                            cancellationToken);

                foreach (var sourceCondition in
                         sourceConditions)
                {
                    if (!ruleIdMap.TryGetValue(
                            sourceCondition.TemplateRoutingRuleId,
                            out
                            var mappedRuleId))
                    {
                        throw new InvalidOperationException(
                            "A routing condition references an invalid rule.");
                    }

                    if (!fieldIdMap.TryGetValue(
                            sourceCondition.SourceTemplateFieldId,
                            out
                            var mappedFieldId))
                    {
                        throw new InvalidOperationException(
                            "A routing condition references an invalid field.");
                    }

                    await _templateRoutingConditionRepository.AddAsync(
                        new TemplateRoutingCondition
                        {
                            Id =
                                Guid.NewGuid(),

                            TemplateRoutingRuleId =
                                mappedRuleId,

                            SourceTemplateFieldId =
                                mappedFieldId,

                            Operator =
                                sourceCondition.Operator,

                            ComparisonValue =
                                sourceCondition.ComparisonValue
                        },
                        cancellationToken);
                }

                // 7. Routing Actions
                var sourceActions = sourceRuleIds.Count == 0
                    ? Array.Empty<TemplateRoutingAction>()
                    : await _templateRoutingActionRepository.FindAsync(
                        x => sourceRuleIds.Contains(x.TemplateRoutingRuleId),
                        cancellationToken);

                foreach (var sourceAction in sourceActions)
                {
                    if (!ruleIdMap.TryGetValue(
                        sourceAction.TemplateRoutingRuleId,
                        out var mappedRuleId))
                    {
                        throw new InvalidOperationException(
                            "A routing action references an invalid rule.");
                    }

                    if (!roleIdMap.TryGetValue(
                        sourceAction.RecipientRoleId,
                        out var mappedRecipientRoleId))
                    {
                        throw new InvalidOperationException(
                            "A routing action references an invalid recipient role.");
                    }

                    await _templateRoutingActionRepository.AddAsync(
                        new TemplateRoutingAction
                        {
                            Id = Guid.NewGuid(),

                            TemplateRoutingRuleId = mappedRuleId,

                            RecipientRoleId = mappedRecipientRoleId,

                            ActionType = sourceAction.ActionType,

                            Order = sourceAction.Order
                        },
                        cancellationToken);
                }

                template.Status =
                    TemplateStatus.Draft;

                template.UpdatedAt =
                    DateTimeOffset.UtcNow;

                _templateRepository.Update(
                    template);

                await _unitOfWork.SaveChangesAsync(
                    cancellationToken);

                return MapTemplateDetails(
                    template,
                    newVersion);
            }
            catch
            {
                /*
                 * Storage copy uğurlu olub DB save uğursuz olarsa
                 * orphan PDF-ləri saxlamırıq.
                 */
                foreach (var path in uploadedPaths)
                {
                    try
                    {
                        await _fileStorage.DeleteAsync(
                            path,
                            CancellationToken.None);
                    }
                    catch
                    {
                        // Cleanup failure əsas exception-u gizlətməsin.
                    }
                }

                throw;
            }
        }

        public async Task ReorderRecipientRolesAsync(
    Guid templateId,
    ReorderTemplateRecipientRolesRequest request,
    CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var template =
                await _templateRepository.GetByIdForOrganizationAsync(
                    templateId,
                    _currentUser.OrganizationId,
                    cancellationToken);

            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            var latestVersion =
                template.Versions
                    .OrderByDescending(x => x.VersionNumber)
                    .FirstOrDefault();

            if (latestVersion is null)
                throw new InvalidOperationException(
                    "Template version was not found.");

            if (latestVersion.IsPublished)
                throw new InvalidOperationException(
                    "Published template versions cannot be edited.");

            var roles =
                await _recipientRoleRepository.FindAsync(
                    x => x.TemplateVersionId == latestVersion.Id,
                    cancellationToken);

            if (request.RoleIds.Count != roles.Count)
                throw new ArgumentException(
                    "All recipient roles must be supplied.");

            if (request.RoleIds.Distinct().Count()
                != request.RoleIds.Count)
                throw new ArgumentException(
                    "Recipient role ids must be unique.");

            var roleMap =
                roles.ToDictionary(x => x.Id);

            for (var i = 0; i < request.RoleIds.Count; i++)
            {
                var roleId = request.RoleIds[i];

                if (!roleMap.TryGetValue(roleId, out var role))
                    throw new ArgumentException(
                        "Invalid recipient role.");

                role.RoutingOrder = i + 1;

                _recipientRoleRepository.Update(role);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }


        public async Task<IReadOnlyList<TemplateRoutingActionDto>>
            GetRoutingActionsAsync(
                Guid templateId,
                Guid ruleSetId,
                Guid ruleId,
                CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var template =
                await _templateRepository.GetByIdForOrganizationAsync(
                    templateId,
                    _currentUser.OrganizationId,
                    cancellationToken);

            if (template is null)
                throw new KeyNotFoundException("Template was not found.");

            var version = template.Versions
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefault();

            if (version is null)
                throw new KeyNotFoundException("Template version was not found.");

            var ruleSet = await _templateRoutingRuleSetRepository.GetByIdAsync(
                ruleSetId, cancellationToken);

            if (ruleSet is null || ruleSet.TemplateVersionId != version.Id)
                throw new KeyNotFoundException("Routing rule set was not found.");

            var rule = await _templateRoutingRuleRepository.GetByIdAsync(
                ruleId, cancellationToken);

            if (rule is null || rule.TemplateRoutingRuleSetId != ruleSet.Id)
                throw new KeyNotFoundException("Routing rule was not found.");

            var actions = await _templateRoutingActionRepository.FindAsync(
                x => x.TemplateRoutingRuleId == ruleId,
                cancellationToken);

            return actions
                .OrderBy(x => x.Order)
                .Select(x => new TemplateRoutingActionDto
                {
                    Id = x.Id,
                    TemplateRoutingRuleId = x.TemplateRoutingRuleId,
                    RecipientRoleId = x.RecipientRoleId,
                    ActionType = x.ActionType,
                    Order = x.Order
                })
                .ToList();
        }

        public async Task<TemplateRoutingActionDto> AddRoutingActionAsync(
            Guid templateId,
            Guid ruleSetId,
            Guid ruleId,
            TemplateRoutingActionRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            if (!Enum.IsDefined(typeof(TemplateRoutingActionType), request.ActionType))
                throw new ArgumentException("Invalid routing action type.");

            if (request.RecipientRoleId == Guid.Empty)
                throw new ArgumentException("Recipient role is required.");

            if (request.Order <= 0)
                throw new ArgumentException("Action order must be greater than zero.");

            var template =
                await _templateRepository.GetByIdForOrganizationAsync(
                    templateId,
                    _currentUser.OrganizationId,
                    cancellationToken);

            if (template is null)
                throw new KeyNotFoundException("Template was not found.");

            var version = template.Versions
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefault();

            if (version is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            if (template.Status != TemplateStatus.Draft || version.IsPublished)
                throw new InvalidOperationException(
                    "Only a draft template version can be modified.");

            var ruleSet = await _templateRoutingRuleSetRepository.GetByIdAsync(
                ruleSetId, cancellationToken);

            if (ruleSet is null || ruleSet.TemplateVersionId != version.Id)
                throw new KeyNotFoundException("Routing rule set was not found.");

            var rule = await _templateRoutingRuleRepository.GetByIdAsync(
                ruleId, cancellationToken);

            if (rule is null || rule.TemplateRoutingRuleSetId != ruleSet.Id)
                throw new KeyNotFoundException("Routing rule was not found.");

            var role = await _recipientRoleRepository.GetByIdAsync(
                request.RecipientRoleId, cancellationToken);

            if (role is null || role.TemplateVersionId != version.Id)
                throw new ArgumentException(
                    "Recipient role does not belong to this template version.");

            var existingActions =
                await _templateRoutingActionRepository.FindAsync(
                    x => x.TemplateRoutingRuleId == ruleId,
                    cancellationToken);

            if (existingActions.Any(x => x.Order == request.Order))
                throw new InvalidOperationException(
                    "An action already uses this order.");

            if (existingActions.Any(x => x.RecipientRoleId == request.RecipientRoleId))
                throw new InvalidOperationException(
                    "This rule already has an action for the selected recipient.");

            var action = new TemplateRoutingAction
            {
                Id = Guid.NewGuid(),
                TemplateRoutingRuleId = rule.Id,
                RecipientRoleId = role.Id,
                ActionType = request.ActionType,
                Order = request.Order
            };

            await _templateRoutingActionRepository.AddAsync(
                action, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new TemplateRoutingActionDto
            {
                Id = action.Id,
                TemplateRoutingRuleId = action.TemplateRoutingRuleId,
                RecipientRoleId = action.RecipientRoleId,
                ActionType = action.ActionType,
                Order = action.Order
            };
        }

        public async Task<TemplateRoutingActionDto> UpdateRoutingActionAsync(
            Guid templateId,
            Guid ruleSetId,
            Guid ruleId,
            Guid actionId,
            TemplateRoutingActionRequest request,
            CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            if (!Enum.IsDefined(typeof(TemplateRoutingActionType), request.ActionType))
                throw new ArgumentException("Invalid routing action type.");

            if (request.RecipientRoleId == Guid.Empty)
                throw new ArgumentException("Recipient role is required.");

            if (request.Order <= 0)
                throw new ArgumentException("Action order must be greater than zero.");

            var template =
                await _templateRepository.GetByIdForOrganizationAsync(
                    templateId,
                    _currentUser.OrganizationId,
                    cancellationToken);

            if (template is null)
                throw new KeyNotFoundException("Template was not found.");

            var version = template.Versions
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefault();

            if (version is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            if (template.Status != TemplateStatus.Draft || version.IsPublished)
                throw new InvalidOperationException(
                    "Only a draft template version can be modified.");

            var ruleSet = await _templateRoutingRuleSetRepository.GetByIdAsync(
                ruleSetId, cancellationToken);

            if (ruleSet is null || ruleSet.TemplateVersionId != version.Id)
                throw new KeyNotFoundException("Routing rule set was not found.");

            var rule = await _templateRoutingRuleRepository.GetByIdAsync(
                ruleId, cancellationToken);

            if (rule is null || rule.TemplateRoutingRuleSetId != ruleSetId)
                throw new KeyNotFoundException("Routing rule was not found.");

            var action = await _templateRoutingActionRepository.GetByIdAsync(
                actionId, cancellationToken);

            if (action is null || action.TemplateRoutingRuleId != ruleId)
                throw new KeyNotFoundException("Routing action was not found.");

            var recipientRole = await _recipientRoleRepository.GetByIdAsync(
                request.RecipientRoleId, cancellationToken);

            if (recipientRole is null ||
                recipientRole.TemplateVersionId != version.Id)
            {
                throw new ArgumentException(
                    "Recipient role does not belong to this template version.");
            }

            var otherActions = await _templateRoutingActionRepository.FindAsync(
                x => x.TemplateRoutingRuleId == ruleId && x.Id != actionId,
                cancellationToken);

            if (otherActions.Any(x => x.Order == request.Order))
                throw new InvalidOperationException(
                    "Another action already uses this order.");

            if (otherActions.Any(x => x.RecipientRoleId == request.RecipientRoleId))
                throw new InvalidOperationException(
                    "This rule already has an action for the selected recipient.");

            action.RecipientRoleId = request.RecipientRoleId;
            action.ActionType = request.ActionType;
            action.Order = request.Order;

            _templateRoutingActionRepository.Update(action);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new TemplateRoutingActionDto
            {
                Id = action.Id,
                TemplateRoutingRuleId = action.TemplateRoutingRuleId,
                RecipientRoleId = action.RecipientRoleId,
                ActionType = action.ActionType,
                Order = action.Order
            };
        }


        public async Task DeleteRoutingActionAsync(
            Guid templateId,
            Guid ruleSetId,
            Guid ruleId,
            Guid actionId,
            CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            var template =
                await _templateRepository.GetByIdForOrganizationAsync(
                    templateId,
                    _currentUser.OrganizationId,
                    cancellationToken);

            if (template is null)
                throw new KeyNotFoundException("Template was not found.");

            var version = template.Versions
                .OrderByDescending(x => x.VersionNumber)
                .FirstOrDefault();

            if (version is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            if (template.Status != TemplateStatus.Draft || version.IsPublished)
                throw new InvalidOperationException(
                    "Only a draft template version can be modified.");

            var ruleSet = await _templateRoutingRuleSetRepository.GetByIdAsync(
                ruleSetId, cancellationToken);

            if (ruleSet is null || ruleSet.TemplateVersionId != version.Id)
                throw new KeyNotFoundException("Routing rule set was not found.");

            var rule = await _templateRoutingRuleRepository.GetByIdAsync(
                ruleId, cancellationToken);

            if (rule is null || rule.TemplateRoutingRuleSetId != ruleSetId)
                throw new KeyNotFoundException("Routing rule was not found.");

            var action = await _templateRoutingActionRepository.GetByIdAsync(
                actionId, cancellationToken);

            if (action is null || action.TemplateRoutingRuleId != ruleId)
                throw new KeyNotFoundException("Routing action was not found.");

            _templateRoutingActionRepository.Delete(action);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }


    }
}
