using Microsoft.AspNetCore.Mvc.Rendering;

namespace Stalika.Web.ViewModels.Tournaments;

public class TournamentRegisterClientViewModel
{
    public int TournamentId { get; set; }
    public string TournamentName { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    public int? SelectedUserId { get; set; }
    public List<SelectListItem> AvailableClients { get; set; } = new();
}