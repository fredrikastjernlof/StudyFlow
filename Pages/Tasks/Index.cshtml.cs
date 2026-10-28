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
    private readonly TaskPriorityService _taskPriorityService;

    public IndexModel(
     TaskService taskService,
     CourseService courseService,
     ChecklistService checklistService,
     TaskPriorityService taskPriorityService)
    {
        _taskService = taskService;
        _courseService = courseService;
        _checklistService = checklistService;
        _taskPriorityService = taskPriorityService;
    }

    public List<StudyTask> Tasks { get; set; } = new();
    public List<Course> Courses { get; set; } = new();
    public TaskPriorityResult? TodaysFocus { get; set; }

    [BindProperty]
    public StudyTaskInputModel TaskInput { get; set; } = new();

    [BindProperty]
    public int? CourseId { get; set; }

    public bool OpenCreateDialog { get; set; }

    // Load tasks and courses belonging to the signed-in user.
    private async Task LoadPageDataAsync(string userId)
    {
        Tasks = await _taskService.GetUserTasksAsync(userId);
        TodaysFocus = _taskPriorityService.GetTodaysFocus(Tasks);
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

        // Preserve binding errors before applying custom validation.
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
                "TaskInput.Title", "Task title is required.");
        }
        else if (TaskInput.Title.Trim().Length > 100)
        {
            ModelState.AddModelError(
                "TaskInput.Title", "Task title cannot exceed 100 characters.");
        }

        if (TaskInput.Description?.Length > 1000)
        {
            ModelState.AddModelError(
                "TaskInput.Description", "Description cannot exceed 1000 characters.");
        }

        if (invalidPriority || TaskInput.Priority < 1 || TaskInput.Priority > 3)
        {
            ModelState.AddModelError(
                "TaskInput.Priority", "Please select a valid priority.");
        }

        if (invalidHours ||
            !double.IsFinite(TaskInput.EstimatedHours) ||
            TaskInput.EstimatedHours < 0 ||
            TaskInput.EstimatedHours > 1000)
        {
            ModelState.AddModelError(
                "TaskInput.EstimatedHours", "Please enter a valid time estimate.");
        }

        if (invalidDate)
        {
            ModelState.AddModelError(
                "TaskInput.Deadline", "Please enter a valid date.");
        }

        if (checklistItems.Any(item =>
    string.IsNullOrWhiteSpace(item) || item.Trim().Length > 100))
        {
            ModelState.AddModelError(
                "checklistItems", "Checklist items must contain 1–100 characters.");
        }

        if (invalidCourse)
        {
            ModelState.AddModelError(
                "CourseId", "Please select a valid course.");
        }

        await LoadPageDataAsync(userId);

        // Verify that a selected course belongs to the user.
        if (CourseId.HasValue && !Courses.Any(c => c.Id == CourseId.Value))
        {
            ModelState.AddModelError(
                "CourseId", "The selected course does not exist");
        }

        if (!ModelState.IsValid)
        {
            ViewData["PostedChecklistItems"] = checklistItems;

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
            ModelState.AddModelError("title", "Task title is required.");
        else if (title.Trim().Length > 100)
            ModelState.AddModelError("title", "Task title cannot exceed 100 characters.");

        if ((description?.Length ?? 0) > 1000)
            ModelState.AddModelError("description", "Description cannot exceed 1000 characters.");

        if (invalidPriority || priority < 1 || priority > 3)
            ModelState.AddModelError("priority", "Please select a valid priority.");

        if (invalidHours || !double.IsFinite(estimatedHours) ||
            estimatedHours < 0 || estimatedHours > 1000)
            ModelState.AddModelError("estimatedHours", "Estimated hours must be between 0 and 1000.");

        if (invalidDate)
            ModelState.AddModelError("deadline", "Please enter a valid date.");
        else if (deadline.HasValue && deadline.Value.Date < DateTime.Today)
            ModelState.AddModelError("deadline", "Deadline cannot be in the past.");

        if (itemIds.Count != itemTitles.Count ||
            itemIds.Count != itemIds.Distinct().Count() ||
            itemIds.Any(id => task.ChecklistItems.All(i => i.Id != id)))
            return BadRequest("The task could not be updated.");

        if (itemTitles.Any(t => string.IsNullOrWhiteSpace(t) || t.Trim().Length > 100) ||
            newItemTitles.Any(t => string.IsNullOrWhiteSpace(t) || t.Trim().Length > 100))
            ModelState.AddModelError("checklist", "Checklist items must contain 1–100 characters.");

        if (!ModelState.IsValid)
            return BadRequest(string.Join(" ",
                ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)));

        var updated = await _taskService.UpdateTaskAsync(
            taskId, task.CourseId, userId,
            title, description, deadline, priority, estimatedHours,
            itemIds, itemTitles, newItemTitles);

        if (!updated) return BadRequest("The task could not be updated.");

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
