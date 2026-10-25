
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudyFlow.Models;
using StudyFlow.Services;
using StudyFlow.ViewModels;

namespace StudyFlow.Pages.Tasks;

[Authorize]
public class IndexModel : PageModel
{
    private readonly TaskService _taskService;
    private readonly CourseService _courseService;
    private readonly ChecklistService _checklistService;

    public IndexModel(
     TaskService taskService,
     CourseService courseService,
     ChecklistService checklistService)
    {
        _taskService = taskService;
        _courseService = courseService;
        _checklistService = checklistService;
    }

    public List<StudyTask> Tasks { get; set; } = new();
    public List<Course> Courses { get; set; } = new();

    [BindProperty]
    public StudyTaskInputModel TaskInput { get; set; } = new();

    [BindProperty]
    public int? CourseId { get; set; }

    public bool OpenCreateDialog { get; set; }

    // Load tasks and courses belonging to the signed-in user.
    private async Task LoadPageDataAsync(string userId)
    {
        Tasks = await _taskService.GetUserTasksAsync(userId);
        Courses = await _courseService.GetUserCoursesAsync(userId);

        ViewData["ShowCourseSelect"] = true;
        ViewData["AvailableCourses"] = Courses;
        ViewData["SelectedCourseId"] = CourseId;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Challenge();
        }

