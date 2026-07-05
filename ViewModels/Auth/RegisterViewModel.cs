using System.ComponentModel.DataAnnotations;

namespace Stalika.Web.ViewModels.Auth;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Введите имя")]
    public string FirstName { get; set; } = string.Empty;

    public string? LastName { get; set; }

    [Required(ErrorMessage = "Введите email")]
    [EmailAddress(ErrorMessage = "Некорректный email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите телефон")]
    public string Phone { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите пароль")]
    [MinLength(6, ErrorMessage = "Пароль должен быть не короче 6 символов")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Повторите пароль")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
}