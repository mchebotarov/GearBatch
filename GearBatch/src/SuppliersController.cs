using Microsoft.AspNetCore.Mvc;

namespace GearBatch.src
{
    [Route("api/v1/suppliers")]
    [ApiController]
    public class SuppliersController : ControllerBase
    {

        [HttpPost("{suppliersId}/imports")]
        [Consumes("multipart/form-data")]
        public async Task<ActionResult> CreateImport([FromRoute] string suppliersId, [FromForm] IFormFile? file,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey)
        {
            if (string.IsNullOrEmpty(idempotencyKey) ||
    idempotencyKey.Length > 64 ||
    idempotencyKey.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '-' && c != '_'))
            {
                return BadRequest(new
                {
                    error = "Idempotency-Key must be 1 to 64 characters and contain only ASCII letters, digits, hyphens, or underscores."
                });
            }

            var form = await Request.ReadFormAsync(HttpContext.RequestAborted);

            if (file is null ||
                form.Files.Count != 1 ||
                file.Name != "file" ||
                form.Count != 0)
            {
                return BadRequest(new
                {
                    error = "Send exactly one file part named 'file' and no other form fields."
                });
            }

            var fileName = file.FileName;

            if (fileName.Length is < 1 or > 120 ||
                fileName.Contains('/') ||
                fileName.Contains('\\') ||
                fileName.Any(char.IsControl))
            {
                return BadRequest(new { error = "The uploaded filename is invalid." });
            }

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { error = "The uploaded file must end in .csv." });
            }

            if (file.Length == 0)
            {
                return BadRequest(new { error = "The uploaded CSV file is empty." });
            }

            const long maxFileSizeBytes = 1024 * 1024;

            if (file.Length > maxFileSizeBytes)
            {
                return BadRequest(new { error = "The uploaded CSV file must be 1 MB or smaller." });
            }

            return StatusCode(StatusCodes.Status501NotImplemented);
        }
    }
}
