using Microsoft.EntityFrameworkCore;
using StudyFlow.Data;
using StudyFlow.Models;
using StudyFlow.ViewModels;

namespace StudyFlow.Services;

public class TaskService
{
    private readonly ApplicationDbContext _context;

    // Inject the database context.
    public TaskService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Get all tasks belonging to the user, including standalone tasks.
    public async Task<List<StudyTask>> GetUserTasksAsync(string userId)
    {
        return await _context.StudyTasks
            .AsNoTracking()
            .Include(t => t.Course)
            .Include(t => t.ChecklistItems)
            .Where(t => t.UserId == userId)
            .OrderBy(t => t.Deadline == null)
            .ThenBy(t => t.Deadline)
            .ToListAsync();
    }

    // Get tasks belonging to a specific course and user.
    public async Task<List<StudyTask>> GetCourseTasksAsync(int courseId, string userId)
    {
        return await _context.StudyTasks
            .Include(t => t.Course)
            .Include(t => t.ChecklistItems)
            .Where(t => t.CourseId == courseId && t.UserId == userId)
            .OrderBy(t => t.Deadline)
            .ToListAsync();
    }

    // Create a task with its checklist items.
    public async Task CreateTaskAsync(
        int? courseId,
        string userId,
        StudyTaskInputModel input,
        List<string> checklistItems)
    {

        // Verify course ownership only when a course is selected.
        if (courseId.HasValue)
        {
            var courseExists = await _context.Courses
                .AnyAsync(c => c.Id == courseId.Value && c.UserId == userId);

            if (!courseExists)
            {
                throw new UnauthorizedAccessException(
                    "The course does not exist or does not belong to the user.");
            }
        }

        var task = new StudyTask
        {
            Title = input.Title.Trim(),
            Description = input.Description ?? string.Empty,
            Deadline = input.Deadline,
            Priority = input.Priority,
            EstimatedHours = input.EstimatedHours,
            Status = "NotStarted",
            CourseId = courseId,
            UserId = userId
        };

        foreach (var title in checklistItems)
        {
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }

            task.ChecklistItems.Add(new ChecklistItem
            {
                Title = title.Trim()
            });
        }

        _context.StudyTasks.Add(task);
        await _context.SaveChangesAsync();
    }

    // Update the status of a task belonging to the user.
    public async Task<bool> UpdateTaskStatusAsync(
        int taskId,
        int? courseId,
        string userId,
        string status)
    {
        if (status != "NotStarted" &&
            status != "InProgress" &&
            status != "Completed")
        {
            return false;
        }

        var task = await _context.StudyTasks.FirstOrDefaultAsync(t =>
            t.Id == taskId &&
            t.UserId == userId &&
            t.CourseId == courseId);

        if (task == null)
        {
            return false;
        }

        task.Status = status;
        await _context.SaveChangesAsync();

        return true;
    }

    // Delete a task belonging to the user.
    public async Task<bool> DeleteTaskAsync(
        int taskId,
        int? courseId,
        string userId)
    {
        var task = await _context.StudyTasks.FirstOrDefaultAsync(t =>
            t.Id == taskId &&
            t.UserId == userId &&
            t.CourseId == courseId);

        if (task == null)
        {
            return false;
        }

        _context.StudyTasks.Remove(task);
        await _context.SaveChangesAsync();

        return true;
    }

    // Get a task and its checklist items for the specified user.
    public async Task<StudyTask?> GetTaskByIdAsync(
        int taskId,
        int? courseId,
        string userId)
    {
        return await _context.StudyTasks
            .Include(t => t.Course)
            .Include(t => t.ChecklistItems)
            .FirstOrDefaultAsync(t =>
                t.Id == taskId &&
                t.UserId == userId &&
                t.CourseId == courseId);
    }

    // Update task details and synchronize checklist items.
    public async Task<bool> UpdateTaskAsync(
        int taskId,
        int? courseId,
        string userId,
        string title,
        string? description,
        DateTime? deadline,
        int priority,
        double estimatedHours,
        List<int> itemIds,
        List<string> itemTitles,
        List<string> newItemTitles)
    {

        // Load the task and verify ownership.
        var task = await GetTaskByIdAsync(taskId, courseId, userId);

        if (task == null)
        {
            return false;
        }

        // Verify that all submitted checklist items belong to this task.
        if (itemIds.Count != itemTitles.Count ||
            itemIds.Count != itemIds.Distinct().Count() ||
            itemIds.Any(itemId => task.ChecklistItems.All(i => i.Id != itemId)))
        {
            return false;
        }
        task.Title = title.Trim();
        task.Description = description ?? string.Empty;
        task.Deadline = deadline;
        task.Priority = priority;
        task.EstimatedHours = estimatedHours;

        // Remove deleted checklist items.
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

        return true;
    }
}