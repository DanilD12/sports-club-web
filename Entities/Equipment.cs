namespace Stalika.Web.Entities;

public class Equipment
{
    public string EquipmentName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal PricePerHour { get; set; }

    public ICollection<EquipmentRental> Rentals { get; set; } = new List<EquipmentRental>();
}