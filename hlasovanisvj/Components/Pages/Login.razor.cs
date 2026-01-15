using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace hlasovanisvj.Components.Pages;

public partial class Login : ComponentBase
{
    private LoginRequest model = new();
    private string? Error;
    private IJSObjectReference? _mod;
    private bool _didLogout;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _mod = await JS.InvokeAsync<IJSObjectReference>("import", "./auth.js");
            
            if(!_didLogout)
                Nav.NavigateTo("logout", forceLoad: true);
        }
    }

    protected override async Task OnInitializedAsync()
    {
        // capture ReturnUrl if provided
        var uri = Nav.ToAbsoluteUri(Nav.Uri);
        var q = System.Web.HttpUtility.ParseQueryString(uri.Query);
        model.ReturnUrl = q["returnUrl"];
        _didLogout = string.Equals(q["loggedout"], "1", StringComparison.Ordinal);
    }

    private async Task HandleLogin()
    {
        
        var loginUrl = new Uri(new Uri(Nav.BaseUri), "login").ToString(); // handles virtual dirs
        await _mod!.InvokeVoidAsync("postLoginJson", loginUrl, new {
            Username = model.Username,
            Password = model.Password,
            ReturnUrl = model.ReturnUrl
        });
        
        Nav.NavigateTo(model.ReturnUrl ?? "/", forceLoad: true);
        //if (await ((CustomAuthStateProvider)AuthStateProvider).LoginAsync(model.Username, model.Password))
        //    Nav.NavigateTo(model.ReturnUrl ?? "/", forceLoad: true);
        //else
        //    Error = "Sign-in failed.";
    }

}