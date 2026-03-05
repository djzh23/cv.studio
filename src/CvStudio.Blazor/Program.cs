using CvStudio.Blazor.Components;
using CvStudio.Blazor.Services;
using CvStudio.Blazor.ViewModels;

const string ApiBaseUrlKey = "ApiBaseUrl";
const string DefaultApiBaseUrl = "https://localhost:7212/";

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddHttpClient<ResumeApiClient>(client =>
{
    var baseUrl = builder.Configuration[ApiBaseUrlKey] ?? DefaultApiBaseUrl;
    client.BaseAddress = new Uri(baseUrl);
});
builder.Services.AddScoped<ResumeEditorViewModel>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

