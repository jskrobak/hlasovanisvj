using System.ComponentModel.DataAnnotations;

namespace hlasovanisvj.Models;

public class UserRegisterModel
{
    [Required, EmailAddress]
    public string Email { get; set; }
    
    [Required, DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    public string Password { get; set; }
    
    [Required, DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string Password2 { get; set; }
    
    [Required, StringLength(200, MinimumLength = 2)]
    public string OrganizationName { get; set; }
}