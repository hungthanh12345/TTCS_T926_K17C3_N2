using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InternshipManagementApi.migrations
{
    /// <inheritdoc />
    public partial class AddWorkScheduleRecurrence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "effective_end_date",
                table: "work_schedules",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<DateOnly>(
                name: "effective_start_date",
                table: "work_schedules",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "recurrence_type",
                table: "work_schedules",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "recurrence_weeks",
                table: "work_schedules",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_work_schedules_recurrence",
                table: "work_schedules",
                sql: "(`effective_start_date` IS NULL AND `recurrence_type` IS NULL AND `recurrence_weeks` IS NULL AND `effective_end_date` IS NULL) OR (`effective_start_date` IS NOT NULL AND `recurrence_type` IS NOT NULL AND ((`recurrence_type` = 'NONE' AND `recurrence_weeks` IS NOT NULL AND `recurrence_weeks` = 1 AND `effective_end_date` IS NOT NULL AND `effective_end_date` >= `effective_start_date`) OR (`recurrence_type` = 'WEEKLY' AND ((`recurrence_weeks` IS NULL AND `effective_end_date` IS NULL) OR (`recurrence_weeks` IS NOT NULL AND `recurrence_weeks` BETWEEN 1 AND 52 AND `effective_end_date` IS NOT NULL AND `effective_end_date` >= `effective_start_date`)))))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "ck_work_schedules_recurrence",
                table: "work_schedules");

            migrationBuilder.DropColumn(
                name: "effective_end_date",
                table: "work_schedules");

            migrationBuilder.DropColumn(
                name: "effective_start_date",
                table: "work_schedules");

            migrationBuilder.DropColumn(
                name: "recurrence_type",
                table: "work_schedules");

            migrationBuilder.DropColumn(
                name: "recurrence_weeks",
                table: "work_schedules");
        }
    }
}
