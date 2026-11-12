using System.Linq.Expressions;
using Ganss.Excel;
using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Components.Shared;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using hlasovanisvj.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.JSInterop;

namespace hlasovanisvj.Components.Pages;

public partial class Members(IJSRuntime jsRuntime,
    IUploadService uploadService, 
    AppDbContext dbContext,
    PdfService pdfService,
    IHxMessengerService messengerService,
    NavigationManager navigationManager,
    IMemoryCache memoryCache,
    DataImportService importService,
    AttendanceService attendanceService, 
    UserService userService,
    MemberService memberService,
    SessionService sessionService)
{
    
    private Member? currentMember = null;
    private MemberDetail memberDetail;
    private MemberFilter filterModel = new();
    private HxGrid<Member> gridComponent;
    private HxOffcanvas importHtmlOffCanvasComponent;
    private HxInputFile hxInputFileComponent;
    private DotNetObjectReference<Members>? objRef;
    private double totalPresentShare = 0;
    private int currentSessionId;
    private Session currentSession;
    

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if(currentSessionId == 0)
            navigationManager.NavigateTo("/");
    }
    

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        
        var user = await userService.GetCurrentUserAsync();
        
        currentSession = await sessionService.GetCurrentSessionAsync(user.OrganizationId);
        
        currentSessionId = currentSession?.Id ?? 0;
        
        totalPresentShare = await attendanceService.GetTotalPresentShareAsync(currentSessionId);
    }

    async Task<GridDataProviderResult<Member>> GetGridData(GridDataProviderRequest<Member> req)
    {
        return memberService.GetAll(req, currentSessionId, filterModel);
    }

    async Task HandleSelectedDataItemChanged()
    {
        memberDetail.Model = currentMember!;
        await memberDetail.ShowAsync();
    }

    private async Task HandleImportHtmlClick()
    {
        await importHtmlOffCanvasComponent.ShowAsync();
    }

    private async Task HandlePrintPdf()
    {
        var members = await memberService.GetMembersAsync(currentSessionId);
        var data = pdfService.GeneratePdf(members);
        
        var guid = Guid.NewGuid();
        memoryCache.Set(guid, data);
        
        var url = $"download?guid={guid}";
        navigationManager.NavigateTo(url, forceLoad: true); 
    }

    private async Task HandleHtmlFileUploaded(UploadCompletedEventArgs fileUploaded)
    {
        var file = fileUploaded.FilesUploaded.FirstOrDefault();
        var data = await uploadService.ReadAllBytesAsync(file.ResponseText.Replace("\"", ""));

        try
        {
            await importService.ImportHtmlAsync(currentSessionId, new MemoryStream(data));

            messengerService.AddInformation("Hotovo!");
        }
        catch (Exception ex)
        {
            messengerService.AddError("Import selhal: " + ex.Message);
        }

        await importHtmlOffCanvasComponent.HideAsync();
        await gridComponent.RefreshDataAsync();
    }

    private async Task HandleUploadHtmlClick()
    {
        await hxInputFileComponent.StartUploadAsync();
    }

    private async Task HandleScanQrClick()
    {
        objRef = DotNetObjectReference.Create(this);
        await jsRuntime.InvokeVoidAsync("qrScanner.start", objRef);
    }
    
    [JSInvokable]
    public async Task OnQrScanned(string text)
    {
        var parts = text.Split("?guid=");
        if (parts.Length != 2 || !Guid.TryParse(parts[1], out var memberId))
        {
            messengerService.AddError("Invalid QR code");
            return;
        }

        var member = dbContext.Members.FirstOrDefault(m => m.GlobalId == memberId);
        if (member == null)
            return;
        
        member.IsPresent = !member.IsPresent;
        await attendanceService.SavePresenceAsync(member.Id, member.IsPresent);
        
        totalPresentShare = await attendanceService.GetTotalPresentShareAsync(currentSessionId);
        
        StateHasChanged();
        
        if(member.IsPresent)
            messengerService.AddInformation($"Přišel:  {member.Name}");
        else
            messengerService.AddWarning($"Odešel:  {member.Name}");
        //await HandleScanQrClick();
    }


    private async Task SavePresenceAsync(Member item)
    {
        await attendanceService.SavePresenceAsync(item.Id, item.IsPresent);
        totalPresentShare = await attendanceService.GetTotalPresentShareAsync(currentSessionId);
        StateHasChanged();
    }

    private async Task HandleDetailClosed(object arg)
    {
        await gridComponent.RefreshDataAsync();
    }

    private async Task HandleExportExcel()
    {
        var members = await memberService.GetMembersAsync(currentSessionId);
        using var memoryStream = new MemoryStream();
        await new ExcelMapper().SaveAsync(memoryStream, members
            .Select(m => new
        {
            m.Id, 
            m.Name,
            m.IsPresent,
             Principals = string.Join(", ", m.Principals.Select(p => p.Name)),
        } ), "Members");
        
        var guid = Guid.NewGuid();
        memoryCache.Set(guid, memoryStream.ToArray());
        
        var url = $"downloadXls?guid={guid}";
        navigationManager.NavigateTo(url, forceLoad: true); 
    }

}