using Microsoft.JSInterop;
using Microsoft.Extensions.Logging;
using CvStudio.Application.Contracts;
using CvStudio.Application.DTOs;
using CvStudio.Application.Services;
using CvStudio.Blazor.Services;

namespace CvStudio.Blazor.ViewModels;

public sealed class ResumeEditorViewModel : IDisposable
{
    private readonly ResumeApiClient _apiClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly ILogger<ResumeEditorViewModel> _logger;
    private readonly object _autoSaveLock = new();
    private CancellationTokenSource? _autoSaveCts;

    public ResumeEditorViewModel(
        ResumeApiClient apiClient,
        IJSRuntime jsRuntime,
        ILogger<ResumeEditorViewModel> logger)
    {
        _apiClient = apiClient;
        _jsRuntime = jsRuntime;
        _logger = logger;
    }

    public event Action? StateChanged;

    public ResumeDto? CurrentResume { get; private set; }
    public IReadOnlyList<ResumeSummaryDto> Arbeitsversionen { get; private set; } = [];
    public ResumeVersionDto? AktiveVariante { get; private set; }
    public IReadOnlyList<ResumeVersionDto> GespeicherteVarianten { get; private set; } = [];
    public IReadOnlyList<ResumeTemplateDto> Vorlagen { get; private set; } = [];
    public string GewaehlteVorlage { get; set; } = string.Empty;
    public bool IsBusy { get; private set; }
    public bool AutoSaveLaeuft { get; private set; }
    public string? ErrorMessage { get; private set; }
    public string VariantenNameEntwurf { get; set; } = string.Empty;
    public DateTime? LastAutoSavedAtUtc { get; private set; }
    public bool HasUnsavedChanges { get; private set; }
    public PdfDesign AusgewaehltesPdfDesign { get; set; } = PdfDesign.DesignA;
    public string AktivKontextText => AktiveVariante is null
        ? "Du bearbeitest: Arbeitsversion"
        : $"Du bearbeitest: Gespeicherte Variante {FormatVariantenName(AktiveVariante)}";

    public string AutoSaveText
    {
        get
        {
            if (AutoSaveLaeuft)
            {
                return "Auto-Save: speichert...";
            }

            if (HasUnsavedChanges)
            {
                return "Auto-Save: wartend";
            }

            return LastAutoSavedAtUtc.HasValue
                ? $"Auto-Save: gespeichert ({LastAutoSavedAtUtc.Value.ToLocalTime():HH:mm:ss})"
                : "Auto-Save: bereit";
        }
    }

