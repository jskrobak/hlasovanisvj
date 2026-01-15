using System.Linq.Expressions;
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
    MemberService memberService)
{
    
    private Member? currentMember = null;
    private MemberDetail memberDetail;
    private MemberFilter filterModel = new();
    private HxGrid<Member> gridComponent;
    private HxOffcanvas importHtmlOffCanvasComponent;
    private HxInputFile hxInputFileComponent;
    private DotNetObjectReference<Members>? objRef;
    private double totalPresentShare = 0;
    

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        //totalPresentShare = await AttendanceService.GetTotalPresentShareAsync();
    }

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        totalPresentShare = await attendanceService.GetTotalPresentShareAsync();
    }

    async Task<GridDataProviderResult<Member>> GetGridData(GridDataProviderRequest<Member> req)
    {
        var user = await userService.GetCurrentUserAsync();
        return memberService.GetGridData(req, user.OrganizationId, filterModel);
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
        var user = await userService.GetCurrentUserAsync();
        var data = memberService.GetMembersAsync(user.OrganizationId);
        
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
            var user = await userService.GetCurrentUserAsync();
            await importService.ImportHtmlAsync(user.Organization, new MemoryStream(data));

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
        if(!Guid.TryParse(text, out var memberId))
        {
            messengerService.AddError("Invalid QR code");
            return;
        }
        
        var member = dbContext.Members.FirstOrDefault(m => m.GlobalId == memberId);
        if (member == null)
            return;
        member.IsPresent = !member.IsPresent;
        await attendanceService.SavePresenceAsync(member);
        
        totalPresentShare = await attendanceService.GetTotalPresentShareAsync();
        
        StateHasChanged();
    }


    private async Task SavePresenceAsync(Member item)
    {
        await attendanceService.SavePresenceAsync(item);
        totalPresentShare = await attendanceService.GetTotalPresentShareAsync();
        StateHasChanged();
    }
}