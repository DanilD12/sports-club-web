using System.ComponentModel.DataAnnotations;

namespace Stalika.Web.ViewModels.Admin;

public class AdminTournamentEditViewModel
{
    public int? TournamentId { get; set; }

    [Required(ErrorMessage = "Введите название турнира")]
    public string TournamentName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите организатора")]
    public string Organizer { get; set; } = string.Empty;

    [Required(ErrorMessage = "Введите дату")]
    public DateTime Date { get; set; }

    [Range(1, 10000, ErrorMessage = "Максимум участников должен быть больше 0")]
    public int MaxParticipants { get; set; }
}