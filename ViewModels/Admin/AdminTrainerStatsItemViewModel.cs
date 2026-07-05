namespace Stalika.Web.ViewModels.Admin;

public class AdminTrainerStatsItemViewModel
{
    public int TrainerId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal TotalRevenue { get; set; }
}