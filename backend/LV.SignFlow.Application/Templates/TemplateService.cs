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
using System.Text;
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
        private readonly IRepository<User> _userRepository;

        public TemplateService(ITemplateRepository templateRepository, ICurrentUser curentUser, IUnitOfWork unitOfWork, IFileStorage fileStorage, IRepository<TemplateRecipientRole> recipientRoleRepository, IRepository<TemplateDocument> templateDocumentRepository, IRepository<User> userRepository, IRepository<TemplateField> templateFieldRepository, IRepository<TemplateRoutingRuleSet> templateRoutingRuleSetRepository = null, IRepository<TemplateRoutingRule> templateRoutingRuleRepository = null, IRepository<TemplateRoutingCondition> templateRoutingConditionRepository = null)
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
                IsDeleted = false,
                CreatedAt = now,
                UpdatedAt = now
            };
            var version = new TemplateVersion
            {
                Id = Guid.NewGuid(),
                CreatedByUserId = _currentUser.UserId,
                TemplateId = template.Id,
                Template = template,
                CreatedAt = now,
                IsPublished = false,
                VersionNumber = 1
            };

            template.Versions.Add(version);
            await _templateRepository.AddAsync(template, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return template.Id;
        }

        public async Task<IReadOnlyList<TemplateListItemDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            var templates = await _templateRepository.GetAllForOrganizationAsync(_currentUser.OrganizationId, cancellationToken);

            return templates.Select(x => new TemplateListItemDto
            {
                Id = x.Id,
                Name = x.Name,
                Description = x.Description,
                Status = x.Status,
                LatestVersionNumber = x.Versions.Count == 0 ? 0 : x.Versions.Max(x => x.VersionNumber),
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
            }).ToList();
        }

        public async Task<TemplateDetailsDto?> GetByIdAsync(Guid Id, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            var template = await _templateRepository.GetByIdForOrganizationAsync(Id, _currentUser.OrganizationId, cancellationToken);
            if (template is null)
            {
                return null;
            }

            return new TemplateDetailsDto
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                OwnerUserId = template.OwnerUserId,
                LatestVersionNumber = template.Versions.Count == 0 ? 0 :
                template.Versions.Max(x => x.VersionNumber),

            };
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

            if (existingRoles.Any(
                x => x.RoutingOrder == request.RoutingOrder))
            {
                throw new InvalidOperationException(
                    "Another recipient already uses this routing order.");
            }

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
                    IsRequired = x.IsRequired
                })
                .ToList();
        }

        public async Task<TemplateFieldDto> AddFieldsAsync(Guid templateId,TemplateFieldRequest templateFieldRequest, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();

            if (templateFieldRequest.TemplateDocumentId == Guid.Empty) throw new ArgumentException("Template document is requied");
            if (templateFieldRequest.RecipientRoleId == Guid.Empty) throw new ArgumentException("Recipient role is requied");
            if (templateFieldRequest.PageNumber <=0) throw new ArgumentException("Page number must be greater than zero.");
             
            ValidateNormalizedCoordinates(templateFieldRequest);
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

            var recipientRole = await _recipientRoleRepository.GetByIdAsync(templateFieldRequest.RecipientRoleId, cancellationToken);
            if (recipientRole is null || recipientRole.TemplateVersionId != latestVersion.Id)
            {
                throw new ArgumentException(
          "The selected recipient role does not belong to this template version.");
            }

            if (document.PageCount.HasValue &&
                templateFieldRequest.PageNumber > document.PageCount.Value)
            {
                throw new ArgumentException(
                    "The selected page does not exist in the document.");
            }

            var field = new TemplateField
            {
                Id = Guid.NewGuid(),

                TemplateDocumentId =
                    document.Id,

                RecipientRoleId =
                    recipientRole.Id,

                FieldType =
                    templateFieldRequest.FieldType,

                PageNumber =
                    templateFieldRequest.PageNumber,

                X = templateFieldRequest.X,

                Y = templateFieldRequest.Y,

                Width = templateFieldRequest.Width,

                Height = templateFieldRequest.Height,

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
                Id = field.Id,
                TemplateDocumentId =
                    field.TemplateDocumentId,
                RecipientRoleId =
                    field.RecipientRoleId,
                FieldType =
                    field.FieldType,
                PageNumber =
                    field.PageNumber,
                X = field.X,
                Y = field.Y,
                Width = field.Width,
                Height = field.Height,
                IsRequired =
                    field.IsRequired
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

            if (request.RecipientRoleId == Guid.Empty)
                throw new ArgumentException(
                    "Recipient role is required.");

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


            var lastVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (lastVersion is null) throw new InvalidOperationException("The template does not contain a version.");

            var field = await _templateFieldRepository.GetByIdAsync(templateId, cancellationToken);
            if (field is null) throw new KeyNotFoundException("Template field was not found.");

            var currentDocument = await _templateDocumentRepository.GetByIdAsync(field.TemplateDocumentId, cancellationToken);
            if (currentDocument is null || currentDocument.TemplateVersionId!=lastVersion.Id) throw new KeyNotFoundException("Template document was not found");

            var targetDocument = await _templateDocumentRepository.GetByIdAsync(field.TemplateDocumentId, cancellationToken);
            if(targetDocument is null || targetDocument.TemplateVersionId != lastVersion.Id)
            {
                throw new ArgumentException(
        "The selected document does not belong to this template version.");
            }
            var recipientRole =
        await _recipientRoleRepository.GetByIdAsync(
            request.RecipientRoleId,
            cancellationToken);

            if (recipientRole is null ||
                recipientRole.TemplateVersionId != lastVersion.Id)
            {
                throw new ArgumentException(
                    "The selected recipient role does not belong to this template version.");
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
                FieldType =
                    field.FieldType,
                PageNumber =
                    field.PageNumber,
                X = field.X,
                Y = field.Y,
                Width = field.Width,
                Height = field.Height,
                IsRequired =
                    field.IsRequired
            };
        }

        public async Task DeleteTemplateFieldAsync(Guid templateId, Guid fieldId, CancellationToken cancellationToken)
        {
            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template is null) throw new KeyNotFoundException("Template was not found.");


            var lastVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (lastVersion is null) throw new InvalidOperationException("The template does not contain a version.");

            var field = await _templateFieldRepository.GetByIdAsync(templateId, cancellationToken);
            if (field is null) throw new KeyNotFoundException("Template field was not found.");

            var currentDocument = await _templateDocumentRepository.GetByIdAsync(field.TemplateDocumentId, cancellationToken);
            if (currentDocument is null || currentDocument.TemplateVersionId != lastVersion.Id) throw new KeyNotFoundException("Template document was not found");

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

            if (existingRoles.Any(x => x.RoutingOrder == request.RoutingOrder))
            {
                throw new InvalidOperationException(
                    "Another recipient already uses this routing order.");
            }

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


            var lastVersion = template.Versions.OrderByDescending(x => x.VersionNumber).FirstOrDefault();
            if (lastVersion is null) throw new InvalidOperationException("The template does not contain a version.");
            var recipientRole = await _recipientRoleRepository.GetByIdAsync(roleId, cancellationToken);

            if (recipientRole is null ||
        recipientRole.TemplateVersionId != lastVersion.Id)
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

            var lastVersion=template.Versions.OrderDescending().FirstOrDefault();
            if (lastVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var document = await _templateDocumentRepository.FindAsync(x => x.TemplateVersionId == lastVersion.Id);
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

            var lastVersion = template.Versions.OrderDescending().FirstOrDefault();
            if (lastVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var document = await _templateDocumentRepository.GetByIdAsync(documentId,cancellationToken);
            if (document is null ||
      document.TemplateVersionId != lastVersion.Id)
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

            var lastVersion = template.Versions.OrderDescending().FirstOrDefault();
            if (lastVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var document = await _templateDocumentRepository.GetByIdAsync(documentId, cancellationToken);
            if (document is null ||
      document.TemplateVersionId != lastVersion.Id)
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

            var lastVersion = template.Versions.OrderDescending().FirstOrDefault();
            if (lastVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");
            var route=await _templateRoutingRuleSetRepository.FindAsync(x=>x.TemplateVersionId==lastVersion.TemplateId, cancellationToken);
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

            var lastVersion = template.Versions.OrderDescending().FirstOrDefault();
            if (lastVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var existingRuleSets =
                await _templateRoutingRuleSetRepository.FindAsync(
                    x => x.TemplateVersionId == lastVersion.Id,
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

                TemplateVersionId = lastVersion.Id,

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

            var lastVersion = template.Versions.OrderDescending().FirstOrDefault();
            if (lastVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var ruleSet = await _templateRoutingRuleSetRepository.GetByIdAsync(ruleSetId,cancellationToken);

            if (ruleSet is null || ruleSet.TemplateVersionId != lastVersion.Id)  throw new KeyNotFoundException( "Routing rule set was not found.");
           

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

            var lastVersion = template.Versions.OrderDescending().FirstOrDefault();
            if (lastVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var ruleSet =
                await _templateRoutingRuleSetRepository.GetByIdAsync(
                    ruleSetId,
                    cancellationToken);

            if (ruleSet is null ||
                ruleSet.TemplateVersionId != lastVersion.Id)
            {
                throw new KeyNotFoundException(
                    "Routing rule set was not found.");
            }

            var targetRecipientRole =
                await _recipientRoleRepository.GetByIdAsync(
                    request.TargetRecipientRoleId,
                    cancellationToken);

            if (targetRecipientRole is null ||
                targetRecipientRole.TemplateVersionId != lastVersion.Id)
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

        public async Task<IReadOnlyList<TemplateRoutingConditionDto>> GetRoutingConditionsAsync(Guid templateId, Guid ruleId, Guid ruleSetId, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template is null)
                throw new KeyNotFoundException(
                    "Template was not found.");

            var lastVersion = template.Versions.OrderDescending().FirstOrDefault();
            if (lastVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");
            var ruleSet = await _templateRoutingRuleSetRepository.GetByIdAsync( ruleSetId,cancellationToken);

            if (ruleSet is null || ruleSet.TemplateVersionId != lastVersion.Id)
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

        public async Task<TemplateRoutingConditionDto> AddRoutingConditionsAsync(Guid templateId, Guid ruleId, Guid ruleSetId, TemplateRoutingConditionRequest request, CancellationToken cancellationToken = default)
        {
            EnsureAuthenticated();
            var template = await _templateRepository.GetByIdForOrganizationAsync(templateId, _currentUser.OrganizationId, cancellationToken);
            if (template is null) throw new KeyNotFoundException( "Template was not found.");

            var lastVersion = template.Versions.OrderDescending().FirstOrDefault();
            if (lastVersion is null) throw new InvalidOperationException( "The template does not contain a version.");

            var ruleSet = await _templateRoutingRuleSetRepository.GetByIdAsync(ruleSetId, cancellationToken);
            if (ruleSet is null || ruleSet.TemplateVersionId != lastVersion.Id) throw new KeyNotFoundException("Routing rule set was not found.");
           

            var rule = await _templateRoutingRuleRepository.GetByIdAsync(ruleId, cancellationToken);
            if (rule is null || rule.TemplateRoutingRuleSetId != ruleSet.Id)  throw new KeyNotFoundException("Routing rule was not found.");
        
            var sourceField=await _templateFieldRepository.GetByIdAsync(request.SourceTemplateFieldId, cancellationToken);
            if (sourceField is null)  throw new ArgumentException("The selected source field is invalid.");

            var sourceDocument=await _templateDocumentRepository.GetByIdAsync(sourceField.TemplateDocumentId, cancellationToken);
            if (sourceDocument is null || sourceDocument.TemplateVersionId != lastVersion.Id) throw new ArgumentException("The selected source field does not belong to this template version.");
      

            var condition = new TemplateRoutingCondition
            {
                Id = new Guid(),
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

            template.UpdatedAt = DateTime.UtcNow;
            _templateRepository.Update(template);
            await _unitOfWork.SaveChangesAsync(cancellationToken);


            var latestVersion = template.Versions.OrderDescending().FirstOrDefault();
            if (latestVersion is null) throw new InvalidOperationException("The template does not contain a version.");
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
                UpdatedAt = template.UpdatedAt
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

            var latestVersion = template.Versions.OrderDescending().FirstOrDefault();
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

            var rules =
                await _templateRoutingRuleRepository.FindAsync(
                    x => x.TemplateRoutingRuleSetId == ruleSetId,
                    cancellationToken);

            if (rules.Count > 0)
            {
                throw new InvalidOperationException(
                    "This routing rule set cannot be deleted because it contains routing rules.");
            }

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

            var lastVersion = template.Versions.OrderDescending().FirstOrDefault();
            if (lastVersion is null)
                throw new InvalidOperationException(
                    "The template does not contain a version.");

            var ruleSet = await _templateRoutingRuleSetRepository.GetByIdAsync(ruleSetId, cancellationToken);

            if (ruleSet is null || ruleSet.TemplateVersionId != lastVersion.Id) throw new KeyNotFoundException("Routing rule set was not found.");


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
                recipientRole.TemplateVersionId != lastVersion.Id)
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

        public async Task<TemplateRoutingConditionDto> UpdateRoutingConditionsAsync(Guid templateId, Guid ruleId, Guid ruleSetId, Guid conditionId, TemplateRoutingConditionRequest request, CancellationToken cancellationToken = default)
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

        public async Task DeleteRoutingConditionsAsync(Guid templateId, Guid ruleId, Guid ruleSetId, Guid conditionId, CancellationToken cancellationToken = default)
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

        public Task<IReadOnlyList<TemplateListItemDto>> GetSharedWithMeAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<TemplateListItemDto>> GetMyTemplatesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
