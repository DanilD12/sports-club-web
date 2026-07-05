using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Stalika.Web.ViewModels.Admin;

public class AdminTrainerEditViewModel
{
    public int? TrainerId { get; set; }

    [Required(ErrorMessage = "Выберите пользователя")]
    public int UserId { get; set; }

    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "Ставка не может быть отрицательной")]
    public decimal HourlyRate { get; set; }

    public string? Qualification { get; set; }

    public List<SelectListItem> AvailableUsers { get; set; } = new();
}