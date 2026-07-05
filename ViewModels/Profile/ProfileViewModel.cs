using System.ComponentModel.DataAnnotations;

namespace Stalika.Web.ViewModels.Profile;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Введите имя")]
    public string FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; }

    [Required(ErrorMessage = "Введите email")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите телефон")]
    public string Phone { get; set; } = string.Empty;

    public string RoleName { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }
}