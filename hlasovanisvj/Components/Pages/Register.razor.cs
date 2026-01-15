using Havit.Blazor.Components.Web;
using Havit.Blazor.Components.Web.Bootstrap;
using hlasovanisvj.Data;
using hlasovanisvj.Domain;
using hlasovanisvj.Models;
using hlasovanisvj.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace hlasovanisvj.Components.Pages;

public partial class Register(
    AppDbContext dbContext,
    IHxMessengerService messengerService,
    ISecurityService securityService) : ComponentBase
{
    private readonly UserRegisterModel _model = new();
    private EditForm? _editForm;

    private async Task RegisterUser()
    {
        try
        {
            if (dbContext.Users.Any(u => u.Email == _model.Email))
                throw new Exception("Email is already registered.");

            var user = new User
            {
                Email = _model.Email,
                PasswordHash = securityService.HashPassword(_model.Password),
                Roles = new List<string> { "User" },
                Organization = new Organization
                {
                    Name = _model.OrganizationName.Trim()
                }
            };
            
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync();
            
            messengerService.AddInformation("Registration successful. You can now log in.");
        }
        catch (Exception ex)
        {
            messengerService.AddError(ex.Message);
        }
    }
}