using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StudyFlow.Data;
using StudyFlow.Models;
using StudyFlow.ViewModels;
using StudyFlow.Services;


namespace StudyFlow.Pages.Courses;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;
    private readonly CourseService _courseService;
    private readonly TaskService _taskService;
    private readonly ChecklistService _checklistService;

    public DetailsModel(
        ApplicationDbContext context,
        CourseService courseService,
        TaskService taskService,
        ChecklistService checklistService)
    {
        _context = context;
        _courseService = courseService;
        _taskService = taskService;
        _checklistService = checklistService;
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

        // Find the course through the course service.
        var course = await _courseService.GetCourseByIdAsync(id, userId!);

        if (course == null)
        {
            return NotFound();
        }

        Course = course;

        // Load course tasks through the task service.
        Tasks = await _taskService.GetCourseTasksAsync(id, userId!);

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

        // Validate only the course form.
        ModelState.Clear();
        if (string.IsNullOrWhiteSpace(CourseName))
            ModelState.AddModelError(nameof(CourseName), "Du måste ange ett kursnamn.");
        else if (CourseName.Trim().Length > 100)
            ModelState.AddModelError(nameof(CourseName), "Kursnamnet får innehålla högst 100 tecken.");

        if (string.IsNullOrWhiteSpace(CourseColor) ||
            !System.Text.RegularExpressions.Regex.IsMatch(CourseColor, "^#[0-9A-Fa-f]{6}$"))
            ModelState.AddModelError(nameof(CourseColor), "Ogiltig färgkod.");

        if (!ModelState.IsValid)
        {
            Tasks = await _taskService.GetCourseTasksAsync(id, userId!);
            return Page();
        }

        // Update the course through the course service.
        var updated = await _courseService.UpdateCourseAsync(
            id, userId!, CourseName, CourseColor);

        if (!updated)
        {
            return NotFound();
        }

        return RedirectToPage("./Details", new { id = course.Id });
    }

    // Delete a course through the course service.
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Unauthorized();
        }

        var deleted = await _courseService.DeleteCourseAsync(id, userId);

        if (!deleted)
        {
            return NotFound();
        }

        // Return to the course overview after deletion.
        return RedirectToPage("./Index");
    }

    public async Task<IActionResult> OnPostCreateTaskAsync(int id, List<string> checklistItems)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (course == null)
        {
            return NotFound();
        }

        // Replace automatic binding messages with consistent Swedish validation.
        bool invalidPriority = ModelState.TryGetValue("TaskInput.Priority", out var p) && p.Errors.Count > 0;
        bool invalidHours = ModelState.TryGetValue("TaskInput.EstimatedHours", out var e) && e.Errors.Count > 0;
        bool invalidDate = ModelState.TryGetValue("TaskInput.Deadline", out var d) && d.Errors.Count > 0;
        ModelState.Clear();
        ValidateTask(TaskInput.Title, TaskInput.Description, TaskInput.Deadline,
            TaskInput.Priority, TaskInput.EstimatedHours, "TaskInput.", invalidPriority, invalidHours, invalidDate);
        if (checklistItems.Any(x => !string.IsNullOrWhiteSpace(x) && x.Trim().Length > 100))
            ModelState.AddModelError("checklistItems", "Delmoment får innehålla högst 100 tecken.");

        if (!ModelState.IsValid)
        {
            Course = course;
            CourseName = course.Name;
            CourseColor = course.Color;
            ViewData["PostedChecklistItems"] = checklistItems.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            Tasks = await _taskService.GetCourseTasksAsync(id, userId!);

            return Page();
        }

        // Create the task and its checklist items through the task service.
        await _taskService.CreateTaskAsync(
            course.Id,
            userId!,
            TaskInput,
            checklistItems);

        return RedirectToPage("./Details", new { id });
    }

    // Update the status of a study task.
    public async Task<IActionResult> OnPostUpdateTaskStatusAsync(
        int id, int taskId, string status)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (status != "NotStarted" &&
            status != "InProgress" &&
            status != "Completed")
        {
            return BadRequest();
        }

        var updated = await _taskService.UpdateTaskStatusAsync(
            taskId,
            id,
            userId!,
            status);

        if (!updated)
            return NotFound();

        return RedirectToPage("./Details", new { id });
    }

    // Update a checklist item's completion status.
    public async Task<IActionResult> OnPostUpdateChecklistItemAsync(
     int id, int itemId, bool isCompleted)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var updated = await _checklistService.UpdateChecklistItemAsync(
            itemId,
            id,
            userId!,
            isCompleted);

        if (!updated)
            return NotFound();

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

        var task = await _taskService.GetTaskByIdAsync(
            taskId,
            id,
            userId!);

        if (task == null)
        {
            return NotFound();
        }

        bool invalidPriority = ModelState.TryGetValue("priority", out var p) && p.Errors.Count > 0;
        bool invalidHours = ModelState.TryGetValue("estimatedHours", out var e) && e.Errors.Count > 0;
        bool invalidDate = ModelState.TryGetValue("deadline", out var d) && d.Errors.Count > 0;
        ModelState.Clear();
        ValidateTask(title, description, deadline, priority, estimatedHours, "",
            invalidPriority, invalidHours, invalidDate);

        if (itemIds.Count != itemTitles.Count ||
            itemIds.Count != itemIds.Distinct().Count() ||
            itemIds.Any(itemId => task.ChecklistItems.All(i => i.Id != itemId)))
            return BadRequest("Ogiltiga checklistuppgifter.");

        if (itemTitles.Any(t => string.IsNullOrWhiteSpace(t) || t.Trim().Length > 100) ||
            newItemTitles.Any(t => string.IsNullOrWhiteSpace(t) || t.Trim().Length > 100))
            ModelState.AddModelError("checklist", "Delmoment måste innehålla 1–100 tecken.");

        if (!ModelState.IsValid)
        {
            // Invalid requests are rejected; client-side validation displays the errors in the form.
            return BadRequest(string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(x => x.ErrorMessage)));
        }

        // Save task changes through the task service.
        await _taskService.UpdateTaskAsync(
            task,
            title,
            description,
            deadline,
            priority,
            estimatedHours,
            itemIds,
            itemTitles,
            newItemTitles);

        return RedirectToPage("./Details", new { id });
    }

    // Add a checklist item to an existing study task.
    public async Task<IActionResult> OnPostAddChecklistItemAsync(
    int id, int taskId, string title)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 100)
            return BadRequest();

        var added = await _checklistService.AddChecklistItemAsync(
            taskId,
            id,
            userId!,
            title);

        if (!added)
            return NotFound();

        return RedirectToPage("./Details", new { id });
    }

    // Delete a checklist item from an existing study task.
    public async Task<IActionResult> OnPostDeleteChecklistItemAsync(
    int id, int itemId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var deleted = await _checklistService.DeleteChecklistItemAsync(
            itemId,
            id,
            userId!);

        if (!deleted)
            return NotFound();

        return RedirectToPage("./Details", new { id });
    }

    // Shared server-side validation for creating and editing tasks.
    private void ValidateTask(string? title, string? description, DateTime? deadline,
        int priority, double hours, string prefix,
        bool invalidPriority, bool invalidHours, bool invalidDate)
    {
        if (string.IsNullOrWhiteSpace(title))
            ModelState.AddModelError(prefix + (prefix == "" ? "title" : "Title"), "Du måste ange ett namn på uppgiften.");
        else if (title.Trim().Length > 100)
            ModelState.AddModelError(prefix + (prefix == "" ? "title" : "Title"), "Uppgiftens namn får innehålla högst 100 tecken.");

        if ((description?.Length ?? 0) > 1000)
            ModelState.AddModelError(prefix + (prefix == "" ? "description" : "Description"), "Beskrivningen får innehålla högst 1000 tecken.");

        if (invalidPriority || priority < 1 || priority > 3)
            ModelState.AddModelError(prefix + (prefix == "" ? "priority" : "Priority"), "Välj en giltig prioritet.");

        if (invalidHours || !double.IsFinite(hours) || hours < 0 || hours > 1000)
            ModelState.AddModelError(prefix + (prefix == "" ? "estimatedHours" : "EstimatedHours"), "Arbetsinsatsen måste vara mellan 0 och 1000 timmar.");

        if (invalidDate)
            ModelState.AddModelError(prefix + (prefix == "" ? "deadline" : "Deadline"), "Ange ett giltigt datum.");
        else if (deadline.HasValue && deadline.Value.Date < DateTime.Today)
            ModelState.AddModelError(prefix + (prefix == "" ? "deadline" : "Deadline"), "Deadline får inte ligga bakåt i tiden.");
    }

    // Delete a task only if it belongs to the signed-in user and selected course.
    public async Task<IActionResult> OnPostDeleteTaskAsync(int id, int taskId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        var deleted = await _taskService.DeleteTaskAsync(
            taskId,
            id,
            userId!);

        if (!deleted)
            return NotFound();

        return RedirectToPage("./Details", new { id });
    }

}