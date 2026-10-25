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

    // Get tasks belonging to a specific course and user.
    public async Task<List<StudyTask>> GetCourseTasksAsync(int courseId, string userId)
    {
        return await _context.StudyTasks
            .Include(t => t.ChecklistItems)
            .Where(t => t.CourseId == courseId && t.UserId == userId)
            .OrderBy(t => t.Deadline)
            .ToListAsync();
    }

    // Create a task with its checklist items.
    public async Task CreateTaskAsync(
        int courseId,
        string userId,
        StudyTaskInputModel input,
        List<string> checklistItems)
    {
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

    // Update the status of a task belonging to the user and course.
    public async Task<bool> UpdateTaskStatusAsync(
        int taskId,
        int courseId,
        string userId,
        string status)
    {
        var task = await _context.StudyTasks.FirstOrDefaultAsync(t =>
            t.Id == taskId &&
            t.CourseId == courseId &&
            t.UserId == userId);

        if (task == null)
            return false;

        if (status != "NotStarted" &&
            status != "InProgress" &&
            status != "Completed")
            return false;

        task.Status = status;
        await _context.SaveChangesAsync();

        return true;
    }

    // Delete a task belonging to the specified user and course.
    public async Task<bool> DeleteTaskAsync(
        int taskId,
        int courseId,
        string userId)
    {
        var task = await _context.StudyTasks.FirstOrDefaultAsync(t =>
            t.Id == taskId &&
            t.CourseId == courseId &&
            t.UserId == userId);

        if (task == null)
            return false;

        _context.StudyTasks.Remove(task);
        await _context.SaveChangesAsync();

        return true;
    }

    // Get a task with its checklist items for the specified user and course.
    public async Task<StudyTask?> GetTaskByIdAsync(
        int taskId,
        int courseId,
        string userId)
    {
        return await _context.StudyTasks
            .Include(t => t.ChecklistItems)
            .FirstOrDefaultAsync(t =>
                t.Id == taskId &&
                t.CourseId == courseId &&
                t.UserId == userId);
    }

    // Update task details and synchronize checklist items.
    public async Task UpdateTaskAsync(
        StudyTask task,
        string title,
        string? description,
        DateTime? deadline,
        int priority,
        double estimatedHours,
        List<int> itemIds,
        List<string> itemTitles,
        List<string> newItemTitles)
    {
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
    }
}