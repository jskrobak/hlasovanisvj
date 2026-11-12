using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace hlasovanisvj.Components.Shared;

/// <summary>Přihlašovací formulář (heslo + volitelně Google) – na stránce /Login i na úvodní stránce.</summary>
public partial class LoginForm(NavigationManager navigationManager,
    IJSRuntime jsRuntime,
    IHxMessengerService messengerService) : ComponentBase, IAsyncDisposable
{
    /// <summary>Kam se vrátit po přihlášení (jen lokální cesta).</summary>
    [Parameter] public string? ReturnUrl { get; set; }

    [Parameter] public InputSize InputSize { get; set; } = InputSize.Regular;
    [Parameter] public ButtonSize ButtonSize { get; set; } = ButtonSize.Regular;

    private readonly LoginRequest model = new();
    private IJSObjectReference? module;
    private bool busy;

    private string Target =>
        !string.IsNullOrEmpty(ReturnUrl) && ReturnUrl.StartsWith('/') && !ReturnUrl.StartsWith("//") && !ReturnUrl.StartsWith("/\\")
            ? ReturnUrl
            : "/Sessions";

    private string GoogleLoginUrl => $"login/google?returnUrl={Uri.EscapeDataString(Target)}";

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
            module = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./auth.js");
    }

    private async Task HandleLogin()
    {
        if (module == null)
            return;

        busy = true;
        try
        {
            var loginUrl = new Uri(new Uri(navigationManager.BaseUri), "login").ToString();
            await module.InvokeVoidAsync("postLoginJson", loginUrl, new { model.Username, model.Password });
            navigationManager.NavigateTo(Target, forceLoad: true);
        }
        catch (JSException)
        {
            messengerService.AddError("Přihlášení se nezdařilo. Zkontrolujte e-mail a heslo.");
            busy = false;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (module != null)
        {
            try { await module.DisposeAsync(); } catch (JSDisconnectedException) { }
        }
    }
}
