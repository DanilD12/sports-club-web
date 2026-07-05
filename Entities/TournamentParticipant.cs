namespace Stalika.Web.Entities;

public class TournamentParticipant
{
    public int UserId { get; set; }
    public int TournamentId { get; set; }
    public int? Place { get; set; }

    public User? User { get; set; }
    public Tournament? Tournament { get; set; }
}