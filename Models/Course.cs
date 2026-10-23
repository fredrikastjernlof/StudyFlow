using System.Security.Cryptography.X509Certificates;

namespace StudyFlow.Models;

public class Course
{
    public int Id { get; set;}
    public string Name { get; set;} = string.Empty;
    public string Color { get; set; } = "#B8FDB8";
    public string UserId { get; set; } = string.Empty;

    // A course can contain multiple tasks.
    public ICollection<StudyTask> Tasks { get; set; } = new List<StudyTask>();

}