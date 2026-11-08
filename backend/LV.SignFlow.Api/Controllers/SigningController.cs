using LV.SignFlow.Application.Envelopes;
using LV.SignFlow.Application.Signing;
using LV.SignFlow.Application.Signing.Dtos;
using LV.SignFlow.Application.Signing.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LV.SignFlow.Api.Controllers
{
    [Route("api/signing")]
    [ApiController]
    public class SigningController : ControllerBase
    {
        private readonly ISigningService _signingService;
        private readonly IEnvelopeService _envelopeService;

        public SigningController(ISigningService signingService, IEnvelopeService envelopeService)
        {
            _signingService = signingService;
            _envelopeService = envelopeService;
        }

        [HttpGet("{envelopeId:guid}")]
        [ProducesResponseType(typeof(SigningSessionDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<SigningSessionDto>> GetSigningSession(
        Guid envelopeId, CancellationToken cancellationToken)
        {
            try
            {
                var session = await _signingService.GetSigningSessionAsync(
                envelopeId, cancellationToken);

                return Ok(session);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{envelopeId:guid}/fields/{fieldId:guid}/value")]
        public async Task<ActionResult<SigningFieldValueDto>> SaveFieldValue(Guid envelopeId, Guid fieldId, [FromBody] UpdateSigningFieldValueRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _signingService.SaveFieldValueAsync(
                envelopeId, fieldId, request, cancellationToken);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(
                StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }
        [HttpPut("{envelopeId:guid}/fields/{fieldId:guid}/mark")]
        public async Task<ActionResult<SigningAppliedSignatureDto>> ApplyMark(
        Guid envelopeId, Guid fieldId, [FromBody] ApplySigningMarkRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _signingService.ApplyMarkAsync(
                envelopeId, fieldId, request, cancellationToken);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(
                StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(
                ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(
                ex.Message);
            }
        }

        [HttpPost("signatures")]
        public async Task<ActionResult<CreatedSigningSignatureDto>> CreateSignature([FromBody] CreateSigningSignatureRequest request, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _signingService.CreateSignatureAsync(
                request, cancellationToken);

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(
                StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(
                ex.Message);
            }
        }

        [HttpGet("signatures/{signatureId:guid}/content")]
        public async Task<IActionResult> GetSignatureContent(Guid signatureId, CancellationToken cancellationToken)
        {
            try
            {
                var result = await _signingService.GetSignatureContentAsync(
                signatureId, cancellationToken);

                return File(
                result.Content, result.ContentType);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(
                StatusCodes.Status403Forbidden, ex.Message);
            }
        }

        [HttpDelete("{envelopeId:guid}/fields/{fieldId:guid}/mark")]
        public async Task<IActionResult> RemoveMark(Guid envelopeId,Guid fieldId,CancellationToken cancellationToken)
        {
            try
            {
                await _signingService.RemoveMarkAsync(
                    envelopeId,
                    fieldId,
                    cancellationToken);

                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(ex.Message);
            }
        }


        [HttpPost("{envelopeId:guid}/finish")]
        [ProducesResponseType(
    typeof(FinishSigningResultDto),
    StatusCodes.Status200OK)]
        [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
    StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(
    StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
    StatusCodes.Status404NotFound)]
        [ProducesResponseType(
    StatusCodes.Status409Conflict)]
        public async Task<ActionResult<FinishSigningResultDto>>
    FinishSigning(
        Guid envelopeId,
        CancellationToken cancellationToken)
        {
            try
            {
                var result =
                    await _envelopeService.FinishSigningAsync(
                        envelopeId,
                        cancellationToken);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    ex.Message);
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


        [HttpPost("{envelopeId:guid}/decline")]
        [ProducesResponseType(
    typeof(DeclineSigningResultDto),
    StatusCodes.Status200OK)]
        [ProducesResponseType(
    StatusCodes.Status400BadRequest)]
        [ProducesResponseType(
    StatusCodes.Status403Forbidden)]
        [ProducesResponseType(
    StatusCodes.Status404NotFound)]
        [ProducesResponseType(
    StatusCodes.Status409Conflict)]
        public async Task<ActionResult<DeclineSigningResultDto>>
    DeclineSigning(
        Guid envelopeId,
        [FromBody] string Reason,
        CancellationToken cancellationToken)
        {
            try
            {
                var result =
                    await _signingService.DeclineSigningAsync(
                        envelopeId,
                        Reason,
                        cancellationToken);

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(
                    ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    ex.Message);
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