using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Components.Shared;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using hlasovanisvj.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.JSInterop;

namespace hlasovanisvj.Components.Pages;

public partial class Resolutions( 
    AppDbContext dbContext,
    IHxMessengerService messengerService) : ComponentBase
{
    private Resolution? currentResolution = null;
    private ResolutionFilter filterModel = new();
    private HxGrid<Resolution> gridComponent;
    
    
    async Task<GridDataProviderResult<Resolution>> GetGridData(GridDataProviderRequest<Resolution> req)
    {
        var data = dbContext.Resolutions.AsQueryable();
        if(!string.IsNullOrWhiteSpace(filterModel.Name))
            data = data.Where(p => p.Title.Contains(filterModel.Name));

        var count = data.Count();
        data = data.ApplyGridDataProviderRequest(req);
		
        return new GridDataProviderResult<Resolution>
        {
            Data = data,
            TotalCount = count
        };
    }

    async Task HandleSelectedDataItemChanged()
    {
        
    }

    private Task HandleNewResolutionClick()
    {
        throw new NotImplementedException();
    }

    private Task HandleOpenResolutionButtonClick()
    {
        throw new NotImplementedException();
    }
}