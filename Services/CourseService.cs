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

