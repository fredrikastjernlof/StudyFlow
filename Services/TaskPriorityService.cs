using StudyFlow.Models;
using StudyFlow.ViewModels;

namespace StudyFlow.Services;

public class TaskPriorityService
{
    public int CalculateScore(StudyTask task)
    {
        int score = 0;

        // 1. Priority
        score += task.Priority switch
        {
            1 => 10, // Low priority
            2 => 20, // Medium priority
            3 => 30, // High priority
            _ => 0
        };

        // 2. Deadline
        if (task.Deadline.HasValue)
        {
            int daysLeft = (task.Deadline.Value.Date - DateTime.Today).Days;

            if (daysLeft <= 0)
                score += 40;
            else if (daysLeft <= 3)
                score += 30;
            else if (daysLeft <= 7)
                score += 20;
            else
                score += 10;
        }

        // 3. Status
        if (task.Status == "InProgress")
        {
            score += 15;
        }

        // 4. Estimated time
        if (task.EstimatedHours > 0 && task.EstimatedHours <= 2)
        {
            score += 10;
        }
        else if (task.EstimatedHours > 2 && task.EstimatedHours <= 5)
        {
            score += 5;
        }

        return score;
    }

    public List<TaskPriorityResult> GetPrioritizedTasks(
        IEnumerable<StudyTask> tasks)
    {
        return tasks
            .Where(task => task.Status != "Completed")
            .Select(task => new TaskPriorityResult
            {
                Task = task,
                Score = CalculateScore(task)
            })
            .OrderByDescending(result => result.Score)
            .ThenBy(result => result.Task.Deadline ?? DateTime.MaxValue)
            .ToList();
    }

    public TaskPriorityResult? GetTodaysFocus(
        IEnumerable<StudyTask> tasks)
    {
        return GetPrioritizedTasks(tasks).FirstOrDefault();
    }
}