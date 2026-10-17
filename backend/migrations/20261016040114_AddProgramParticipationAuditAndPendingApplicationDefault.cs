using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementApi.migrations
{
    /// <inheritdoc />
    public partial class AddProgramParticipationAuditAndPendingApplicationDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "application_status",
                table: "students",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PENDING_APPROVAL",
                collation: "utf8mb4_unicode_ci",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "APPROVED",
                oldCollation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "student_program_audit_events",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    program_id = table.Column<int>(type: "int", nullable: false),
                    actor_user_id = table.Column<int>(type: "int", nullable: false),
                    event_type = table.Column<string>(type: "varchar(40)", maxLength: 40, nullable: false, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    previous_status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    new_status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reason = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true, collation: "utf8mb4_unicode_ci")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    occurred_at_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_program_audit_events", x => x.id);
                    table.CheckConstraint("ck_student_program_audit_events_status", "(`event_type` = 'PARTICIPATION_STATUS_CHANGED' AND `new_status` IS NOT NULL AND `new_status` IN ('UNVERIFIED', 'ACTIVE', 'ENDED', 'CANCELLED') AND (`previous_status` IS NULL OR `previous_status` IN ('UNVERIFIED', 'ACTIVE', 'ENDED', 'CANCELLED'))) OR (`event_type` IN ('COMPLETION_CONFIRMED', 'COMPLETION_REVOKED') AND `previous_status` IS NULL AND `new_status` IS NULL)");
                    table.CheckConstraint("ck_student_program_audit_events_type", "`event_type` IN ('PARTICIPATION_STATUS_CHANGED', 'COMPLETION_CONFIRMED', 'COMPLETION_REVOKED')");
                    table.ForeignKey(
                        name: "FK_student_program_audit_events_internship_programs_program_id",
                        column: x => x.program_id,
                        principalTable: "internship_programs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_program_audit_events_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_program_audit_events_users_actor_user_id",
                        column: x => x.actor_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4")
                .Annotation("Relational:Collation", "utf8mb4_unicode_ci");

            migrationBuilder.CreateIndex(
                name: "IX_student_program_audit_events_actor_user_id",
                table: "student_program_audit_events",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_student_program_audit_events_program_id",
                table: "student_program_audit_events",
                columns: new[] { "program_id", "id" });

            migrationBuilder.CreateIndex(
                name: "ix_student_program_audit_events_student_program_id",
                table: "student_program_audit_events",
                columns: new[] { "student_id", "program_id", "id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "student_program_audit_events");

            migrationBuilder.AlterColumn<string>(
                name: "application_status",
                table: "students",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "APPROVED",
                collation: "utf8mb4_unicode_ci",
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldDefaultValue: "PENDING_APPROVAL",
                oldCollation: "utf8mb4_unicode_ci")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");
        }
    }
}
