using System.ComponentModel.DataAnnotations;

namespace Stalika.Web.ViewModels.Admin;

public class AdminEquipmentEditViewModel
{
    [Required(ErrorMessage = "Введите название")]
    public string EquipmentName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите тип")]
    public string Type { get; set; } = string.Empty;

    [Range(0, 100000, ErrorMessage = "Количество не может быть отрицательным")]
    public int Quantity { get; set; }

    [Range(typeof(decimal), "0", "1000000", ErrorMessage = "Цена не может быть отрицательной")]
    public decimal PricePerHour { get; set; }
}