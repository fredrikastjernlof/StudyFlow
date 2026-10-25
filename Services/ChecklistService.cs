using Microsoft.EntityFrameworkCore;
using StudyFlow.Data;
using StudyFlow.Models;

namespace StudyFlow.Services;

public class ChecklistService
{
    private readonly ApplicationDbContext _context;

    // Inject the database context.
    public ChecklistService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Update the completion status of a checklist item.
    public async Task<bool> UpdateChecklistItemAsync(
        int itemId,
        int courseId,
        string userId,
        bool isCompleted)
    {
        var item = await _context.ChecklistItems
            .Include(i => i.StudyTask)
            .FirstOrDefaultAsync(i =>
                i.Id == itemId &&
                i.StudyTask.CourseId == courseId &&
                i.StudyTask.UserId == userId);

        if (item == null)
            return false;

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
        var task = await _context.StudyTasks.FirstOrDefaultAsync(t =>
            t.Id == taskId &&
            t.CourseId == courseId &&
            t.UserId == userId);

        if (task == null)
            return false;

        var item = new ChecklistItem
        {
            Title = title.Trim(),
            StudyTaskId = task.Id
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
            .Include(i => i.StudyTask)
            .FirstOrDefaultAsync(i =>
                i.Id == itemId &&
                i.StudyTask.CourseId == courseId &&
                i.StudyTask.UserId == userId);

        if (item == null)
            return false;

        _context.ChecklistItems.Remove(item);
        await _context.SaveChangesAsync();

        return true;
    }
}