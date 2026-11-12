using Microsoft.AspNetCore.Components;

namespace hlasovanisvj.Components.Pages;

public partial class Login(NavigationManager navigationManager) : ComponentBase
{
    private string? returnUrl;
    private bool googleError;
    private bool didLogout;

    protected override void OnInitialized()
    {
        var uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);
        var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
        returnUrl = q["returnUrl"] ?? q["ReturnUrl"];
        didLogout = string.Equals(q["loggedout"], "1", StringComparison.Ordinal);
        googleError = string.Equals(q["error"], "google", StringComparison.Ordinal);
    }

    protected override void OnAfterRender(bool firstRender)
    {
        // Původní chování: příchod na /Login nejdřív smaže případnou starou cookie.
        if (firstRender && !didLogout)
            navigationManager.NavigateTo(string.IsNullOrEmpty(returnUrl)
                ? "logout"
                : $"logout?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
    }
}
