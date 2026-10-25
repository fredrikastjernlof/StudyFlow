using Microsoft.EntityFrameworkCore;
using StudyFlow.Data;
using StudyFlow.Models;

namespace StudyFlow.Services;

public class CourseService
{
    private readonly ApplicationDbContext _context;

    // Inject the database context.
    public CourseService(ApplicationDbContext context)
    {
        _context = context;
    }

    // Get all courses belonging to a specific user.
    public async Task<List<Course>> GetUserCoursesAsync(string userId)
    {
        return await _context.Courses
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    // Get a course only if it belongs to the specified user.
    public async Task<Course?> GetCourseByIdAsync(int courseId, string userId)
    {
        return await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == courseId && c.UserId == userId);
    }

    // Update a course only if it belongs to the specified user.
    public async Task<bool> UpdateCourseAsync(
        int courseId, string userId, string name, string color)
    {
        var course = await GetCourseByIdAsync(courseId, userId);

        if (course == null)
        {
            return false;
        }

        course.Name = name.Trim();
        course.Color = color;

        await _context.SaveChangesAsync();

        return true;
    }

    // Delete a course only if it belongs to the specified user.
    public async Task<bool> DeleteCourseAsync(int courseId, string userId)
    {
        var course = await GetCourseByIdAsync(courseId, userId);

        if (course == null)
        {
            return false;
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        return true;
    }

    // Create a new course for the specified user.
    public async Task CreateCourseAsync(string userId, string name, string color)
    {
        var course = new Course
        {
            Name = name.Trim(),
            Color = color,
            UserId = userId
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();
    }
}

