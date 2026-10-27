using LV.SignFlow.Application.Envelopes;
using LV.SignFlow.Application.Envelopes.Dtos;
using LV.SignFlow.Application.Envelopes.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LV.SignFlow.Api.Controllers
{
    [Route("api/envelopes")]
    [ApiController]
    public class EnvelopesController : ControllerBase
    {
        private readonly IEnvelopeService _envelopeService;
        public EnvelopesController(IEnvelopeService envelopeService)
        {
            _envelopeService = envelopeService;
        }
        //envelope endpoints
        [HttpPost]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<Guid>> CreateEnvelope(EnvelopeRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var envelopeId = await _envelopeService.CreateEnvelopeAsync(request, cancellationToken);
                return StatusCode(StatusCodes.Status201Created, envelopeId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpGet]
        [ProducesResponseType(typeof(EnvelopeListItemDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<IReadOnlyList<EnvelopeListItemDto>>> GetAllEnvelopes(CancellationToken cancellationToken)
        {
            try
            {
                var envelopes = await _envelopeService.GetAllAsync(cancellationToken);
                return StatusCode(StatusCodes.Status201Created, envelopes);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }

        }
        [HttpGet("{envelopeId:guid}")]
        [ProducesResponseType(typeof(EnvelopeDetailsDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<EnvelopeDetailsDto>> GetById(Guid envelopeId, CancellationToken cancellationToken)
        {
            try
            {
                var envelope = await _envelopeService.GetByIdAsync(envelopeId, cancellationToken);
                if (envelope == null) return NotFound();
                return StatusCode(StatusCodes.Status201Created, envelope);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpDelete("{envelopeId:guid}")]
        [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteEnvelope(Guid envelopeId, CancellationToken cancellationToken)
        {
            try
            {
                await _envelopeService.DeleteEnvelopeAsync(envelopeId, cancellationToken);
                return StatusCode(StatusCodes.Status200OK, envelopeId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{envelopeId:guid}")]
        [ProducesResponseType(typeof(EnvelopeDetailsDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<EnvelopeDetailsDto>> UpdateEnvelope(Guid envelopeId, EnvelopeRequest request, CancellationToken cancellationToken =
      default)
        {
            try
            {
                await _envelopeService.UpdateEnvelopeAsync(envelopeId, request, cancellationToken);
                return StatusCode(StatusCodes.Status200OK, envelopeId);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //document

        [HttpPost("{envelopeId:guid}/documents")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(EnvelopeDocumentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<EnvelopeDocumentDto>> UploadDocument(Guid envelopeId, IFormFile file, CancellationToken cancellationToken)
        {
            if (file.Length == 0) return BadRequest("The uploaded file is empty.");

            await using
            var stream = file.OpenReadStream();

            var request = new UploadEnvelopeDocumentRequest
            {
                Stream = stream,
                FileName = file.FileName,
                ContentType = file.ContentType,
                FileSize = file.Length
            };

            try
            {
                var document = await _envelopeService.UploadDocumentAsync(
                envelopeId, request, cancellationToken);

                return StatusCode(StatusCodes.Status201Created, document);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpGet("{envelopeId:guid}/documents")]
        [ProducesResponseType(
        typeof(IReadOnlyList<EnvelopeDocumentDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(
        StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<EnvelopeDocumentDto>>> GetDocuments(
        Guid envelopeId, CancellationToken cancellationToken)
        {
            try
            {
                var documents = await _envelopeService.GetDocumentsAsync(
                envelopeId, cancellationToken);

                return Ok(documents);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpGet("{envelopeId:guid}/documents/{documentId:guid}/content")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDocumentContent(Guid envelopeId, Guid documentId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _envelopeService.GetDocumentContentAsync(envelopeId, documentId, cancellationToken);

                return File(
                result.Stream, result.ContentType, result.FileName, enableRangeProcessing: true);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpDelete("{envelopeId:guid}/documents/{documentId:guid}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteDocument(Guid envelopeId, Guid documentId, CancellationToken cancellationToken)
        {
            try
            {
                await _envelopeService.DeleteDocumentAsync(envelopeId, documentId, cancellationToken);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("{envelopeId:guid}/recipients")]
        [ProducesResponseType(typeof(EnvelopeRecipientDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<EnvelopeRecipientDto>> AddRecipient(
        Guid envelopeId, EnvelopeRecipientRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var recipient = await _envelopeService.AddEnvelopeRecipientAsync(
                envelopeId, request, cancellationToken);

                return StatusCode(
                StatusCodes.Status201Created, recipient);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpGet("{envelopeId:guid}/recipients")]
        public async Task<ActionResult<IReadOnlyList<EnvelopeRecipientDto>>> GetRecipients(
        Guid envelopeId, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(
                await _envelopeService.GetAllEnvelopeRecipientsAsync(
                envelopeId, cancellationToken));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPut("{envelopeId:guid}/recipients/{recipientId:guid}")]
        public async Task<ActionResult<EnvelopeRecipientDto>> UpdateRecipient(
        Guid envelopeId, Guid recipientId, EnvelopeRecipientRequest request, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(
                await _envelopeService.UpdateEnvelopeRecipientAsync(
                envelopeId, recipientId, request, cancellationToken));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpDelete("{envelopeId:guid}/recipients/{recipientId:guid}")]
        public async Task<IActionResult> DeleteRecipient(
        Guid envelopeId, Guid recipientId, CancellationToken cancellationToken)
        {
            try
            {
                await _envelopeService.DeleteEnvelopeRecipientAsync(
                envelopeId, recipientId, cancellationToken);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpGet("{envelopeId:guid}/fields")]
        public async Task<ActionResult<IReadOnlyList<EnvelopeFieldDto>>> GetFields(
        Guid envelopeId, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(
                await _envelopeService.GetAllEnvelopeFieldsAsync(
                envelopeId, cancellationToken));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpPost("{envelopeId:guid}/fields")]
        public async Task<ActionResult<EnvelopeFieldDto>> AddField(
        Guid envelopeId, EnvelopeFieldRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var field = await _envelopeService.AddEnvelopeFieldAsync(
                envelopeId, request, cancellationToken);

                return StatusCode(
                StatusCodes.Status201Created, field);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPut("{envelopeId:guid}/fields/{fieldId:guid}")]
        public async Task<ActionResult<EnvelopeFieldDto>> UpdateField(
        Guid envelopeId, Guid fieldId, EnvelopeFieldRequest request, CancellationToken cancellationToken)
        {
            try
            {
                return Ok(
                await _envelopeService.UpdateEnvelopeFieldAsync(
                envelopeId, fieldId, request, cancellationToken));
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpDelete("{envelopeId:guid}/fields/{fieldId:guid}")]
        public async Task<IActionResult> DeleteField(
        Guid envelopeId, Guid fieldId, CancellationToken cancellationToken)
        {
            try
            {
                await _envelopeService.DeleteEnvelopeFieldAsync(
                envelopeId, fieldId, cancellationToken);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("from-template/{templateId:guid}")]
        [ProducesResponseType(
        typeof(Guid), StatusCodes.Status201Created)]
        [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
        StatusCodes.Status404NotFound)]
        [ProducesResponseType(
        StatusCodes.Status409Conflict)]
        public async Task<ActionResult<Guid>> CreateFromTemplate(
        Guid templateId, EnvelopeFromTemplateRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var envelopeId = await _envelopeService.CreateFromTemplateAsync(
                templateId, request, cancellationToken);

                return StatusCode(
                StatusCodes.Status201Created, envelopeId);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("{envelopeId:guid}/send")]
        [ProducesResponseType(
        StatusCodes.Status204NoContent)]
        [ProducesResponseType(
        StatusCodes.Status404NotFound)]
        [ProducesResponseType(
        StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Send(
        Guid envelopeId, CancellationToken cancellationToken)
        {
            try
            {
                await _envelopeService.SendAsync(
                envelopeId, cancellationToken);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("{envelopeId:guid}/recipients/{recipientId:guid}/finish")]
        [ProducesResponseType(
        StatusCodes.Status204NoContent)]
        [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
        StatusCodes.Status404NotFound)]
        [ProducesResponseType(
        StatusCodes.Status409Conflict)]
        public async Task<IActionResult> FinishRecipient(
        Guid envelopeId, Guid recipientId, FinishRecipientRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await _envelopeService.FinishRecipientAsync(
                envelopeId, recipientId, request, cancellationToken);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("{envelopeId:guid}/recipients/{recipientId:guid}/decline")]
        [ProducesResponseType(
        StatusCodes.Status204NoContent)]
        [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
        StatusCodes.Status404NotFound)]
        [ProducesResponseType(
        StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeclineRecipient(
        Guid envelopeId, Guid recipientId, VoidAndDeclineEnvelopeRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await _envelopeService.DeclineRecipientAsync(
                envelopeId, recipientId, request, cancellationToken);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("{envelopeId:guid}/void")]
        [ProducesResponseType(
        StatusCodes.Status204NoContent)]
        [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
        StatusCodes.Status404NotFound)]
        [ProducesResponseType(
        StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Void(
        Guid envelopeId, VoidAndDeclineEnvelopeRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await _envelopeService.VoidAsync(
                envelopeId, request, cancellationToken);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        //    [HttpPut("{envelopeId:guid}")]
        //    [ProducesResponseType(
        //StatusCodes.Status204NoContent)]
        //    [ProducesResponseType(
        //StatusCodes.Status400BadRequest)]
        //    [ProducesResponseType(
        //StatusCodes.Status404NotFound)]
        //    [ProducesResponseType(
        //StatusCodes.Status409Conflict)]
        //    public async Task<IActionResult> UpdateDraft(
        //Guid envelopeId,
        //EnvelopeDraftRequest request,
        //CancellationToken cancellationToken)
        //    {
        //        try
        //        {
        //            await _envelopeService.UpdateEnvelopeDraftAsync(
        //                envelopeId,
        //                request,
        //                cancellationToken);

        //            return NoContent();
        //        }
        //        catch (KeyNotFoundException)
        //        {
        //            return NotFound();
        //        }
        //        catch (ArgumentException ex)
        //        {
        //            return BadRequest(
        //                ex.Message);
        //        }
        //        catch (InvalidOperationException ex)
        //        {
        //            return Conflict(
        //                ex.Message);
        //        }
        //    }

        [HttpPut("{envelopeId:guid}/documents/order")]
        [ProducesResponseType(
        StatusCodes.Status204NoContent)]
        public async Task<IActionResult> ReorderDocuments(
        Guid envelopeId, ReorderEnvelopeDocumentsRequest request, CancellationToken cancellationToken)
        {
            try
            {
                await _envelopeService.ReOrderDocumentAsync(
                envelopeId, request, cancellationToken);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPost("{envelopeId:guid}/routing-rules")]
        [ProducesResponseType(
        typeof(EnvelopeRoutingRuleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(
        StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
        StatusCodes.Status404NotFound)]
        [ProducesResponseType(
        StatusCodes.Status409Conflict)]
        public async Task<ActionResult<EnvelopeRoutingRuleDto>> CreateRoutingRule(
        Guid envelopeId, EnvelopeRoutingRuleRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var rule = await _envelopeService.CreateRoutingRuleAsync(
                envelopeId, request, cancellationToken);

                return StatusCode(
                StatusCodes.Status201Created, rule);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(
                ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(
                ex.Message);
            }
        }

        [HttpGet("{envelopeId:guid}/routing-rules")]
        [ProducesResponseType(
        typeof(IReadOnlyList<EnvelopeRoutingRuleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(
        StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<EnvelopeRoutingRuleDto>>> GetRoutingRules(
        Guid envelopeId, CancellationToken cancellationToken)
        {
            try
            {
                var rules = await _envelopeService.GetRoutingRulesAsync(
                envelopeId, cancellationToken);

                return Ok(rules);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
        }

        [HttpDelete(
    "{envelopeId:guid}/routing-rules/{ruleId:guid}")]
        [ProducesResponseType(
    StatusCodes.Status204NoContent)]
        [ProducesResponseType(
    StatusCodes.Status404NotFound)]
        [ProducesResponseType(
    StatusCodes.Status409Conflict)]
        public async Task<IActionResult> DeleteRoutingRule(
    Guid envelopeId,
    Guid ruleId,
    CancellationToken cancellationToken)
        {
            try
            {
                await _envelopeService
                    .DeleteRoutingRuleAsync(
                        envelopeId,
                        ruleId,
                        cancellationToken);

                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }

        [HttpPut(
    "{envelopeId:guid}/routing-rules/{ruleId:guid}")]
        [ProducesResponseType(
    typeof(EnvelopeRoutingRuleDto),
    StatusCodes.Status200OK)]
        [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
    StatusCodes.Status404NotFound)]
        [ProducesResponseType(
    StatusCodes.Status409Conflict)]
        public async Task<ActionResult<EnvelopeRoutingRuleDto>>
    UpdateRoutingRule(
        Guid envelopeId,
        Guid ruleId,
        EnvelopeRoutingRuleRequest request,
        CancellationToken cancellationToken)
        {
            try
            {
                var rule =
                    await _envelopeService
                        .UpdateRoutingRuleAsync(
                            envelopeId,
                            ruleId,
                            request,
                            cancellationToken);

                return Ok(rule);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(
                    ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(
                    ex.Message);
            }
        }
    }
}