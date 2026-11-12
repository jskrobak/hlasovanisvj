using hlasovanisvj.Models;
using hlasovanisvj.Services;
using Microsoft.AspNetCore.Components;

namespace hlasovanisvj.Components.Pages;

public partial class SessionView(UserService userService, SessionReportService reportService) : ComponentBase
{
    [Parameter] public int SessionId { get; set; }

    private SessionReport? report;
    private bool notFound;

    protected override async Task OnParametersSetAsync()
    {
        var user = await userService.GetCurrentUserAsync();
        if (user == null)
        {
            notFound = true;
            return;
        }

        report = await reportService.GetReportAsync(SessionId, user.Organization.Id);
        notFound = report == null;
    }
}
