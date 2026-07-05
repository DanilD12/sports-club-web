namespace Stalika.Web.ViewModels.Booking;

public class BookingEquipmentItemViewModel
{
    public string EquipmentName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }
    public decimal PricePerHour { get; set; }
    public int SelectedQuantity { get; set; }
}