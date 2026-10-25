using System.Security.Claims;
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

    public List<StudyTask> Tasks { get; set; } = new();
    public TaskPriorityResult? TodaysFocus { get; set; }

    public List<StudyTask> ComingUp { get; set; } = new();
    public List<Course> Courses { get; set; } = new();

    public int CompletedTaskCount { get; set; }

    public IndexModel(
    TaskService taskService,
    TaskPriorityService taskPriorityService,
    CourseService courseService)
    {
        _taskService = taskService;
        _taskPriorityService = taskPriorityService;
        _courseService = courseService;
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
}