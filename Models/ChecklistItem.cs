namespace StudyFlow.Models;

public class ChecklistItem
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;

    // Every cheklist item belongs to a task.
    public int StudyTaskId { get; set; }
    public StudyTask StudyTask { get; set; } = null!;
    
}