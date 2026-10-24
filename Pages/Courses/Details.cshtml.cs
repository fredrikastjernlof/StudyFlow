using Microsoft.AspNetCore.Authorization;
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

        return Page();
    }
}