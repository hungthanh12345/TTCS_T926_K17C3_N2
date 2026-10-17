using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementApi.migrations
{
    /// <inheritdoc />
    public partial class RestrictMentorTaskDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_sprint2_internship_tasks_mentor",
                table: "internship_tasks");

            migrationBuilder.AddForeignKey(
                name: "fk_sprint2_internship_tasks_mentor",
                table: "internship_tasks",
                column: "mentor_id",
                principalTable: "mentors",
                principalColumn: "id",
                onUpdate: ReferentialAction.Cascade,
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) =>
            throw new NotSupportedException(
                "Reverting this migration would restore ON DELETE CASCADE for internship tasks, which the approved business rule forbids.");
    }
}
