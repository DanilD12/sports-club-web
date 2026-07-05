namespace Stalika.Web.Entities;

public class EquipmentRental
{
    public int RentalNumber { get; set; }
    public string EquipmentName { get; set; } = string.Empty;
    public int BookingNumber { get; set; }
    public int Quantity { get; set; }
    public decimal Amount { get; set; }

    public Equipment? Equipment { get; set; }
    public Booking? Booking { get; set; }
}