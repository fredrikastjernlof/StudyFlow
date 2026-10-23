namespace StudyFlow.Models;

public class StudyTask
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? Deadline { get; set; }
    public int Priority { get; set; }
    public double EstimatedHours { get; set; }
    public string Status { get; set; } = "NotStarted";

    // A task can optionally belong to a course.
    public int? CourseId { get; set; }
    public Course? Course { get; set; }

    // Every task belongs to a user.
    public string UserId { get; set; } = string.Empty;

    // A task can contain multiple checklist items.
    public ICollection<ChecklistItem> ChecklistItems { get; set; } = new List<ChecklistItem>();
    
}