    public async Task LoadTemplatesAsync(CancellationToken cancellationToken = default)
    {
        await RunBusyAsync(async () =>
        {
            Vorlagen = await _apiClient.GetTemplatesAsync(cancellationToken);
            Arbeitsversionen = await _apiClient.ListResumesAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(GewaehlteVorlage))
            {
                GewaehlteVorlage = Vorlagen.FirstOrDefault()?.Key ?? string.Empty;
            }
        });
    }

    public async Task<Guid> NeueArbeitsversionErstellenAsync(CancellationToken cancellationToken = default)
    {
        return await RunBusyAsync(async () =>
        {
            ResumeDto erstellt;
            if (!string.IsNullOrWhiteSpace(GewaehlteVorlage))
            {
                erstellt = await _apiClient.CreateResumeFromTemplateAsync(GewaehlteVorlage, cancellationToken);
            }
            else
            {
                erstellt = await _apiClient.CreateResumeAsync(new CreateResumeRequest
                {
                    Title = "Neue Arbeitsversion",
                    ResumeData = CreateFallbackData()
                }, cancellationToken);
            }

            CurrentResume = erstellt;
            AktiveVariante = null;
            HasUnsavedChanges = false;
            await RefreshVariantenAsync(cancellationToken);
            await RefreshArbeitsversionenAsync(cancellationToken);
            await PersistLastResumeIdAsync(erstellt.Id, cancellationToken);
            return erstellt.Id;
        });
    }

    public async Task ArbeitsversionLadenAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await RunBusyAsync(async () =>
        {
            CurrentResume = await _apiClient.GetResumeAsync(id, cancellationToken);
            if (!string.IsNullOrWhiteSpace(CurrentResume.TemplateKey))
            {
                GewaehlteVorlage = CurrentResume.TemplateKey;
            }

            AktiveVariante = null;
            HasUnsavedChanges = false;
            await RefreshVariantenAsync(cancellationToken);
            await RefreshArbeitsversionenAsync(cancellationToken);
            await PersistLastResumeIdAsync(CurrentResume.Id, cancellationToken);
        });
    }

    public async Task<Guid?> TryGetLastResumeIdAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var raw = await _jsRuntime.InvokeAsync<string?>("CvStudio.getLastResumeId", cancellationToken);
            if (Guid.TryParse(raw, out var parsed))
            {
                return parsed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not read last resume id from browser storage.");
        }

        return null;
    }

    public void OnEditorChanged()
    {
        if (CurrentResume is null)
        {
            return;
        }

        HasUnsavedChanges = true;
        AktiveVariante = null;
        QueueAutoSave();
        NotifyStateChanged();
    }

    public async Task<ResumeVersionDto?> VarianteSpeichernAsync(string? name = null, CancellationToken cancellationToken = default)
    {
        if (CurrentResume is null)
        {
            return null;
        }

        await FlushAutoSaveAsync(cancellationToken);

        return await RunBusyAsync(async () =>
        {
            var label = string.IsNullOrWhiteSpace(name) ? VariantenNameEntwurf : name;
            var variante = await _apiClient.CreateVersionAsync(CurrentResume.Id, new CreateVersionRequest
            {
                Label = string.IsNullOrWhiteSpace(label) ? null : label.Trim()
            }, cancellationToken);

            VariantenNameEntwurf = string.Empty;
            AktiveVariante = variante;
            HasUnsavedChanges = false;
            await RefreshVariantenAsync(cancellationToken);
            await RefreshArbeitsversionenAsync(cancellationToken);
            await PersistLastResumeIdAsync(CurrentResume.Id, cancellationToken);
            return variante;
        });
    }

    public async Task VarianteInEditorLadenAsync(Guid variantenId, CancellationToken cancellationToken = default)
    {
        if (CurrentResume is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            var variante = await _apiClient.GetVersionAsync(CurrentResume.Id, variantenId, cancellationToken);
            CurrentResume.ResumeData = variante.ResumeData;
            CurrentResume.UpdatedAtUtc = DateTime.UtcNow;
            AktiveVariante = variante;
            HasUnsavedChanges = true;
            QueueAutoSave();
            await PersistLastResumeIdAsync(CurrentResume.Id, cancellationToken);
        });
    }

    public async Task VarianteUmbenennenAsync(ResumeVersionDto variante, CancellationToken cancellationToken = default)
    {
        if (CurrentResume is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _apiClient.UpdateVersionAsync(CurrentResume.Id, variante.Id, new UpdateVersionRequest
            {
                Label = string.IsNullOrWhiteSpace(variante.Label) ? null : variante.Label.Trim()
            }, cancellationToken);
            await RefreshVariantenAsync(cancellationToken);
        });
    }

    public async Task VarianteLoeschenAsync(ResumeVersionDto variante, CancellationToken cancellationToken = default)
    {
        if (CurrentResume is null)
        {
            return;
        }

        await RunBusyAsync(async () =>
        {
            await _apiClient.DeleteVersionAsync(CurrentResume.Id, variante.Id, cancellationToken);
            if (AktiveVariante?.Id == variante.Id)
            {
                AktiveVariante = null;
            }
            await RefreshVariantenAsync(cancellationToken);
        });
    }

    public async Task<byte[]> ExportArbeitsversionPdfAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentResume is null)
        {
            return [];
        }

        await FlushAutoSaveAsync(cancellationToken);
        return await _apiClient.DownloadPdfAsync(CurrentResume.Id, null, AusgewaehltesPdfDesign, cancellationToken);
    }

    public async Task<byte[]> ExportArbeitsversionDocxAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentResume is null)
        {
            return [];
        }

        await FlushAutoSaveAsync(cancellationToken);
        return await _apiClient.DownloadDocxAsync(CurrentResume.Id, null, cancellationToken);
    }

    public async Task<byte[]> ExportVariantePdfAsync(Guid variantenId, CancellationToken cancellationToken = default)
    {
        if (CurrentResume is null)
        {
            return [];
        }

        return await _apiClient.DownloadPdfAsync(CurrentResume.Id, variantenId, AusgewaehltesPdfDesign, cancellationToken);
    }

    public async Task<byte[]> ExportVarianteDocxAsync(Guid variantenId, CancellationToken cancellationToken = default)
    {
        if (CurrentResume is null)
        {
            return [];
        }

        return await _apiClient.DownloadDocxAsync(CurrentResume.Id, variantenId, cancellationToken);
    }

    public async Task ResetAllDataAsync(CancellationToken cancellationToken = default)
    {
        await RunBusyAsync(async () =>
        {
            await _apiClient.DeleteAllResumesAsync(cancellationToken);
            CurrentResume = null;
            AktiveVariante = null;
            GespeicherteVarianten = [];
            HasUnsavedChanges = false;
            await RefreshArbeitsversionenAsync(cancellationToken);
            try
            {
                await _jsRuntime.InvokeVoidAsync("CvStudio.clearLastResumeId", cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not clear last resume id in browser storage.");
            }
        });
    }

    public async Task FlushAutoSaveAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentResume is null || !HasUnsavedChanges)
        {
            return;
        }

        await SaveCurrentArbeitsversionAsync(cancellationToken, force: true);
    }

    private void QueueAutoSave()
    {
        lock (_autoSaveLock)
        {
            _autoSaveCts?.Cancel();
            _autoSaveCts?.Dispose();
            _autoSaveCts = new CancellationTokenSource();
            var token = _autoSaveCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1000, token);
                    await SaveCurrentArbeitsversionAsync(token);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogDebug("Auto-save debounce was canceled.");
                }
            }, token);
        }
    }

    private async Task SaveCurrentArbeitsversionAsync(CancellationToken cancellationToken, bool force = false)
    {
        if (CurrentResume is null)
        {
            return;
        }

        if (!force && !HasUnsavedChanges)
        {
            return;
        }

        try
        {
            AutoSaveLaeuft = true;
            NotifyStateChanged();

            CurrentResume = await _apiClient.UpdateResumeAsync(CurrentResume.Id, new UpdateResumeRequest
            {
                Title = CurrentResume.Title,
                TemplateKey = string.IsNullOrWhiteSpace(GewaehlteVorlage) ? CurrentResume.TemplateKey : GewaehlteVorlage,
                ResumeData = CurrentResume.ResumeData
            }, cancellationToken);

            LastAutoSavedAtUtc = DateTime.UtcNow;
            HasUnsavedChanges = false;
            await RefreshArbeitsversionenAsync(cancellationToken);
            await PersistLastResumeIdAsync(CurrentResume.Id, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            AutoSaveLaeuft = false;
            NotifyStateChanged();
        }
    }

    private async Task RefreshVariantenAsync(CancellationToken cancellationToken)
    {
        if (CurrentResume is null)
        {
            GespeicherteVarianten = [];
            return;
        }

        GespeicherteVarianten = await _apiClient.ListVersionsAsync(CurrentResume.Id, cancellationToken);
    }

    public async Task RefreshArbeitsversionenAsync(CancellationToken cancellationToken = default)
    {
        Arbeitsversionen = await _apiClient.ListResumesAsync(cancellationToken);
    }

    public static string FormatVariantenName(ResumeVersionDto variante)
    {
        var basis = string.IsNullOrWhiteSpace(variante.Label) ? "Ohne Namen" : variante.Label;
        return $"{basis} v{variante.VersionNumber}";
    }

    private async Task RunBusyAsync(Func<Task> operation)
    {
        try
        {
            ErrorMessage = null;
            IsBusy = true;
            NotifyStateChanged();
            await operation();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            NotifyStateChanged();
        }
    }

    private async Task<T> RunBusyAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            ErrorMessage = null;
            IsBusy = true;
            NotifyStateChanged();
            return await operation();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            return default!;
        }
        finally
        {
            IsBusy = false;
            NotifyStateChanged();
        }
    }

    private static ResumeData CreateFallbackData()
    {
        return new ResumeData
        {
            Profile = new ProfileData
            {
                FirstName = "Max",
                LastName = "Mustermann",
                Headline = "Profil",
                Email = "max@example.com",
                Phone = "+49 170 0000000",
                Location = "Deutschland",
                Summary = "Kurzprofil"
            }
        };
    }

    private async Task PersistLastResumeIdAsync(Guid resumeId, CancellationToken cancellationToken)
    {
        try
        {
            await _jsRuntime.InvokeVoidAsync("CvStudio.setLastResumeId", cancellationToken, resumeId.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not persist last resume id in browser storage.");
        }
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();

    public void Dispose()
    {
        lock (_autoSaveLock)
        {
            _autoSaveCts?.Cancel();
            _autoSaveCts?.Dispose();
            _autoSaveCts = null;
        }
    }
}

