namespace Stalika.Web.Entities;

public class BookingInfoRow
{
    public int BookingNumber { get; set; }
    public DateTime? BookingDate { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int TableNumber { get; set; }

    public string ClientName { get; set; } = string.Empty;
    public string TrainerName { get; set; } = string.Empty;

    public decimal TotalPrice { get; set; }

    public string? EquipmentName { get; set; }
    public int? EquipmentQuantity { get; set; }
    public decimal? EquipmentAmount { get; set; }
    public ICollection<EquipmentRental> EquipmentRentals { get; set; } = new List<EquipmentRental>();
}