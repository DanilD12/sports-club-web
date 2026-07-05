namespace Stalika.Web.ViewModels.Tournaments;

public class TournamentItemViewModel
{
    public int TournamentId { get; set; }
    public string TournamentName { get; set; } = string.Empty;
    public string Organizer { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    public int CurrentParticipants { get; set; }
    public int MaxParticipants { get; set; }
    public int FreePlaces { get; set; }

    public bool IsUserRegistered { get; set; }
    public bool CanRegister { get; set; }
}