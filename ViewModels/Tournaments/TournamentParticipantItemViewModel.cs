namespace Stalika.Web.ViewModels.Tournaments;

public class TournamentParticipantItemViewModel
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
    public int? Place { get; set; }
}