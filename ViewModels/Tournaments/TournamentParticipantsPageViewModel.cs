namespace Stalika.Web.ViewModels.Tournaments;

public class TournamentParticipantsPageViewModel
{
    public int TournamentId { get; set; }
    public string TournamentName { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    public List<TournamentParticipantItemViewModel> Participants { get; set; } = new();
}