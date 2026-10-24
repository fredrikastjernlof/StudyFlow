using Microsoft.AspNetCore.Authorization;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using StudyFlow.Data;
using StudyFlow.Models;
using System.Security.Claims;

namespace StudyFlow.Pages.Courses;

[Authorize]
public class DetailsModel : PageModel
{
    private readonly ApplicationDbContext _context;

    public DetailsModel(ApplicationDbContext context)
    {
        _context = context;
    }

    public Course Course { get; set; } = null!;

    // Fields used when editing a course.
    [BindProperty]
    [Required(ErrorMessage = "Du måste ange ett kursnamn.")]
    [StringLength(100, ErrorMessage = "Kursnamnet får innehålla högst 100 tecken.")]
    public string CourseName { get; set; } = string.Empty;

    [BindProperty]
    [Required]
    [RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Ogiltig färgkod.")]
    public string CourseColor { get; set; } = "#B8D8C0";

    public async Task<IActionResult> OnGetAsync(int id)
    {
        // Get the currently logged-in user's ID.
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Find the course and verify that it belongs to the user.
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (course == null)
        {
            return NotFound();
        }

        Course = course;

        // Populate the edit form with the course's current values.
        CourseName = course.Name;
        CourseColor = course.Color;

        return Page();
    }

    // Save changes made in the edit course dialog.
    public async Task<IActionResult> OnPostEditAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Find the course and verify ownership.
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (course == null)
        {
            return NotFound();
        }

        Course = course;

        // Reject empty or whitespace-only course names.
        if (string.IsNullOrWhiteSpace(CourseName))
        {
            ModelState.AddModelError(
                nameof(CourseName),
                "Du måste ange ett kursnamn."
            );
        }

        // Do not save invalid input.
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Update and save the course.
        course.Name = CourseName.Trim();
        course.Color = CourseColor;

        await _context.SaveChangesAsync();

        return RedirectToPage("./Details", new { id = course.Id });
    }

    // Delete a course and its related tasks and checklist items.
    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        // Find the course and verify that it belongs to the user.
        var course = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId);

        if (course == null)
        {
            return NotFound();
        }

        // Related tasks and checklist items are deleted through cascade delete.
        _context.Courses.Remove(course);

        await _context.SaveChangesAsync();

        // Return to the course overview after deletion.
        return RedirectToPage("./Index");
    }
}