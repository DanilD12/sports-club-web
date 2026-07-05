namespace Stalika.Web.ViewModels.Admin;

public class AdminAnalyticsViewModel
{
    public int TotalUsers { get; set; }
    public int TotalBookings { get; set; }
    public int TotalTournaments { get; set; }
    public int TotalTrainers { get; set; }
    public decimal TotalRevenue { get; set; }

    public List<AdminTopTableItemViewModel> TopTables { get; set; } = new();
    public List<AdminLowInventoryItemViewModel> LowInventoryItems { get; set; } = new();
    public List<AdminTopClientItemViewModel> TopClients { get; set; } = new();

    public List<AdminChartPointViewModel> BookingsByDay { get; set; } = new();
    public List<AdminChartPointViewModel> RevenueByDay { get; set; } = new();
    public List<AdminPopularEquipmentItemViewModel> PopularEquipment { get; set; } = new();
    public List<AdminTrainerStatsItemViewModel> TrainerStats { get; set; } = new();
}