namespace Stalika.Web.Entities;

public class Tournament
{
    public int TournamentId { get; set; }
    public string TournamentName { get; set; } = string.Empty;
    public string Organizer { get; set; } = string.Empty;
    public int? ParticipantCount { get; set; }
    public DateTime Date { get; set; }
    public int MaxParticipants { get; set; }

    public ICollection<TournamentParticipant> Participants { get; set; } = new List<TournamentParticipant>();
}