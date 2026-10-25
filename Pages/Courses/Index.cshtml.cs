using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using StudyFlow.Models;
using StudyFlow.Services;

namespace StudyFlow.Pages.Courses;

// Only authenticated users can access this page.
[Authorize]
public class IndexModel : PageModel
{
    private readonly CourseService _courseService;

    // Inject the service for course operations.
    public IndexModel(CourseService courseService)
    {
        _courseService = courseService;
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
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userId != null)
        {
            Courses = await _courseService.GetUserCoursesAsync(userId);
        }
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
                Courses = await _courseService.GetUserCoursesAsync(userId);
            }

            return Page();
        }

        // Create the course using the course service.
        await _courseService.CreateCourseAsync(userId, CourseName, CourseColor);

        // Redirect to prevent duplicate submissions when refreshing.
        return RedirectToPage();
    }
}