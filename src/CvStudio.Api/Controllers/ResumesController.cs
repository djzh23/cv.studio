using Microsoft.AspNetCore.Mvc;
using CvStudio.Application.DTOs;
using CvStudio.Application.Services;

namespace CvStudio.Api.Controllers;

[ApiController]
[Route("api/resumes")]
public sealed class ResumesController : ControllerBase
{
    private const string PdfContentType = "application/pdf";
    private const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string PdfDesignBShort = "B";
    private const string PdfDesignCShort = "C";
    private const string PdfDesignBName = "DESIGNB";
    private const string PdfDesignCName = "DESIGNC";

    private readonly IResumeService _resumeService;
    private readonly ISnapshotService _snapshotService;
    private readonly IPdfExportService _pdfExportService;
    private readonly IDocxExportService _docxExportService;
    private readonly ILogger<ResumesController> _logger;

    public ResumesController(
        IResumeService resumeService,
        ISnapshotService snapshotService,
        IPdfExportService pdfExportService,
        IDocxExportService docxExportService,
        ILogger<ResumesController> logger)
    {
        _resumeService = resumeService;
        _snapshotService = snapshotService;
        _pdfExportService = pdfExportService;
        _docxExportService = docxExportService;
        _logger = logger;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ResumeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateResumeRequest request, CancellationToken cancellationToken)
    {
        var resume = await _resumeService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCurrent), new { id = resume.Id }, resume);
    }

    [HttpPost("templates/{templateKey}")]
    [ProducesResponseType(typeof(ResumeDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateFromTemplate(string templateKey, CancellationToken cancellationToken)
    {
        var resume = await _resumeService.CreateFromTemplateAsync(templateKey, cancellationToken);
        return CreatedAtAction(nameof(GetCurrent), new { id = resume.Id }, resume);
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ResumeSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ResumeSummaryDto>>> List(CancellationToken cancellationToken)
    {
        var resumes = await _resumeService.ListAsync(cancellationToken);
        return Ok(resumes);
    }

    [HttpDelete]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAll(CancellationToken cancellationToken)
    {
        await _resumeService.DeleteAllAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ResumeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResumeDto>> GetCurrent(Guid id, CancellationToken cancellationToken)
    {
        var resume = await _resumeService.GetCurrentAsync(id, cancellationToken);
        return Ok(resume);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ResumeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ResumeDto>> Update(Guid id, [FromBody] UpdateResumeRequest request, CancellationToken cancellationToken)
    {
        var resume = await _resumeService.UpdateAsync(id, request, cancellationToken);
        return Ok(resume);
    }

    [HttpPost("{id:guid}/versions")]
    [ProducesResponseType(typeof(ResumeVersionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ResumeVersionDto>> CreateVersion(Guid id, [FromBody] CreateVersionRequest? request, CancellationToken cancellationToken)
    {
        var normalized = request ?? new CreateVersionRequest();
        var version = await _snapshotService.CreateSnapshotAsync(id, normalized, cancellationToken);
        return CreatedAtAction(nameof(GetVersion), new { id, versionId = version.Id }, version);
    }

    [HttpGet("{id:guid}/versions")]
    [ProducesResponseType(typeof(IReadOnlyList<ResumeVersionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ResumeVersionDto>>> ListVersions(Guid id, CancellationToken cancellationToken)
    {
        var versions = await _snapshotService.ListSnapshotsAsync(id, cancellationToken);
        return Ok(versions);
    }

    [HttpGet("{id:guid}/versions/{versionId:guid}")]
    [ProducesResponseType(typeof(ResumeVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ResumeVersionDto>> GetVersion(Guid id, Guid versionId, CancellationToken cancellationToken)
    {
        var version = await _snapshotService.GetSnapshotAsync(id, versionId, cancellationToken);
        return Ok(version);
    }

    [HttpPut("{id:guid}/versions/{versionId:guid}")]
    [ProducesResponseType(typeof(ResumeVersionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ResumeVersionDto>> UpdateVersion(Guid id, Guid versionId, [FromBody] UpdateVersionRequest request, CancellationToken cancellationToken)
    {
        var version = await _snapshotService.UpdateSnapshotAsync(id, versionId, request, cancellationToken);
        return Ok(version);
    }

    [HttpDelete("{id:guid}/versions/{versionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVersion(Guid id, Guid versionId, CancellationToken cancellationToken)
    {
        await _snapshotService.DeleteSnapshotAsync(id, versionId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{id:guid}/pdf")]
    [Produces(PdfContentType)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPdf(Guid id, [FromQuery] Guid? versionId, [FromQuery] string? design, CancellationToken cancellationToken)
    {
        var parsedDesign = ParsePdfDesign(design);
        var pdf = await _pdfExportService.ExportAsync(id, versionId, parsedDesign, cancellationToken);
        var fileName = versionId.HasValue ? $"resume-{id}-v{versionId}.pdf" : $"resume-{id}.pdf";

        _logger.LogInformation("Returning PDF for resume {ResumeId}, version {VersionId}, design {Design}", id, versionId, parsedDesign);

        return File(pdf, PdfContentType, fileName);
    }

    [HttpGet("{id:guid}/docx")]
    [Produces(DocxContentType)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadDocx(Guid id, [FromQuery] Guid? versionId, CancellationToken cancellationToken)
    {
        var docx = await _docxExportService.ExportAsync(id, versionId, cancellationToken);
        var fileName = versionId.HasValue ? $"resume-{id}-v{versionId}.docx" : $"resume-{id}.docx";

        _logger.LogInformation("Returning DOCX for resume {ResumeId}, version {VersionId}", id, versionId);

        return File(docx, DocxContentType, fileName);
    }

    private static PdfDesign ParsePdfDesign(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return PdfDesign.DesignA;
        }

        return raw.Trim().ToUpperInvariant() switch
        {
            PdfDesignBShort => PdfDesign.DesignB,
            PdfDesignBName => PdfDesign.DesignB,
            PdfDesignCShort => PdfDesign.DesignC,
            PdfDesignCName => PdfDesign.DesignC,
            _ => PdfDesign.DesignA
        };
    }
}
