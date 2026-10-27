namespace StudyFlow.Models;

public class Course
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#B8FDB8";
    public string UserId { get; set; } = string.Empty;

    // A course can contain multiple tasks.
    public ICollection<StudyTask> Tasks { get; set; } = new List<StudyTask>();

    // Available pastel colors for courses.
    public static IReadOnlyList<string> AvailableColors { get; } = new[]
    {
        "#D6CBFC",
        "#FFEB92",
        "#B6E9FF",
        "#B8FDB8",
        "#FFD3D3",
        "#E9D3FF",
        "#FFC8E7",
        "#FFDDB8",
        "#FFE5C7",
        "#F5F0AE",
        "#D6F2BA",
        "#BCEFE0",
        "#BDE4F5",
        "#D2DBFF",
        "#E6D6E9",
        "#F5D6CB"
    };
}