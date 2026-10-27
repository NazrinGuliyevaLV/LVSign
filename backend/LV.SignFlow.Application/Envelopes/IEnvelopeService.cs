using LV.SignFlow.Application.Envelopes.Dtos;
using LV.SignFlow.Application.Envelopes.Requests;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes
{
    public interface IEnvelopeService
    {
        //envelope
        public Task<Guid> CreateEnvelopeAsync(EnvelopeRequest request,CancellationToken cancellationToken = default);
        public Task<IReadOnlyList<EnvelopeListItemDto>> GetAllAsync(CancellationToken cancellationToken=default);
        public Task<EnvelopeDetailsDto> GetByIdAsync(Guid envelopeId,CancellationToken cancellationToken = default);
        public Task DeleteEnvelopeAsync(Guid envelopeId,CancellationToken cancellationToken = default) ;
        public Task<EnvelopeDetailsDto> UpdateEnvelopeAsync(Guid envelopeId,EnvelopeRequest request, CancellationToken cancellationToken = default);

        //document
        public Task<EnvelopeDocumentDto> UploadDocumentAsync(Guid envelopeId, UploadEnvelopeDocumentRequest request, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<EnvelopeDocumentDto>> GetDocumentsAsync(Guid envelopeId,CancellationToken cancellationToken = default);
        Task<EnvelopeDocumentContentResultDto> GetDocumentContentAsync(Guid envelopeId,Guid documentId,CancellationToken cancellationToken = default);
        Task DeleteDocumentAsync(Guid envelopeId,Guid documentId, CancellationToken cancellationToken = default);
        //recipients
        public Task<EnvelopeRecipientDto> AddEnvelopeRecipientAsync(Guid envelopeId,EnvelopeRecipientRequest request, CancellationToken cancellationToken = default);
        public Task<IReadOnlyList<EnvelopeRecipientDto>> GetAllEnvelopeRecipientsAsync(Guid envelopeId, CancellationToken cancellationToken = default);
        public Task DeleteEnvelopeRecipientAsync(Guid envelopeId,Guid recipientId, CancellationToken cancellationToken = default);
        public Task<EnvelopeRecipientDto> UpdateEnvelopeRecipientAsync(Guid envelopeId,Guid recipientId,EnvelopeRecipientRequest request, CancellationToken cancellationToken = default);

        //fields
        public Task<EnvelopeFieldDto> AddEnvelopeFieldAsync(Guid envelopeId, EnvelopeFieldRequest request, CancellationToken cancellationToken = default);
        public Task<IReadOnlyList<EnvelopeFieldDto>> GetAllEnvelopeFieldsAsync(Guid envelopeId, CancellationToken cancellationToken = default);
        public Task DeleteEnvelopeFieldAsync(Guid envelopeId, Guid fieldId, CancellationToken cancellationToken = default);
        public Task<EnvelopeFieldDto> UpdateEnvelopeFieldAsync(Guid envelopeId, Guid fieldId, EnvelopeFieldRequest request, CancellationToken cancellationToken = default);

        //Snapshot
        public Task<Guid> CreateFromTemplateAsync(Guid templateId,EnvelopeFromTemplateRequest request,CancellationToken cancellationToken = default);

        public Task SendAsync(Guid envelopeId,CancellationToken cancellationToken = default);
        public Task FinishRecipientAsync(Guid envelopeId,Guid recipientId,FinishRecipientRequest request,CancellationToken cancellationToken = default);
        public Task DeclineRecipientAsync(Guid envelopeId,Guid recipientId,VoidAndDeclineEnvelopeRequest request,CancellationToken cancellationToken = default);
        public Task VoidAsync(Guid envelopeId, VoidAndDeclineEnvelopeRequest request, CancellationToken cancellationToken = default);


        public Task UpdateEnvelopeDraftAsync(Guid envelopeId, EnvelopeDraftRequest req,CancellationToken cancellationToken = default);
        public Task ReOrderDocumentAsync(Guid envelopeId,ReorderEnvelopeDocumentsRequest req,CancellationToken cancellationToken = default);

        //routing rule
        public Task<EnvelopeRoutingRuleDto> CreateRoutingRuleAsync(Guid envelopeId,EnvelopeRoutingRuleRequest request,CancellationToken cancellationToken = default);
        public Task<IReadOnlyList<EnvelopeRoutingRuleDto>> GetRoutingRulesAsync(Guid envelopeId, CancellationToken cancellationToken = default);

        public Task DeleteRoutingRuleAsync(Guid envelopeId,Guid ruleId,CancellationToken cancellationToken = default);
        public Task<EnvelopeRoutingRuleDto> UpdateRoutingRuleAsync(Guid envelopeId,Guid ruleId,EnvelopeRoutingRuleRequest request,CancellationToken cancellationToken = default);
    }
}
