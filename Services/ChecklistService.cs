
using Microsoft.EntityFrameworkCore;
using StudyFlow.Data;
using StudyFlow.Models;

namespace StudyFlow.Services;

public class ChecklistService
{
    private readonly ApplicationDbContext _context;

    public ChecklistService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Update a checklist item's completion status.
    public async Task<bool> UpdateChecklistItemAsync(
        int itemId,
        int courseId,
        string userId,
        bool isCompleted)
    {
        var item = await _context.ChecklistItems
            .FirstOrDefaultAsync(i =>
                i.Id == itemId &&
                i.StudyTask.CourseId == courseId &&
                i.StudyTask.UserId == userId);

        if (item == null)
        {
            return false;
        }

        item.IsCompleted = isCompleted;
        await _context.SaveChangesAsync();

        return true;
    }

    // Add a checklist item to a task owned by the user.
    public async Task<bool> AddChecklistItemAsync(
        int taskId,
        int courseId,
        string userId,
        string title)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 100)
        {
            throw new ArgumentException("Invalid checklist item title.", nameof(title));
        }

        var taskExists = await _context.StudyTasks
            .AnyAsync(t =>
                t.Id == taskId &&
                t.CourseId == courseId &&
                t.UserId == userId);

        if (!taskExists)
        {
            return false;
        }

        var item = new ChecklistItem
        {
            Title = title.Trim(),
            StudyTaskId = taskId
        };

        _context.ChecklistItems.Add(item);
        await _context.SaveChangesAsync();

        return true;
    }

    // Delete a checklist item belonging to the specified user and course.
    public async Task<bool> DeleteChecklistItemAsync(
        int itemId,
        int courseId,
        string userId)
    {
        var item = await _context.ChecklistItems
            .FirstOrDefaultAsync(i =>
                i.Id == itemId &&
                i.StudyTask.CourseId == courseId &&
                i.StudyTask.UserId == userId);

        if (item == null)
        {
            return false;
        }

        _context.ChecklistItems.Remove(item);
        await _context.SaveChangesAsync();

        return true;
    }
}
