namespace Stalika.Web.ViewModels.Admin;

public class AdminBookingsPageViewModel
{
    public string? Search { get; set; }
    public string? TrainerName { get; set; }
    public int? TableNumber { get; set; }
    public DateTime? Date { get; set; }

    public List<string> Trainers { get; set; } = new();
    public List<int> Tables { get; set; } = new();

    public List<AdminBookingItemViewModel> Items { get; set; } = new();
}