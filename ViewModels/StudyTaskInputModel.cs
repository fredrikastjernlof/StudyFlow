using System.ComponentModel.DataAnnotations;

namespace StudyFlow.ViewModels;

// Input fields used when creating or editing a study task.
public class StudyTaskInputModel
{
    [Required(ErrorMessage = "Du måste ange ett namn på uppgiften.")]
    [StringLength(100, ErrorMessage = "Namnet får innehålla högst 100 tecken.")]
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime? Deadline { get; set; }

    [Range(1, 3, ErrorMessage = "Välj en giltig prioritet.")]
    public int Priority { get; set; } = 2;

    [Range(0, 1000, ErrorMessage = "Ange en giltig tidsuppskattning.")]
    public double EstimatedHours { get; set; }
}