        await LoadPageDataAsync(userId);
        return Page();
    }

    public async Task<IActionResult> OnPostCreateTaskAsync(List<string> checklistItems)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return Challenge();
        }

        checklistItems ??= new List<string>();

        // Replace automatic binding errors with Swedish validation messages.
        bool invalidPriority = ModelState.TryGetValue(
            "TaskInput.Priority", out var priorityEntry) &&
            priorityEntry.Errors.Count > 0;

        bool invalidHours = ModelState.TryGetValue(
            "TaskInput.EstimatedHours", out var hoursEntry) &&
            hoursEntry.Errors.Count > 0;

        bool invalidDate = ModelState.TryGetValue(
            "TaskInput.Deadline", out var dateEntry) &&
            dateEntry.Errors.Count > 0;

        bool invalidCourse = ModelState.TryGetValue(
            "CourseId", out var courseEntry) &&
            courseEntry.Errors.Count > 0;

        ModelState.Clear();

        if (string.IsNullOrWhiteSpace(TaskInput.Title))
        {
            ModelState.AddModelError(
                "TaskInput.Title", "Du måste ange ett namn på uppgiften.");
        }
        else if (TaskInput.Title.Trim().Length > 100)
        {
            ModelState.AddModelError(
                "TaskInput.Title", "Namnet får innehålla högst 100 tecken.");
        }

        if (TaskInput.Description?.Length > 1000)
        {
            ModelState.AddModelError(
                "TaskInput.Description", "Beskrivningen får innehålla högst 1000 tecken.");
        }

        if (invalidPriority || TaskInput.Priority < 1 || TaskInput.Priority > 3)
        {
            ModelState.AddModelError(
                "TaskInput.Priority", "Välj en giltig prioritet.");
        }

        if (invalidHours ||
            !double.IsFinite(TaskInput.EstimatedHours) ||
            TaskInput.EstimatedHours < 0 ||
            TaskInput.EstimatedHours > 1000)
        {
            ModelState.AddModelError(
                "TaskInput.EstimatedHours", "Ange en giltig tidsuppskattning.");
        }

        if (invalidDate)
        {
            ModelState.AddModelError(
                "TaskInput.Deadline", "Ange ett giltigt datum.");
        }

        if (checklistItems.Any(item =>
            !string.IsNullOrWhiteSpace(item) && item.Trim().Length > 100))
        {
            ModelState.AddModelError(
                "checklistItems", "Delmoment får innehålla högst 100 tecken.");
        }

        if (invalidCourse)
        {
            ModelState.AddModelError(
                "CourseId", "Välj en giltig kurs.");
        }

        await LoadPageDataAsync(userId);

        // Verify that a selected course belongs to the user.
        if (CourseId.HasValue && !Courses.Any(c => c.Id == CourseId.Value))
        {
            ModelState.AddModelError(
                "CourseId", "Den valda kursen finns inte.");
        }

        if (!ModelState.IsValid)
        {
            ViewData["PostedChecklistItems"] = checklistItems
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .ToList();

            OpenCreateDialog = true;
            return Page();
        }

        await _taskService.CreateTaskAsync(
            CourseId,
            userId,
            TaskInput,
            checklistItems);

        return RedirectToPage("./Index");
    }


    // Find a task belonging to the signed-in user.
    private async Task<StudyTask?> FindUserTaskAsync(int taskId, string userId)
    {
        var tasks = await _taskService.GetUserTasksAsync(userId);
        return tasks.FirstOrDefault(t => t.Id == taskId);
    }

    // Update the status of an existing task.
    public async Task<IActionResult> OnPostUpdateTaskStatusAsync(
        int taskId, string status)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Challenge();

        if (status != "NotStarted" &&
            status != "InProgress" &&
            status != "Completed")
            return BadRequest();

        var task = await FindUserTaskAsync(taskId, userId);
        if (task == null) return NotFound();

        var updated = await _taskService.UpdateTaskStatusAsync(
            taskId, task.CourseId, userId, status);

        if (!updated) return NotFound();

        return RedirectToPage("./Index");
    }

    // Save checklist completion changes.
    public async Task<IActionResult> OnPostUpdateChecklistItemAsync(
        int itemId, bool isCompleted)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Challenge();

        var tasks = await _taskService.GetUserTasksAsync(userId);
        var task = tasks.FirstOrDefault(t =>
            t.ChecklistItems.Any(i => i.Id == itemId));

        if (task == null) return NotFound();

        var updated = await _checklistService.UpdateChecklistItemAsync(
            itemId, task.CourseId, userId, isCompleted);

        if (!updated) return NotFound();

        return new JsonResult(new { success = true });
    }

    // Save task details and checklist items.
    public async Task<IActionResult> OnPostEditTaskAsync(
        int taskId,
        string title,
        string? description,
        DateTime? deadline,
        int priority,
        double estimatedHours,
        List<int> itemIds,
        List<string> itemTitles,
        List<string> newItemTitles)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Challenge();

        var task = await FindUserTaskAsync(taskId, userId);
        if (task == null) return NotFound();

        itemIds ??= new();
        itemTitles ??= new();
        newItemTitles ??= new();

        bool invalidPriority = ModelState.TryGetValue(
            "priority", out var p) && p.Errors.Count > 0;
        bool invalidHours = ModelState.TryGetValue(
            "estimatedHours", out var h) && h.Errors.Count > 0;
        bool invalidDate = ModelState.TryGetValue(
            "deadline", out var d) && d.Errors.Count > 0;

        ModelState.Clear();

        if (string.IsNullOrWhiteSpace(title))
            ModelState.AddModelError("title", "Du måste ange ett namn på uppgiften.");
        else if (title.Trim().Length > 100)
            ModelState.AddModelError("title", "Uppgiftens namn får innehålla högst 100 tecken.");

        if ((description?.Length ?? 0) > 1000)
            ModelState.AddModelError("description", "Beskrivningen får innehålla högst 1000 tecken.");

        if (invalidPriority || priority < 1 || priority > 3)
            ModelState.AddModelError("priority", "Välj en giltig prioritet.");

        if (invalidHours || !double.IsFinite(estimatedHours) ||
            estimatedHours < 0 || estimatedHours > 1000)
            ModelState.AddModelError("estimatedHours", "Arbetsinsatsen måste vara mellan 0 och 1000 timmar.");

        if (invalidDate)
            ModelState.AddModelError("deadline", "Ange ett giltigt datum.");
        else if (deadline.HasValue && deadline.Value.Date < DateTime.Today)
            ModelState.AddModelError("deadline", "Deadline får inte ligga bakåt i tiden.");

        if (itemIds.Count != itemTitles.Count ||
            itemIds.Count != itemIds.Distinct().Count() ||
            itemIds.Any(id => task.ChecklistItems.All(i => i.Id != id)))
            return BadRequest("Ogiltiga checklistuppgifter.");

        if (itemTitles.Any(t => string.IsNullOrWhiteSpace(t) || t.Trim().Length > 100) ||
            newItemTitles.Any(t => string.IsNullOrWhiteSpace(t) || t.Trim().Length > 100))
            ModelState.AddModelError("checklist", "Delmoment måste innehålla 1–100 tecken.");

        if (!ModelState.IsValid)
            return BadRequest(string.Join(" ",
                ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)));

        var updated = await _taskService.UpdateTaskAsync(
            taskId, task.CourseId, userId,
            title, description, deadline, priority, estimatedHours,
            itemIds, itemTitles, newItemTitles);

        if (!updated) return BadRequest("Uppgiften kunde inte uppdateras.");

        return RedirectToPage("./Index");
    }

    // Delete a task belonging to the signed-in user.
    public async Task<IActionResult> OnPostDeleteTaskAsync(int taskId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Challenge();

        var task = await FindUserTaskAsync(taskId, userId);
        if (task == null) return NotFound();

        var deleted = await _taskService.DeleteTaskAsync(
            taskId, task.CourseId, userId);

        if (!deleted) return NotFound();

        return RedirectToPage("./Index");
    }
}
