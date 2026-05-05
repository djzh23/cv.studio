using Microsoft.AspNetCore.Mvc;
using CvStudio.Application.DTOs;

namespace CvStudio.Api.Controllers;

[ApiController]
[Route("api/access")]
public sealed class AccessController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AccessController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Validates the same passcode as Blazor <c>Access:Passcode</c> (server-side).
    /// </summary>
    [HttpPost("verify")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Verify([FromBody] AccessVerifyRequest? body)
    {
        var expected = _configuration["Access:Passcode"];
        if (string.IsNullOrWhiteSpace(expected))
        {
            return BadRequest(new ProblemDetails
            {
                Title = "Access not configured",
                Detail = "Access:Passcode is not set on the API."
            });
        }

        var input = body?.Passcode?.Trim() ?? string.Empty;
        if (!string.Equals(input, expected.Trim(), StringComparison.Ordinal))
        {
            return Unauthorized();
        }

        return NoContent();
    }
}
