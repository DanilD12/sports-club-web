namespace Stalika.Web.Entities;

public class Gym
{
    public int GymNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TableCount { get; set; }
    public TimeSpan OpeningTime { get; set; }
    public TimeSpan ClosingTime { get; set; }

    public ICollection<Table> Tables { get; set; } = new List<Table>();
}