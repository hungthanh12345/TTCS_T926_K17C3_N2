using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace InternshipManagementApi.migrations
{
    /// <inheritdoc />
    public partial class Sprint3ReviewAndEmail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "students",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "students",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reviewed_by_user_id",
                table: "students",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "rejection_reason",
                table: "student_documents",
                type: "text",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "review_status",
                table: "student_documents",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "PENDING")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "reviewed_at",
                table: "student_documents",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "reviewed_by_user_id",
                table: "student_documents",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "email_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    subject_template = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    body_template = table.Column<string>(type: "text", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.ComputedColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_templates", x => x.id);
                    table.UniqueConstraint("AK_email_templates_code", x => x.code);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "email_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    student_id = table.Column<int>(type: "int", nullable: true),
                    recipient_email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    template_code = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "PENDING")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    error_message = table.Column<string>(type: "text", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    retry_count = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)"),
                    sent_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    next_attempt_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_logs", x => x.id);
                    table.ForeignKey(
                        name: "FK_email_logs_email_templates_template_code",
                        column: x => x.template_code,
                        principalTable: "email_templates",
                        principalColumn: "code",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_email_logs_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "email_templates",
                columns: new[] { "id", "body_template", "code", "created_at", "is_active", "subject_template" },
                values: new object[,]
                {
                    { 1, "Chào {{StudentName}},\n\nHồ sơ đăng ký thực tập của bạn đã được duyệt cho chương trình {{ProgramName}}{{StartDate}}.\n\nBước tiếp theo: {{NextSteps}}\n\nTrân trọng,\nBộ phận Nhân sự", "APP_APPROVED", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Kết quả xét duyệt hồ sơ thực tập" },
                    { 2, "Chào {{StudentName}},\n\nCảm ơn bạn đã đăng ký chương trình {{ProgramName}}. Sau khi xem xét, hồ sơ của bạn chưa được duyệt.\n\nLý do: {{RejectionReason}}\n\nNếu cần hỗ trợ thêm, vui lòng liên hệ bộ phận Nhân sự.\n\nTrân trọng,\nBộ phận Nhân sự", "APP_REJECTED", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "Kết quả xét duyệt hồ sơ thực tập" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_students_reviewed_by_user_id",
                table: "students",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_documents_review_status",
                table: "student_documents",
                column: "review_status");

            migrationBuilder.CreateIndex(
                name: "IX_student_documents_reviewed_by_user_id",
                table: "student_documents",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_email_logs_status_next_attempt_at_id",
                table: "email_logs",
                columns: new[] { "status", "next_attempt_at", "id" });

            migrationBuilder.CreateIndex(
                name: "IX_email_logs_student_id_created_at",
                table: "email_logs",
                columns: new[] { "student_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_email_logs_template_code",
                table: "email_logs",
                column: "template_code");

            migrationBuilder.AddForeignKey(
                name: "FK_student_documents_users_reviewed_by_user_id",
                table: "student_documents",
                column: "reviewed_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_students_users_reviewed_by_user_id",
                table: "students",
                column: "reviewed_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException("Sprint 3 migration is forward-only to protect review and email history.");
        }
    }
}
