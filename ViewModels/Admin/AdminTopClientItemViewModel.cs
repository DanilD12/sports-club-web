namespace Stalika.Web.ViewModels.Admin;

public class AdminTopClientItemViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public int BookingCount { get; set; }
    public decimal TotalSpent { get; set; }
}