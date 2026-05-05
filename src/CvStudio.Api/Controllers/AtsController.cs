using Microsoft.AspNetCore.Mvc;
using CvStudio.Application.DTOs;
using CvStudio.Application.Services;

namespace CvStudio.Api.Controllers;

[ApiController]
[Route("api/ats")]
public sealed class AtsController : ControllerBase
{
    private readonly IResumeAtsAnalyzer _analyzer;

    public AtsController(IResumeAtsAnalyzer analyzer)
    {
        _analyzer = analyzer;
    }

    [HttpPost("analyze")]
    [ProducesResponseType(typeof(AtsScoreResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AtsScoreResult>> Analyze(
        [FromBody] AtsAnalyzeRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _analyzer.AnalyzeAsync(
            request.ResumeData,
            request.JobDescription ?? string.Empty,
            request.Category,
            cancellationToken);

        return Ok(result);
    }
}
