using System.ComponentModel.DataAnnotations;

namespace StudyFlow.ViewModels;

// Input fields used when creating or editing a study task.
public class StudyTaskInputModel
{
    [Required(ErrorMessage = "Task title is required.")]
    [StringLength(100, ErrorMessage = "Task title cannot exceed 100 characters.")]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime? Deadline { get; set; }

    [Range(1, 3, ErrorMessage = "Please select a valid priority.")]
    public int Priority { get; set; } = 2;

    [Range(0, 1000, ErrorMessage = "Please enter a valid time estimate.")]
    public double EstimatedHours { get; set; }
}