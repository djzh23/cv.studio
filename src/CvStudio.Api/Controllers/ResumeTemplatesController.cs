using Microsoft.AspNetCore.Mvc;
using CvStudio.Application.DTOs;
using CvStudio.Application.Services;

namespace CvStudio.Api.Controllers;

[ApiController]
[Route("api/resume-templates")]
public sealed class ResumeTemplatesController : ControllerBase
{
    private readonly IResumeService _resumeService;

    public ResumeTemplatesController(IResumeService resumeService)
    {
        _resumeService = resumeService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ResumeTemplateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ResumeTemplateDto>>> List(CancellationToken cancellationToken)
    {
        var templates = await _resumeService.ListTemplatesAsync(cancellationToken);
        return Ok(templates);
    }
}

