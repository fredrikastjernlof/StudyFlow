using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudyFlow.Models;

namespace StudyFlow.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    // Database tables for courses, tasks and checklist items.
public DbSet<Course> Courses { get; set; } = null!;
public DbSet<StudyTask> StudyTasks { get; set; } = null!;
public DbSet<ChecklistItem> ChecklistItems { get; set; } = null!;
}
