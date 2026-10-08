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

        Task<IReadOnlyList<TemplateRoutingConditionDto>> GetRoutingConditionsAsync(Guid templateId, Guid ruleId, Guid ruleSetId, CancellationToken cancellationToken = default);
        Task<TemplateRoutingConditionDto> AddRoutingConditionsAsync(Guid templateId, Guid ruleId, Guid ruleSetId, TemplateRoutingConditionRequest request, CancellationToken cancellationToken = default);
        Task<TemplateRoutingConditionDto> UpdateRoutingConditionsAsync(Guid templateId, Guid ruleId, Guid ruleSetId, Guid conditionId, TemplateRoutingConditionRequest request, CancellationToken cancellationToken = default);
        Task DeleteRoutingConditionsAsync(Guid templateId, Guid ruleId, Guid ruleSetId, Guid conditionId,  CancellationToken cancellationToken = default);



    }
}
