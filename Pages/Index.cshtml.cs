using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StudyFlow.Models;
using StudyFlow.Services;
using StudyFlow.ViewModels;

namespace StudyFlow.Pages;

public class IndexModel : PageModel
{
    private readonly TaskService _taskService;
    private readonly TaskPriorityService _taskPriorityService;
    private readonly CourseService _courseService;
    private readonly ChecklistService _checklistService;

    public List<StudyTask> Tasks { get; set; } = new();
    public TaskPriorityResult? TodaysFocus { get; set; }

    public List<StudyTask> ComingUp { get; set; } = new();
    public List<Course> Courses { get; set; } = new();

    public int CompletedTaskCount { get; set; }

    public IndexModel(
    TaskService taskService,
    TaskPriorityService taskPriorityService,
    CourseService courseService,
    ChecklistService checklistService)
    {
        _taskService = taskService;
        _taskPriorityService = taskPriorityService;
        _courseService = courseService;
        _checklistService = checklistService;
    }

    public async Task OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId == null)
        {
            return;
        }

        Tasks = await _taskService.GetUserTasksAsync(userId);

        // Select the highest-priority unfinished task.
        TodaysFocus = _taskPriorityService.GetTodaysFocus(Tasks);

        // Count completed tasks.
        CompletedTaskCount = Tasks.Count(task => task.Status == "Completed");

        // Show four upcoming tasks without repeating today's focus.
        ComingUp = Tasks
            .Where(task => task.Status != "Completed"
                && task.Id != TodaysFocus?.Task.Id)
            .OrderBy(task => task.Deadline ?? DateTime.MaxValue)
            .Take(4)
            .ToList();

        // Load the user's courses.
        Courses = await _courseService.GetUserCoursesAsync(userId);
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
            return BadRequest("Invalid checklist items.");

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

        if (!updated) return BadRequest("The task could not be updated");

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
