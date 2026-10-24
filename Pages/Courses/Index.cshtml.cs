/*
This file contains the C# logic, 
such as retrieving the user's 
courses from the database.
*/
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.Data;
using StudyFlow.Models;

namespace StudyFlow.Pages.Courses;

// Only authenticated users can access this page.
[Authorize]
public class IndexModel : PageModel
{
    private readonly ApplicationDbContext _context;

    // Inject the database context to access course data.
    public IndexModel(ApplicationDbContext context)
    {
        _context = context;
    }

    // Courses belonging to the currently logged-in user.
    public List<Course> Courses { get; set; } = new();

    // Validate the course name before creating a course.
    [BindProperty]
    [Required(ErrorMessage = "Du måste ange ett kursnamn.")]
    [StringLength(100, ErrorMessage = "Kursnamnet får innehålla högst 100 tecken.")]
    public string CourseName { get; set; } = string.Empty;

    // Stores the selected course color.
    [BindProperty]
    [Required]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Ogiltig färgkod.")]
    public string CourseColor { get; set; } = "#B8D8C0";

    public async Task OnGetAsync()
    {
        // Get the ID of the authenticated user.
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Load only courses owned by this user.
        Courses = await _context.Courses
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Get the ID of the authenticated user.
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Reject empty or whitespace-only course names.
        if (string.IsNullOrWhiteSpace(CourseName))
        {
            ModelState.AddModelError(nameof(CourseName), "Du måste ange ett kursnamn.");
        }

        // Return to the page if validation fails.
        if (!ModelState.IsValid || userId == null)
        {
            if (userId != null)
            {
                // Reload existing courses before displaying validation errors.
                Courses = await _context.Courses
                    .Where(c => c.UserId == userId)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
            }

            return Page();
        }

        // Create a new course for the current user.
        var course = new Course
        {
            Name = CourseName.Trim(),
            Color = CourseColor,
            UserId = userId
        };

        // Save the course to the database.
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        // Redirect to prevent duplicate submissions when refreshing.
        return RedirectToPage();
    }
}