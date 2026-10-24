using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StudyFlow.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCascadeDeleteForCourses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudyTasks_Courses_CourseId",
                table: "StudyTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_StudyTasks_Courses_CourseId",
                table: "StudyTasks",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StudyTasks_Courses_CourseId",
                table: "StudyTasks");

            migrationBuilder.AddForeignKey(
                name: "FK_StudyTasks_Courses_CourseId",
                table: "StudyTasks",
                column: "CourseId",
                principalTable: "Courses",
                principalColumn: "Id");
        }
    }
}
