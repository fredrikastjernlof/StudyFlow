using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StudyFlow.Data;

namespace StudyFlow.Pages.Courses;

// Only authenticated users can edit courses.
[Authorize]
public class EditModel : PageModel
{
    private readonly ApplicationDbContext _context;

    // Inject the database context.
    public EditModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public int CourseId { get; set; }

    [BindProperty]
    [Required(ErrorMessage = "Du måste ange ett kursnamn.")]
    [StringLength(100, ErrorMessage = "Kursnamnet får innehålla högst 100 tecken.")]
    public string CourseName { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Ogiltig färgkod.")]
    public string CourseColor { get; set; } = "#B8D8C0";

    // Load the course that the user wants to edit.
    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Only retrieve courses owned by the current user.
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (course == null)
        {
            return NotFound();
        }

        CourseId = course.Id;
        CourseName = course.Name;
        CourseColor = course.Color;

        return Page();
    }

    // Save changes to an existing course.
    public async Task<IActionResult> OnPostAsync(int id)
    {
        // Get the ID of the authenticated user.
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Find the course and verify ownership.
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (course == null)
        {
            return NotFound();
        }

        CourseId = course.Id;

        // Reject empty or whitespace-only course names.
        if (string.IsNullOrWhiteSpace(CourseName))
        {
            ModelState.AddModelError(
                nameof(CourseName),
                "Du måste ange ett kursnamn."
            );
        }

        // Display validation errors without saving changes.
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Update the existing course.
        course.Name = CourseName.Trim();
        course.Color = CourseColor;

        // Save changes to the database.
        await _context.SaveChangesAsync();

        // Return to the course list.
        return RedirectToPage("./Index");
    }
}