using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.WebUtilities;
using CvStudio.Application.DTOs;
using CvStudio.Application.Services;

namespace CvStudio.Blazor.Services;

public sealed class ResumeApiClient
{
    private const string ResumeTemplatesEndpoint = "api/resume-templates";
    private const string ResumesEndpoint = "api/resumes";
    private const string VersionsSegment = "versions";
    private const string PdfSegment = "pdf";
    private const string DocxSegment = "docx";
    private const string TemplatesSegment = "templates";
    private const string VersionIdQueryKey = "versionId";
    private const string DesignQueryKey = "design";
    private const string DesignA = "A";
    private const string DesignB = "B";
    private const string DesignC = "C";

    private readonly HttpClient _httpClient;

    public ResumeApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IReadOnlyList<ResumeTemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(ResumeTemplatesEndpoint, cancellationToken);
        return await ReadOrThrowAsync<List<ResumeTemplateDto>>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<ResumeSummaryDto>> ListResumesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(ResumesEndpoint, cancellationToken);
        return await ReadOrThrowAsync<List<ResumeSummaryDto>>(response, cancellationToken);
    }

    public async Task DeleteAllResumesAsync(CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync(ResumesEndpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }
    }

    public async Task<ResumeDto> CreateResumeAsync(CreateResumeRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(ResumesEndpoint, request, cancellationToken);
        return await ReadOrThrowAsync<ResumeDto>(response, cancellationToken);
    }

    public async Task<ResumeDto> CreateResumeFromTemplateAsync(string templateKey, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsync($"{ResumesEndpoint}/{TemplatesSegment}/{templateKey}", null, cancellationToken);
        return await ReadOrThrowAsync<ResumeDto>(response, cancellationToken);
    }

    public async Task<ResumeDto> GetResumeAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{ResumesEndpoint}/{id}", cancellationToken);
        return await ReadOrThrowAsync<ResumeDto>(response, cancellationToken);
    }

    public async Task<ResumeDto> UpdateResumeAsync(Guid id, UpdateResumeRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"{ResumesEndpoint}/{id}", request, cancellationToken);
        return await ReadOrThrowAsync<ResumeDto>(response, cancellationToken);
    }

    public async Task<ResumeVersionDto> CreateVersionAsync(Guid id, CreateVersionRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{ResumesEndpoint}/{id}/{VersionsSegment}", request, cancellationToken);
        return await ReadOrThrowAsync<ResumeVersionDto>(response, cancellationToken);
    }

    public async Task<IReadOnlyList<ResumeVersionDto>> ListVersionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{ResumesEndpoint}/{id}/{VersionsSegment}", cancellationToken);
        return await ReadOrThrowAsync<List<ResumeVersionDto>>(response, cancellationToken);
    }

    public async Task<ResumeVersionDto> GetVersionAsync(Guid id, Guid versionId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{ResumesEndpoint}/{id}/{VersionsSegment}/{versionId}", cancellationToken);
        return await ReadOrThrowAsync<ResumeVersionDto>(response, cancellationToken);
    }

    public async Task<ResumeVersionDto> UpdateVersionAsync(Guid id, Guid versionId, UpdateVersionRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PutAsJsonAsync($"{ResumesEndpoint}/{id}/{VersionsSegment}/{versionId}", request, cancellationToken);
        return await ReadOrThrowAsync<ResumeVersionDto>(response, cancellationToken);
    }

    public async Task DeleteVersionAsync(Guid id, Guid versionId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.DeleteAsync($"{ResumesEndpoint}/{id}/{VersionsSegment}/{versionId}", cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }
    }

    public async Task<byte[]> DownloadPdfAsync(Guid id, Guid? versionId = null, PdfDesign design = PdfDesign.DesignA, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{ResumesEndpoint}/{id}/{PdfSegment}";
        var query = new Dictionary<string, string?>();
        if (versionId.HasValue)
        {
            query[VersionIdQueryKey] = versionId.Value.ToString();
        }

        query[DesignQueryKey] = design switch
        {
            PdfDesign.DesignB => DesignB,
            PdfDesign.DesignC => DesignC,
            _ => DesignA
        };
        endpoint = QueryHelpers.AddQueryString(endpoint, query);

        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<byte[]> DownloadDocxAsync(Guid id, Guid? versionId = null, CancellationToken cancellationToken = default)
    {
        var endpoint = $"{ResumesEndpoint}/{id}/{DocxSegment}";
        if (versionId.HasValue)
        {
            endpoint = QueryHelpers.AddQueryString(endpoint, new Dictionary<string, string?>
            {
                [VersionIdQueryKey] = versionId.Value.ToString()
            });
        }

        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }

        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    private static async Task<T> ReadOrThrowAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw await CreateExceptionAsync(response, cancellationToken);
        }

        var result = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
        if (result is null)
        {
            throw new InvalidOperationException("API returned empty content.");
        }

        return result;
    }

    private static async Task<ApiClientException> CreateExceptionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var message = string.IsNullOrWhiteSpace(body) ? response.ReasonPhrase ?? "API request failed." : body;
        return new ApiClientException(response.StatusCode, message);
    }
}

public sealed class ApiClientException : Exception
{
    public ApiClientException(HttpStatusCode statusCode, string message) : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
