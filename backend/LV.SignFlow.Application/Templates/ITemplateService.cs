using LV.SignFlow.Application.Templates.Dtos;
using LV.SignFlow.Application.Templates.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates
{
    public interface ITemplateService
    {
        Task<IReadOnlyList<TemplateListItemDto>> GetAllAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TemplateListItemDto>> GetMyTemplatesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TemplateListItemDto>> GetSharedWithMeAsync(CancellationToken cancellationToken = default);
        Task<TemplateDetailsDto?> GetByIdAsync(Guid Id,CancellationToken cancellationToken = default);
        Task<TemplateDetailsDto?> UpdateTemplate(Guid Id,TemplateRequest request,CancellationToken cancellationToken = default);
        Task DeleteTemplate(Guid Id,CancellationToken cancellationToken = default);
        Task<Guid> CreateAsync(TemplateRequest request,CancellationToken cancellationToken = default);
        //template fiel;d
        Task<IReadOnlyList<TemplateFieldDto>> GetFieldsAsync(Guid templateId,CancellationToken cancellationToken = default);
        Task<TemplateFieldDto> AddFieldsAsync(Guid templateId,TemplateFieldRequest templateFieldRequest,CancellationToken cancellationToken = default);
        Task<TemplateFieldDto> UpdateTemplateFieldAsync(Guid templateId,Guid fieldId, TemplateFieldRequest request, CancellationToken cancellationToken);
        Task DeleteTemplateFieldAsync(Guid templateId, Guid fieldId, CancellationToken cancellationToken);
        //recipient role
        Task<IReadOnlyList<TemplateRecipientRoleDto>> GetRecipientRoleAsync(Guid templateId, CancellationToken cancellationToken = default);
        Task<TemplateRecipientRoleDto> AddRecipientRoleAsync(Guid templateId, TemplateRecipientRoleRequest request, CancellationToken cancellationToken = default);
        Task<TemplateRecipientRoleDto> UpdateTemplateRecipientRoledAsync(Guid templateId, Guid fieldId, TemplateRecipientRoleRequest request, CancellationToken cancellationToken);
        Task DeleteTemplateRecipientRoleAsync(Guid templateId, Guid fieldId, CancellationToken cancellationToken);

        //document
        Task<TemplateDocumentDto> UploadDocumentAsync(Guid templateId, UploadTemplateDocumentRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<TemplateDocumentDto>> GetDocumentAsync(Guid templateId,CancellationToken cancellationToken);
        Task<TemplateDocumentContentDto> GetDocumentContentAsync(Guid templateId,Guid documentId,CancellationToken cancellationToken);
        Task DeleteDocumentAsync(Guid templateId,Guid documentId,CancellationToken cancellationToken);

        //routing role
        Task<IReadOnlyList<TemplateRoutingRuleSetDto>> GetRoutingRuleSetAsync(Guid templateId, CancellationToken cancellationToken = default);
        Task<TemplateRoutingRuleSetDto> AddRoutingRuleSetAsync(Guid templateId,TemplateRoutingRuleSetRequest request, CancellationToken cancellationToken = default);
        Task<TemplateRoutingRuleSetDto> UpdateRoutingRuleSetAsync(Guid templateId,Guid ruleSetId, TemplateRoutingRuleSetRequest request, CancellationToken cancellationToken = default);
        Task DeleteRoutingRuleSetAsync(Guid templateId,Guid ruleSetId, CancellationToken cancellationToken = default);


        Task<IReadOnlyList<TemplateRoutingRuleDto>> GetRoutingRuleAsync(Guid templateId, Guid ruleSetId, CancellationToken cancellationToken = default);
        Task<TemplateRoutingRuleDto> AddRoutingRuleAsync(Guid templateId, Guid ruleSetId, TemplateRoutingRuleRequest request, CancellationToken cancellationToken = default);
        Task<TemplateRoutingRuleDto> UpdateRoutingRuleAsync(Guid templateId, Guid ruleSetId, Guid ruleId, TemplateRoutingRuleRequest request, CancellationToken cancellationToken = default);
        Task DeleteRoutingRuleAsync(Guid templateId, Guid ruleSetId, Guid ruleId, CancellationToken cancellationToken = default);

        //condition

        Task<IReadOnlyList<TemplateRoutingConditionDto>> GetRoutingConditionsAsync(Guid templateId, Guid ruleSetId, Guid ruleId, CancellationToken cancellationToken = default);
        Task<TemplateRoutingConditionDto> AddRoutingConditionsAsync(Guid templateId, Guid ruleSetId, Guid ruleId, TemplateRoutingConditionRequest request, CancellationToken cancellationToken = default);
        Task<TemplateRoutingConditionDto> UpdateRoutingConditionsAsync(Guid templateId, Guid ruleSetId, Guid ruleId, Guid conditionId, TemplateRoutingConditionRequest request, CancellationToken cancellationToken = default);
        Task DeleteRoutingConditionsAsync(Guid templateId, Guid ruleSetId, Guid ruleId, Guid conditionId,  CancellationToken cancellationToken = default);

        //share template
        Task<IReadOnlyList<TemplateShareDto>> GetSharesAsync(Guid templateId,CancellationToken cancellationToken = default);
        Task<TemplateShareDto> AddShareAsync(Guid templateId,TemplateShareRequest request,CancellationToken cancellationToken = default);
        Task<TemplateShareDto> UpdateShareAsync(Guid templateId,Guid shareId,TemplateShareRequest request,CancellationToken cancellationToken = default);
        Task DeleteShareAsync(Guid templateId,Guid shareId,CancellationToken cancellationToken = default);


        Task<TemplateDetailsDto> PublishAsync(Guid templateId, CancellationToken cancellationToken = default);
        Task<TemplateDetailsDto> CreateNewVersionAsync(Guid templateId, CancellationToken cancellationToken = default);
        Task ReorderRecipientRolesAsync(
    Guid templateId,
    ReorderTemplateRecipientRolesRequest request,
    CancellationToken cancellationToken = default);

        // Conditional routing actions

        Task<IReadOnlyList<TemplateRoutingActionDto>> GetRoutingActionsAsync(
            Guid templateId,
            Guid ruleSetId,
            Guid ruleId,
            CancellationToken cancellationToken = default);

        Task<TemplateRoutingActionDto> AddRoutingActionAsync(
            Guid templateId,
            Guid ruleSetId,
            Guid ruleId,
            TemplateRoutingActionRequest request,
            CancellationToken cancellationToken = default);

        Task<TemplateRoutingActionDto> UpdateRoutingActionAsync(
    Guid templateId,
    Guid ruleSetId,
    Guid ruleId,
    Guid actionId,
    TemplateRoutingActionRequest request,
    CancellationToken cancellationToken = default);

        Task DeleteRoutingActionAsync(
            Guid templateId,
            Guid ruleSetId,
            Guid ruleId,
            Guid actionId,
            CancellationToken cancellationToken = default);

    }
}
