using Microsoft.JSInterop;

namespace CvStudio.Blazor.Services;

public sealed class PasscodeGateService
{
    private const string PasscodeConfigurationKey = "Access:Passcode";

    private readonly IConfiguration _configuration;
    private readonly IJSRuntime _jsRuntime;
    private bool _initialized;

    public PasscodeGateService(IConfiguration configuration, IJSRuntime jsRuntime)
    {
        _configuration = configuration;
        _jsRuntime = jsRuntime;
    }

    public bool HasAccess { get; private set; }

    public async Task EnsureInitializedAsync()
    {
        if (_initialized)
        {
            return;
        }

        try
        {
            HasAccess = await _jsRuntime.InvokeAsync<bool>("CvStudio.getAccessGranted");
        }
        catch
        {
            HasAccess = false;
        }

        _initialized = true;
    }

    public async Task<bool> UnlockAsync(string? inputPasscode)
    {
        var configuredPasscode = _configuration[PasscodeConfigurationKey];
        if (string.IsNullOrWhiteSpace(configuredPasscode))
        {
            return false;
        }

        var isValid = string.Equals(inputPasscode?.Trim(), configuredPasscode.Trim(), StringComparison.Ordinal);
        if (!isValid)
        {
            return false;
        }

        HasAccess = true;
        _initialized = true;
        await _jsRuntime.InvokeVoidAsync("CvStudio.setAccessGranted", true);
        return true;
    }

    public async Task ResetAsync()
    {
        HasAccess = false;
        _initialized = true;
        await _jsRuntime.InvokeVoidAsync("CvStudio.clearAccessGranted");
    }
}
