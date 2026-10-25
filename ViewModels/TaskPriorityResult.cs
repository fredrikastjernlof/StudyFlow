using StudyFlow.Models;

namespace StudyFlow.ViewModels;

public class TaskPriorityResult
{
    public StudyTask Task { get; set; } = null!;
    public int Score { get; set; }
}