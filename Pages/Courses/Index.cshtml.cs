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

    // Available pastel colors for courses.
    public IReadOnlyList<string> AvailableColors => Course.AvailableColors;

    // Validate the course name before creating a course.
    [BindProperty]
    [Required(ErrorMessage = "You must enter a course name.")]
    [StringLength(100, ErrorMessage = "The course name cannot exceed 100 characters.")]
    public string CourseName { get; set; } = string.Empty;

    // Stores the selected course color.
    [BindProperty]
    [Required(ErrorMessage = "You must select a course color.")]
    public string CourseColor { get; set; } = "#D6CBFC";

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
            ModelState.AddModelError(nameof(CourseName), "You must enter a course name.");
        }

        // Only allow colors from the predefined palette.
        if (!AvailableColors.Contains(CourseColor, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(nameof(CourseColor), "Please select a color from the palette.");
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