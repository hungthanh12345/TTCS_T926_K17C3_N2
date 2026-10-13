using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementApi.migrations
{
    /// <inheritdoc />
    public partial class Sprint4Workforce : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "allowances",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    allowance_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    paid_amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false, defaultValue: 0m),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_allowances", x => x.id);
                    table.CheckConstraint("ck_allowances_amount", "`amount` > 0 AND `paid_amount` >= 0 AND `paid_amount` <= `amount`");
                    table.CheckConstraint("ck_allowances_period", "`period_end` >= `period_start`");
                    table.CheckConstraint("ck_allowances_status", "`status` IN ('ACTIVE', 'CANCELLED')");
                    table.ForeignKey(
                        name: "FK_allowances_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "attendances",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    check_in_time = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    check_out_time = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    duration_minutes = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_attendances", x => x.id);
                    table.CheckConstraint("ck_attendances_checkout_order", "`check_out_time` IS NULL OR `check_out_time` >= `check_in_time`");
                    table.CheckConstraint("ck_attendances_duration_nonnegative", "`duration_minutes` IS NULL OR `duration_minutes` >= 0");
                    table.CheckConstraint("ck_attendances_status", "`status` IN ('CHECKED_IN', 'COMPLETED')");
                    table.ForeignKey(
                        name: "FK_attendances_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "leave_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    reason = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reviewed_by_user_id = table.Column<int>(type: "int", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    rejection_reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_leave_requests", x => x.id);
                    table.CheckConstraint("ck_leave_requests_date_order", "`end_date` >= `start_date`");
                    table.CheckConstraint("ck_leave_requests_status", "`status` IN ('PENDING', 'APPROVED', 'REJECTED')");
                    table.ForeignKey(
                        name: "FK_leave_requests_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_leave_requests_users_reviewed_by_user_id",
                        column: x => x.reviewed_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "student_groups",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_groups", x => x.id);
                    table.CheckConstraint("ck_student_groups_status", "`status` IN ('ACTIVE', 'INACTIVE')");
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "support_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    student_id = table.Column<int>(type: "int", nullable: false),
                    subject = table.Column<string>(type: "varchar(160)", maxLength: 160, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(5000)", maxLength: 5000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    category = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    response = table.Column<string>(type: "varchar(4000)", maxLength: 4000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    handled_by_user_id = table.Column<int>(type: "int", nullable: true),
                    handled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_support_requests", x => x.id);
                    table.CheckConstraint("ck_support_requests_status", "`status` IN ('PENDING', 'IN_PROGRESS', 'RESOLVED')");
                    table.ForeignKey(
                        name: "FK_support_requests_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_support_requests_users_handled_by_user_id",
                        column: x => x.handled_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "allowance_payments",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    allowance_id = table.Column<int>(type: "int", nullable: false),
                    amount = table.Column<decimal>(type: "decimal(12,2)", precision: 12, scale: 2, nullable: false),
                    paid_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    recorded_by_user_id = table.Column<int>(type: "int", nullable: false),
                    reference = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_allowance_payments", x => x.id);
                    table.CheckConstraint("ck_allowance_payments_amount", "`amount` > 0");
                    table.ForeignKey(
                        name: "FK_allowance_payments_allowances_allowance_id",
                        column: x => x.allowance_id,
                        principalTable: "allowances",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_allowance_payments_users_recorded_by_user_id",
                        column: x => x.recorded_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "student_group_memberships",
                columns: table => new
                {
                    student_id = table.Column<int>(type: "int", nullable: false),
                    student_group_id = table.Column<int>(type: "int", nullable: false),
                    joined_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_student_group_memberships", x => new { x.student_group_id, x.student_id });
                    table.ForeignKey(
                        name: "FK_student_group_memberships_student_groups_student_group_id",
                        column: x => x.student_group_id,
                        principalTable: "student_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_student_group_memberships_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "work_schedules",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(120)", maxLength: 120, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    target_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    student_group_id = table.Column<int>(type: "int", nullable: true),
                    student_id = table.Column<int>(type: "int", nullable: true),
                    is_flexible = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_schedules", x => x.id);
                    table.CheckConstraint("ck_work_schedules_status", "`status` IN ('ACTIVE', 'INACTIVE')");
                    table.CheckConstraint("ck_work_schedules_target", "(`target_type` = 'STUDENT' AND `student_id` IS NOT NULL AND `student_group_id` IS NULL) OR (`target_type` = 'GROUP' AND `student_id` IS NULL AND `student_group_id` IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_work_schedules_student_groups_student_group_id",
                        column: x => x.student_group_id,
                        principalTable: "student_groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_work_schedules_students_student_id",
                        column: x => x.student_id,
                        principalTable: "students",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "work_schedule_days",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    work_schedule_id = table.Column<int>(type: "int", nullable: false),
                    day_of_week = table.Column<int>(type: "int", nullable: false),
                    start_time = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    end_time = table.Column<TimeSpan>(type: "time(6)", nullable: false),
                    break_start = table.Column<TimeSpan>(type: "time(6)", nullable: true),
                    break_end = table.Column<TimeSpan>(type: "time(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_work_schedule_days", x => x.id);
                    table.CheckConstraint("ck_work_schedule_days_break_pair", "(`break_start` IS NULL AND `break_end` IS NULL) OR (`break_start` IS NOT NULL AND `break_end` IS NOT NULL AND `break_start` < `break_end` AND `break_start` >= `start_time` AND `break_end` <= `end_time`)");
                    table.CheckConstraint("ck_work_schedule_days_time_order", "`start_time` < `end_time`");
                    table.CheckConstraint("ck_work_schedule_days_weekday", "`day_of_week` BETWEEN 0 AND 6");
                    table.ForeignKey(
                        name: "FK_work_schedule_days_work_schedules_work_schedule_id",
                        column: x => x.work_schedule_id,
                        principalTable: "work_schedules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_allowance_payments_allowance_id_paid_at",
                table: "allowance_payments",
                columns: new[] { "allowance_id", "paid_at" });

            migrationBuilder.CreateIndex(
                name: "IX_allowance_payments_recorded_by_user_id",
                table: "allowance_payments",
                column: "recorded_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_allowances_student_id_allowance_type_period_start_period_end",
                table: "allowances",
                columns: new[] { "student_id", "allowance_type", "period_start", "period_end" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_allowances_student_id_period_start_period_end",
                table: "allowances",
                columns: new[] { "student_id", "period_start", "period_end" });

            migrationBuilder.CreateIndex(
                name: "IX_attendances_date",
                table: "attendances",
                column: "date");

            migrationBuilder.CreateIndex(
                name: "IX_attendances_student_id_date",
                table: "attendances",
                columns: new[] { "student_id", "date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_reviewed_by_user_id",
                table: "leave_requests",
                column: "reviewed_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_leave_requests_student_id_status_start_date_end_date",
                table: "leave_requests",
                columns: new[] { "student_id", "status", "start_date", "end_date" });

            migrationBuilder.CreateIndex(
                name: "IX_student_group_memberships_student_group_id",
                table: "student_group_memberships",
                column: "student_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_group_memberships_student_id",
                table: "student_group_memberships",
                column: "student_id");

            migrationBuilder.CreateIndex(
                name: "IX_student_groups_name",
                table: "student_groups",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_student_groups_status",
                table: "student_groups",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_support_requests_handled_by_user_id",
                table: "support_requests",
                column: "handled_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_support_requests_status_created_at",
                table: "support_requests",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_support_requests_student_id_created_at",
                table: "support_requests",
                columns: new[] { "student_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_work_schedule_days_work_schedule_id_day_of_week",
                table: "work_schedule_days",
                columns: new[] { "work_schedule_id", "day_of_week" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_work_schedules_student_group_id",
                table: "work_schedules",
                column: "student_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_work_schedules_student_id",
                table: "work_schedules",
                column: "student_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "allowance_payments");

            migrationBuilder.DropTable(
                name: "attendances");

            migrationBuilder.DropTable(
                name: "leave_requests");

            migrationBuilder.DropTable(
                name: "student_group_memberships");

            migrationBuilder.DropTable(
                name: "support_requests");

            migrationBuilder.DropTable(
                name: "work_schedule_days");

            migrationBuilder.DropTable(
                name: "allowances");

            migrationBuilder.DropTable(
                name: "work_schedules");

            migrationBuilder.DropTable(
                name: "student_groups");
        }
    }
}
