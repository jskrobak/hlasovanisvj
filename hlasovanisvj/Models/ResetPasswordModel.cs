using System.ComponentModel.DataAnnotations;

namespace hlasovanisvj.Models;

public class ForgotPasswordModel
{
    [Required(ErrorMessage = "Zadejte e-mail."), EmailAddress(ErrorMessage = "Neplatný e-mail.")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordModel
{
    [Required(ErrorMessage = "Zadejte heslo."), DataType(DataType.Password)]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Heslo musí mít alespoň 6 znaků.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Zopakujte heslo."), DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Hesla se neshodují.")]
    public string Password2 { get; set; } = string.Empty;
}
