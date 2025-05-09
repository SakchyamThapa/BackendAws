using SonicPoints.Models;

public class ProjectUserPoints
{
    public int Id { get; set; }
    public string UserId { get; set; } = null!;
    public int ProjectId { get; set; }
    public int TotalPoints { get; set; }

    public User User { get; set; } = null!;
    public Project Project { get; set; } = null!;
}
