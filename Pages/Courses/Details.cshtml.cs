using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StudyFlow.Data;
using StudyFlow.Models;
using StudyFlow.ViewModels;


namespace StudyFlow.Pages.Courses;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Course Course { get; set; } = null!;

    // Fields used when editing a course.
    [BindProperty]
    [Required(ErrorMessage = "Du måste ange ett kursnamn.")]
    [StringLength(100, ErrorMessage = "Kursnamnet får innehålla högst 100 tecken.")]
    public string CourseName { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Ogiltig färgkod.")]
    public string CourseColor { get; set; } = "#B8D8C0";

    // Form data for creating a new study task.
    [BindProperty]
    public StudyTaskInputModel TaskInput { get; set; } = new();

    public List<StudyTask> Tasks { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Get the currently logged-in user's ID.
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Find the course and verify that it belongs to the user.
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (course == null)
        {
            return NotFound();
        }

        Course = course;

        // Load course tasks together with their checklist items.
        Tasks = await _context.StudyTasks
            .Include(t => t.ChecklistItems)
            .Where(t => t.CourseId == id && t.UserId == userId)
            .OrderBy(t => t.Deadline)
            .ToListAsync();

        // Populate the edit form with the course's current values.
        CourseName = course.Name;
        CourseColor = course.Color;

        return Page();
    }

    // Save changes made in the edit course dialog.
    public async Task<IActionResult> OnPostEditAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Find the course and verify ownership.
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (course == null)
        {
            return NotFound();
        }

        Course = course;

        // Reject empty or whitespace-only course names.
        if (string.IsNullOrWhiteSpace(CourseName))
        {
            ModelState.AddModelError(
                nameof(CourseName),
                "Du måste ange ett kursnamn."
            );
        }

        // Ignore validation for the task creation form.
        ModelState.Remove("Title");
        ModelState.Remove("TaskInput.Title");
        ModelState.Remove("TaskInput.Description");
        ModelState.Remove("TaskInput.Deadline");
        ModelState.Remove("TaskInput.Priority");
        ModelState.Remove("TaskInput.EstimatedHours");
        ModelState.Remove("TaskInput");

        // Do not save invalid input.
        if (!ModelState.IsValid)
        {

            return Page();
        }

        // Update and save the course.
        course.Name = CourseName.Trim();
        course.Color = CourseColor;

        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { id = course.Id });
    }

    // Delete a course and its related tasks and checklist items.
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Find the course and verify that it belongs to the user.
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (course == null)
        {
            return NotFound();
        }

        // Related tasks and checklist items are deleted through cascade delete.
        _context.Courses.Remove(course);

        await _context.SaveChangesAsync();

        // Return to the course overview after deletion.
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostCreateTaskAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (course == null)
        {
            return NotFound();
        }

        // Validate only the task form.
        ModelState.Remove(nameof(CourseName));
        ModelState.Remove(nameof(CourseColor));

        if (string.IsNullOrWhiteSpace(TaskInput.Title))
        {
            ModelState.AddModelError(
                "TaskInput.Title",
                "Du måste ange ett namn på uppgiften.");
        }

        if (!ModelState.IsValid)
        {
            Course = course;
            return Page();
        }

        var task = new StudyTask
        {
            Title = TaskInput.Title.Trim(),
            Description = TaskInput.Description,
            Deadline = TaskInput.Deadline,
            Priority = TaskInput.Priority,
            EstimatedHours = TaskInput.EstimatedHours,
            Status = "NotStarted",
            CourseId = course.Id,
            UserId = userId!
        };

        _context.StudyTasks.Add(task);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { id });
    }

    // Update the status of a study task.
    public async Task<IActionResult> OnPostUpdateTaskStatusAsync(
        int id, int taskId, string status)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Find the task and verify that it belongs to the user and course.
        var task = await _context.StudyTasks
            .FirstOrDefaultAsync(t =>
                t.Id == taskId &&
                t.CourseId == id &&
                t.UserId == userId);

        if (task == null)
        {
            return NotFound();
        }

        // Only allow the three supported status values.
        if (status != "NotStarted" &&
            status != "InProgress" &&
            status != "Completed")
        {
            return BadRequest();
        }

        task.Status = status;

        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { id });
    }

    // Update a checklist item's completion status.
    public async Task<IActionResult> OnPostUpdateChecklistItemAsync(
        int id, int itemId, bool isCompleted)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Find the item and verify that its task belongs to the user and course.
        var item = await _context.ChecklistItems
            .Include(i => i.StudyTask)
            .FirstOrDefaultAsync(i =>
                i.Id == itemId &&
                i.StudyTask.CourseId == id &&
                i.StudyTask.UserId == userId);

        if (item == null)
        {
            return NotFound();
        }

        item.IsCompleted = isCompleted;

        await _context.SaveChangesAsync();

        return new JsonResult(new { success = true });
    }

    // Save task details and checklist items together.
    public async Task<IActionResult> OnPostEditTaskAsync(
        int id, int taskId,
        string title, string? description,
        DateTime? deadline, int priority, double estimatedHours,
        List<int> itemIds, List<string> itemTitles,
        List<string> newItemTitles)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var task = await _context.StudyTasks
            .Include(t => t.ChecklistItems)
            .FirstOrDefaultAsync(t =>
                t.Id == taskId &&
                t.CourseId == id &&
                t.UserId == userId);

        if (task == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(title) ||
            title.Trim().Length > 100 ||
            priority < 1 || priority > 3 ||
            estimatedHours < 0 || estimatedHours > 1000 ||
            itemIds.Count != itemTitles.Count)
        {
            return BadRequest();
        }

        if (itemTitles.Any(t => string.IsNullOrWhiteSpace(t) || t.Trim().Length > 100) ||
            newItemTitles.Any(t => string.IsNullOrWhiteSpace(t) || t.Trim().Length > 100))
        {
            return BadRequest();
        }

        if (itemIds.Count != itemIds.Distinct().Count() ||
            itemIds.Any(itemId => task.ChecklistItems.All(i => i.Id != itemId)))
        {
            return BadRequest();
        }

        task.Title = title.Trim();
        task.Description = description ?? string.Empty;
        task.Deadline = deadline;
        task.Priority = priority;
        task.EstimatedHours = estimatedHours;

        // Remove checklist items deleted in the editor.
        var removedItems = task.ChecklistItems
            .Where(i => !itemIds.Contains(i.Id))
            .ToList();

        _context.ChecklistItems.RemoveRange(removedItems);

        // Update existing checklist items.
        for (int i = 0; i < itemIds.Count; i++)
        {
            var item = task.ChecklistItems.First(x => x.Id == itemIds[i]);
            item.Title = itemTitles[i].Trim();
        }

        // Add new checklist items.
        foreach (var itemTitle in newItemTitles)
        {
            _context.ChecklistItems.Add(new ChecklistItem
            {
                Title = itemTitle.Trim(),
                StudyTaskId = task.Id
            });
        }

        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { id });
    }

    // Add a checklist item to an existing study task.
    public async Task<IActionResult> OnPostAddChecklistItemAsync(
        int id, int taskId, string title)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Verify that the task belongs to the current user and course.
        var task = await _context.StudyTasks
            .FirstOrDefaultAsync(t =>
                t.Id == taskId &&
                t.CourseId == id &&
                t.UserId == userId);

        if (task == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 100)
        {
            return BadRequest();
        }

        var item = new ChecklistItem
        {
            Title = title.Trim(),
            StudyTaskId = task.Id
        };

        _context.ChecklistItems.Add(item);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { id });
    }

    // Delete a checklist item from an existing study task.
    public async Task<IActionResult> OnPostDeleteChecklistItemAsync(
        int id, int itemId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Verify that the checklist item belongs to the current user and course.
        var item = await _context.ChecklistItems
            .Include(i => i.StudyTask)
            .FirstOrDefaultAsync(i =>
                i.Id == itemId &&
                i.StudyTask.CourseId == id &&
                i.StudyTask.UserId == userId);

        if (item == null)
        {
            return NotFound();
        }

        _context.ChecklistItems.Remove(item);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { id });
    }
}