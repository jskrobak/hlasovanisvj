using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;

namespace hlasovanisvj.Components.Pages;

public partial class Login(NavigationManager navigationManager,
    AuthenticationStateProvider authStateProvider,
    IJSRuntime jsRuntime,
    IHxMessengerService messengerService) : ComponentBase
{
    private LoginRequest model = new();
    private IJSObjectReference? _mod;
    private bool _didLogout;
    private bool googleError;

    private string GoogleLoginUrl =>
        "login/google" + (string.IsNullOrEmpty(model.ReturnUrl) ? "" : $"?returnUrl={Uri.EscapeDataString(model.ReturnUrl)}");

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _mod = await jsRuntime.InvokeAsync<IJSObjectReference>("import", "./auth.js");
            
            if(!_didLogout)
                navigationManager.NavigateTo("logout", forceLoad: true);
        }
    }

    protected override async Task OnInitializedAsync()
    {
        // capture ReturnUrl if provided
        var uri = navigationManager.ToAbsoluteUri(navigationManager.Uri);
        var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
        model.ReturnUrl = q["returnUrl"];
        _didLogout = string.Equals(q["loggedout"], "1", StringComparison.Ordinal);
        googleError = string.Equals(q["error"], "google", StringComparison.Ordinal);
    }

    private async Task HandleLogin()
    {
        
        var loginUrl = new Uri(new Uri(navigationManager.BaseUri), "login").ToString(); // handles virtual dirs

        try
        {
            await _mod!.InvokeVoidAsync("postLoginJson", loginUrl, new
            {
                Username = model.Username,
                Password = model.Password,
                ReturnUrl = model.ReturnUrl
            });
            
            navigationManager.NavigateTo(model.ReturnUrl ?? "/", forceLoad: true);
        }
        catch (JSException jsEx)
        {
            messengerService.AddError("Přihlášení se nezdařilo!");
        }

        
        //if (await ((CustomAuthStateProvider)AuthStateProvider).LoginAsync(model.Username, model.Password))
        //    Nav.NavigateTo(model.ReturnUrl ?? "/", forceLoad: true);
        //else
        //    Error = "Sign-in failed.";
    }

}