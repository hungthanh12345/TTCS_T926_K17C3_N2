using InternshipManagementApi.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagementApi.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Mentor> Mentors => Set<Mentor>();
        public DbSet<Student> Students => Set<Student>();
        public DbSet<Department> Departments => Set<Department>();
        public DbSet<InternshipProgram> InternshipPrograms => Set<InternshipProgram>();
        public DbSet<StudentDocument> StudentDocuments => Set<StudentDocument>();
        public DbSet<InternshipContract> InternshipContracts => Set<InternshipContract>();
        public DbSet<InternshipTask> Tasks => Set<InternshipTask>();
        public DbSet<WeeklyReport> WeeklyReports => Set<WeeklyReport>();
        public DbSet<MentorFeedback> MentorFeedbacks => Set<MentorFeedback>();
        public DbSet<InternshipEvaluation> InternshipEvaluations => Set<InternshipEvaluation>();
        public DbSet<UserNotification> Notifications => Set<UserNotification>();
        public DbSet<EmailTemplate> EmailTemplates => Set<EmailTemplate>();
        public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
        public DbSet<Attendance> Attendances => Set<Attendance>();
        public DbSet<StudentGroup> StudentGroups => Set<StudentGroup>();
        public DbSet<StudentGroupMembership> StudentGroupMemberships => Set<StudentGroupMembership>();
        public DbSet<WorkSchedule> WorkSchedules => Set<WorkSchedule>();
        public DbSet<WorkScheduleDay> WorkScheduleDays => Set<WorkScheduleDay>();
        public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>();
        public DbSet<Allowance> Allowances => Set<Allowance>();
        public DbSet<AllowancePayment> AllowancePayments => Set<AllowancePayment>();
        public DbSet<SupportRequest> SupportRequests => Set<SupportRequest>();
        public DbSet<StudentProgramAuditEvent> StudentProgramAuditEvents => Set<StudentProgramAuditEvent>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Role entity
            modelBuilder.Entity<Role>(entity =>
            {
                entity.ToTable("roles");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(50).IsRequired();
                entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(255);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAddOrUpdate();

                entity.HasIndex(e => e.Name).IsUnique();
            });

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("users");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(150).IsRequired();
                entity.Property(e => e.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsRequired();
                entity.Property(e => e.RoleId).HasColumnName("role_id").IsRequired();
                entity.Property(e => e.Status)
                      .HasColumnName("status")
                      .HasConversion<string>()
                      .HasColumnType("enum('ACTIVE','INACTIVE','LOCKED','PENDING_APPROVAL','REJECTED')")
                      .HasDefaultValue(UserStatus.ACTIVE);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAddOrUpdate();

                entity.HasIndex(e => e.Email).IsUnique();
                entity.HasIndex(e => e.RoleId);

                entity.HasOne(e => e.Role)
                      .WithMany(r => r.Users)
                      .HasForeignKey(e => e.RoleId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure Mentor entity
            modelBuilder.Entity<Mentor>(entity =>
            {
                entity.ToTable("mentors");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20);
                entity.Property(e => e.Department).HasColumnName("department").HasMaxLength(100).IsRequired();
                entity.Property(e => e.Specialization).HasColumnName("specialization").HasMaxLength(150);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAddOrUpdate();

                entity.HasIndex(e => e.UserId).IsUnique();

                entity.HasOne(e => e.User)
                      .WithOne(u => u.Mentor)
                      .HasForeignKey<Mentor>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure Student entity
            modelBuilder.Entity<Student>(entity =>
            {
                entity.ToTable("students");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.UserId).HasColumnName("user_id");
                entity.Property(e => e.StudentCode).HasColumnName("student_code").HasMaxLength(50).IsRequired();
                entity.Property(e => e.FullName).HasColumnName("full_name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20);
                entity.Property(e => e.University).HasColumnName("university").HasMaxLength(150).IsRequired();
                entity.Property(e => e.Major).HasColumnName("major").HasMaxLength(100).IsRequired();
                entity.Property(e => e.ApplicationStatus)
                    .HasColumnName("application_status")
                    .HasColumnType("varchar(20)")
                    .HasMaxLength(20)
                    .UseCollation("utf8mb4_unicode_ci")
                    .HasDefaultValue(StudentApplicationStatus.PendingApproval)
                    .IsRequired();
                entity.Property(e => e.MentorId).HasColumnName("mentor_id");
                entity.Property(e => e.ProgramId).HasColumnName("program_id");
                entity.Property(e => e.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
                entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at").HasColumnType("datetime(6)");
                entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason").HasColumnType("text");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAddOrUpdate();

                entity.HasIndex(e => e.StudentCode).IsUnique();
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasIndex(e => e.University);
                entity.HasIndex(e => e.Major);
                entity.HasIndex(e => e.MentorId);
                entity.HasIndex(e => e.ProgramId);
                entity.HasIndex(e => e.ReviewedByUserId);

                entity.HasOne(e => e.User)
                      .WithOne(u => u.Student)
                      .HasForeignKey<Student>(e => e.UserId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Mentor)
                      .WithMany(m => m.Students)
                      .HasForeignKey(e => e.MentorId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne(e => e.Program)
                      .WithMany(program => program.Students)
                      .HasForeignKey(e => e.ProgramId)
                      .OnDelete(DeleteBehavior.SetNull);

                entity.HasOne<User>()
                      .WithMany()
                      .HasForeignKey(e => e.ReviewedByUserId)
                      .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Department>(entity =>
            {
                entity.ToTable("departments");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(500);
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAddOrUpdate();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            modelBuilder.Entity<InternshipProgram>(entity =>
            {
                entity.ToTable("internship_programs");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(150).IsRequired();
                entity.Property(e => e.Description).HasColumnName("description").HasMaxLength(1000);
                entity.Property(e => e.DepartmentId).HasColumnName("department_id").IsRequired();
                entity.Property(e => e.StartDate).HasColumnName("start_date").HasColumnType("date");
                entity.Property(e => e.EndDate).HasColumnName("end_date").HasColumnType("date");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAddOrUpdate();
                entity.HasIndex(e => new { e.DepartmentId, e.Name }).IsUnique();
                entity.HasOne(e => e.Department)
                    .WithMany(d => d.Programs)
                    .HasForeignKey(e => e.DepartmentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StudentDocument>(entity =>
            {
                entity.ToTable("student_documents");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.StudentId).HasColumnName("student_id").IsRequired();
                entity.Property(e => e.DocumentType).HasColumnName("document_type").HasMaxLength(30).IsRequired();
                entity.Property(e => e.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255).IsRequired();
                entity.Property(e => e.StoredFileName).HasColumnName("stored_file_name").HasMaxLength(100).IsRequired();
                entity.Property(e => e.ContentType).HasColumnName("content_type").HasMaxLength(100).IsRequired();
                entity.Property(e => e.SizeBytes).HasColumnName("size_bytes").IsRequired();
                entity.Property(e => e.FileContent).HasColumnName("file_content").HasColumnType("longblob");
                entity.Property(e => e.UploadedAt).HasColumnName("uploaded_at")
                    .HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
                entity.Property(e => e.ReviewStatus).HasColumnName("review_status").HasMaxLength(20)
                    .HasDefaultValue("PENDING").IsRequired();
                entity.Property(e => e.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
                entity.Property(e => e.ReviewedAt).HasColumnName("reviewed_at").HasColumnType("datetime(6)");
                entity.Property(e => e.RejectionReason).HasColumnName("rejection_reason").HasColumnType("text");
                entity.HasIndex(e => new { e.StudentId, e.UploadedAt });
                entity.HasIndex(e => e.ReviewStatus);
                entity.HasIndex(e => e.ReviewedByUserId);
                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<InternshipContract>(entity =>
            {
                entity.ToTable("internship_contracts", table =>
                {
                    table.ExcludeFromMigrations();
                    table.HasCheckConstraint("chk_internship_contracts_status",
                        "`status` IN ('PENDING_CONFIRMATION', 'CONFIRMED', 'CANCELLED')");
                });
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(e => e.StudentId).HasColumnName("student_id").IsRequired();
                entity.Property(e => e.ContractNumber).HasColumnName("contract_number").HasMaxLength(50).IsRequired();
                entity.Property(e => e.FileName).HasColumnName("file_name").HasMaxLength(255);
                entity.Property(e => e.ContentType).HasColumnName("content_type").HasMaxLength(150);
                entity.Property(e => e.SizeBytes).HasColumnName("size_bytes").HasDefaultValue(0L).IsRequired();
                entity.Property(e => e.FileContent).HasColumnName("file_content").HasColumnType("longblob");
                entity.Property(e => e.ContentHash).HasColumnName("content_hash").HasMaxLength(64).IsFixedLength();
                entity.Property(e => e.UploadedByUserId).HasColumnName("uploaded_by_user_id");
                entity.Property(e => e.FileUploadedAt).HasColumnName("file_uploaded_at").HasColumnType("datetime(6)");
                entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(30)
                    .HasDefaultValue(ContractStatus.PendingConfirmation).IsRequired();
                entity.Property(e => e.IssuedAt).HasColumnName("issued_at").HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").ValueGeneratedOnAdd();
                entity.Property(e => e.ConfirmedAt).HasColumnName("confirmed_at").HasColumnType("datetime(6)");
                entity.Property(e => e.ConfirmedByUserId).HasColumnName("confirmed_by_user_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").ValueGeneratedOnAdd();
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").ValueGeneratedOnAddOrUpdate();
                entity.HasIndex(e => e.StudentId).IsUnique();
                entity.HasIndex(e => e.ContractNumber).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.ConfirmedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UploadedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<EmailTemplate>(entity =>
            {
                entity.ToTable("email_templates");
                entity.HasKey(template => template.Id);
                entity.Property(template => template.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(template => template.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
                entity.Property(template => template.SubjectTemplate).HasColumnName("subject_template").HasMaxLength(255).IsRequired();
                entity.Property(template => template.BodyTemplate).HasColumnName("body_template").HasColumnType("text").IsRequired();
                entity.Property(template => template.IsActive).HasColumnName("is_active").HasDefaultValue(true).IsRequired();
                entity.Property(template => template.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").ValueGeneratedOnAdd();
                entity.Property(template => template.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").ValueGeneratedOnAddOrUpdate();
                entity.HasData(
                    new EmailTemplate
                    {
                        Id = 1,
                        Code = "APP_APPROVED",
                        SubjectTemplate = "Kết quả xét duyệt hồ sơ thực tập",
                        BodyTemplate = "Chào {{StudentName}},\n\nHồ sơ đăng ký thực tập của bạn đã được duyệt cho chương trình {{ProgramName}}{{StartDate}}.\n\nBước tiếp theo: {{NextSteps}}\n\nTrân trọng,\nBộ phận Nhân sự",
                        IsActive = true,
                        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    },
                    new EmailTemplate
                    {
                        Id = 2,
                        Code = "APP_REJECTED",
                        SubjectTemplate = "Kết quả xét duyệt hồ sơ thực tập",
                        BodyTemplate = "Chào {{StudentName}},\n\nCảm ơn bạn đã đăng ký chương trình {{ProgramName}}. Sau khi xem xét, hồ sơ của bạn chưa được duyệt.\n\nLý do: {{RejectionReason}}\n\nNếu cần hỗ trợ thêm, vui lòng liên hệ bộ phận Nhân sự.\n\nTrân trọng,\nBộ phận Nhân sự",
                        IsActive = true,
                        CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                        UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                    });
            });

            modelBuilder.Entity<EmailLog>(entity =>
            {
                entity.ToTable("email_logs");
                entity.HasKey(log => log.Id);
                entity.Property(log => log.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(log => log.StudentId).HasColumnName("student_id");
                entity.Property(log => log.RecipientEmail).HasColumnName("recipient_email").HasMaxLength(150).IsRequired();
                entity.Property(log => log.TemplateCode).HasColumnName("template_code").HasMaxLength(50).IsRequired();
                entity.Property(log => log.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("PENDING").IsRequired();
                entity.Property(log => log.ErrorMessage).HasColumnName("error_message").HasColumnType("text");
                entity.Property(log => log.RetryCount).HasColumnName("retry_count").HasDefaultValue(0).IsRequired();
                entity.Property(log => log.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").ValueGeneratedOnAdd();
                entity.Property(log => log.SentAt).HasColumnName("sent_at").HasColumnType("datetime(6)");
                entity.Property(log => log.NextAttemptAt).HasColumnName("next_attempt_at").HasColumnType("datetime(6)")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").ValueGeneratedOnAdd();
                entity.HasIndex(log => new { log.Status, log.NextAttemptAt, log.Id });
                entity.HasIndex(log => new { log.StudentId, log.CreatedAt });
                entity.HasOne(log => log.Student)
                    .WithMany()
                    .HasForeignKey(log => log.StudentId)
                    .OnDelete(DeleteBehavior.SetNull);
                entity.HasOne(log => log.Template)
                    .WithMany()
                    .HasForeignKey(log => log.TemplateCode)
                    .HasPrincipalKey(template => template.Code)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<InternshipTask>(entity =>
            {
                entity.ToTable("internship_tasks");
                entity.HasKey(task => task.Id);
                entity.Property(task => task.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(task => task.MentorId).HasColumnName("mentor_id").IsRequired();
                entity.Property(task => task.StudentId).HasColumnName("student_id").IsRequired();
                entity.Property(task => task.Title).HasColumnName("title").HasMaxLength(200).IsRequired();
                entity.Property(task => task.Description).HasColumnName("description").HasMaxLength(2000);
                entity.Property(task => task.DueDate).HasColumnName("due_date").HasColumnType("date");
                entity.Property(task => task.Status)
                    .HasColumnName("status")
                    .HasMaxLength(20)
                    .HasDefaultValue("TO_DO");
                entity.Property(task => task.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
                entity.Property(task => task.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();

                entity.HasIndex(task => new { task.MentorId, task.StudentId });
                entity.HasIndex(task => task.DueDate);
                entity.HasOne(task => task.Mentor)
                    .WithMany()
                    .HasForeignKey(task => task.MentorId)
                    .OnDelete(DeleteBehavior.Restrict)
                    .HasConstraintName("fk_sprint2_internship_tasks_mentor");
                entity.HasOne(task => task.Student)
                    .WithMany()
                    .HasForeignKey(task => task.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<WeeklyReport>(entity =>
            {
                entity.ToTable("weekly_reports");
                entity.HasKey(report => report.Id);
                entity.Property(report => report.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(report => report.StudentId).HasColumnName("student_id").IsRequired();
                entity.Property(report => report.WeekStartDate).HasColumnName("week_start_date").HasColumnType("date").IsRequired();
                entity.Property(report => report.WorkSummary).HasColumnName("work_summary").HasColumnType("text").IsRequired();
                entity.Property(report => report.Results).HasColumnName("results").HasColumnType("text");
                entity.Property(report => report.Challenges).HasColumnName("challenges").HasColumnType("text");
                entity.Property(report => report.NextWeekPlan).HasColumnName("next_week_plan").HasColumnType("text");
                entity.Property(report => report.AttachmentUrl).HasColumnName("attachment_url").HasMaxLength(2048);
                entity.Property(report => report.Status).HasColumnName("status").HasMaxLength(20).HasDefaultValue("SUBMITTED").IsRequired();
                entity.Property(report => report.CreatedAt).HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").ValueGeneratedOnAdd();
                entity.Property(report => report.UpdatedAt).HasColumnName("updated_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").ValueGeneratedOnAddOrUpdate();
                entity.HasIndex(report => new { report.StudentId, report.WeekStartDate }).IsUnique();
                entity.HasOne(report => report.Student)
                    .WithMany()
                    .HasForeignKey(report => report.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(report => report.MentorFeedback)
                    .WithOne(feedback => feedback.WeeklyReport)
                    .HasForeignKey<MentorFeedback>(feedback => feedback.WeeklyReportId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<MentorFeedback>(entity =>
            {
                entity.ToTable("mentor_feedback");
                entity.HasKey(feedback => feedback.Id);
                entity.Property(feedback => feedback.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(feedback => feedback.WeeklyReportId).HasColumnName("weekly_report_id").IsRequired();
                entity.Property(feedback => feedback.MentorId).HasColumnName("mentor_id").IsRequired();
                entity.Property(feedback => feedback.Content).HasColumnName("content").HasColumnType("text").IsRequired();
                entity.Property(feedback => feedback.CreatedAt).HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").ValueGeneratedOnAdd();
                entity.Property(feedback => feedback.UpdatedAt).HasColumnName("updated_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").ValueGeneratedOnAddOrUpdate();
                entity.HasIndex(feedback => feedback.WeeklyReportId).IsUnique();
                entity.HasIndex(feedback => feedback.MentorId);
                entity.HasOne(feedback => feedback.Mentor)
                    .WithMany()
                    .HasForeignKey(feedback => feedback.MentorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<InternshipEvaluation>(entity =>
            {
                entity.ToTable("internship_evaluations", table => table.HasCheckConstraint(
                    "ck_internship_evaluations_scores",
                    "`skills_score` BETWEEN 1 AND 10 AND `attitude_score` BETWEEN 1 AND 10"));
                entity.HasKey(evaluation => evaluation.Id);
                entity.Property(evaluation => evaluation.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(evaluation => evaluation.StudentId).HasColumnName("student_id").IsRequired();
                entity.Property(evaluation => evaluation.MentorId).HasColumnName("mentor_id");
                entity.Property(evaluation => evaluation.MentorNameSnapshot).HasColumnName("mentor_name_snapshot").HasMaxLength(100).IsRequired();
                entity.Property(evaluation => evaluation.SkillsScore).HasColumnName("skills_score").IsRequired();
                entity.Property(evaluation => evaluation.AttitudeScore).HasColumnName("attitude_score").IsRequired();
                entity.Property(evaluation => evaluation.Comments).HasColumnName("comments").HasColumnType("text").IsRequired();
                entity.Property(evaluation => evaluation.EvaluatedAt).HasColumnName("evaluated_at").IsRequired();
                entity.Property(evaluation => evaluation.CreatedAt).HasColumnName("created_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").ValueGeneratedOnAdd();
                entity.Property(evaluation => evaluation.UpdatedAt).HasColumnName("updated_at")
                    .HasDefaultValueSql("CURRENT_TIMESTAMP(6)").ValueGeneratedOnAddOrUpdate();
                entity.HasIndex(evaluation => evaluation.StudentId).IsUnique();
                entity.HasIndex(evaluation => evaluation.MentorId);
                entity.HasOne(evaluation => evaluation.Student)
                    .WithMany()
                    .HasForeignKey(evaluation => evaluation.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(evaluation => evaluation.Mentor)
                    .WithMany()
                    .HasForeignKey(evaluation => evaluation.MentorId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<UserNotification>(entity =>
            {
                entity.ToTable("notifications");
                entity.HasKey(notification => notification.Id);
                entity.Property(notification => notification.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(notification => notification.UserId).HasColumnName("user_id").IsRequired();
                entity.Property(notification => notification.SourceKey).HasColumnName("source_key").HasMaxLength(191).IsRequired();
                entity.Property(notification => notification.Title).HasColumnName("title").HasMaxLength(160).IsRequired();
                entity.Property(notification => notification.Message).HasColumnName("message").HasMaxLength(1000).IsRequired();
                entity.Property(notification => notification.Route).HasColumnName("route").HasMaxLength(255).IsRequired();
                entity.Property(notification => notification.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)").IsRequired();
                entity.Property(notification => notification.ReadAt).HasColumnName("read_at").HasColumnType("datetime(6)");
                entity.HasIndex(notification => new { notification.UserId, notification.SourceKey }).IsUnique();
                entity.HasIndex(notification => new { notification.UserId, notification.CreatedAt });
                entity.HasOne(notification => notification.User)
                    .WithMany()
                    .HasForeignKey(notification => notification.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.ToTable("attendances", table =>
                {
                    table.HasCheckConstraint("ck_attendances_status", "`status` IN ('CHECKED_IN', 'COMPLETED')");
                    table.HasCheckConstraint("ck_attendances_duration_nonnegative", "`duration_minutes` IS NULL OR `duration_minutes` >= 0");
                    table.HasCheckConstraint("ck_attendances_checkout_order", "`check_out_time` IS NULL OR `check_out_time` >= `check_in_time`");
                });
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(item => item.StudentId).HasColumnName("student_id").IsRequired();
                entity.Property(item => item.Date).HasColumnName("date").HasColumnType("date").IsRequired();
                entity.Property(item => item.CheckInTime).HasColumnName("check_in_time").HasColumnType("datetime(6)").IsRequired();
                entity.Property(item => item.CheckOutTime).HasColumnName("check_out_time").HasColumnType("datetime(6)");
                entity.Property(item => item.DurationMinutes).HasColumnName("duration_minutes");
                entity.Property(item => item.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
                entity.Property(item => item.Notes).HasColumnName("notes").HasMaxLength(500);
                entity.Property(item => item.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)").IsRequired();
                entity.Property(item => item.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime(6)").IsRequired();
                entity.HasIndex(item => new { item.StudentId, item.Date }).IsUnique();
                entity.HasIndex(item => item.Date);
                entity.HasOne(item => item.Student).WithMany().HasForeignKey(item => item.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<StudentGroup>(entity =>
            {
                entity.ToTable("student_groups", table => table.HasCheckConstraint(
                    "ck_student_groups_status", "`status` IN ('ACTIVE', 'INACTIVE')"));
                entity.HasKey(group => group.Id);
                entity.Property(group => group.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(group => group.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
                entity.Property(group => group.Description).HasColumnName("description").HasMaxLength(500);
                entity.Property(group => group.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
                entity.Property(group => group.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)").IsRequired();
                entity.Property(group => group.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime(6)").IsRequired();
                entity.HasIndex(group => group.Name).IsUnique();
                entity.HasIndex(group => group.Status);
            });

            modelBuilder.Entity<StudentGroupMembership>(entity =>
            {
                entity.ToTable("student_group_memberships");
                entity.HasKey(membership => new { membership.StudentGroupId, membership.StudentId });
                entity.Property(membership => membership.StudentId).HasColumnName("student_id");
                entity.Property(membership => membership.StudentGroupId).HasColumnName("student_group_id").IsRequired();
                entity.Property(membership => membership.JoinedAt).HasColumnName("joined_at").HasColumnType("datetime(6)").IsRequired();
                entity.HasIndex(membership => membership.StudentGroupId);
                entity.HasIndex(membership => membership.StudentId);
                entity.HasOne(membership => membership.Student).WithMany().HasForeignKey(membership => membership.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(membership => membership.Group).WithMany(group => group.Memberships)
                    .HasForeignKey(membership => membership.StudentGroupId).OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WorkSchedule>(entity =>
            {
                entity.ToTable("work_schedules", table =>
                {
                    table.HasCheckConstraint("ck_work_schedules_target", "(`target_type` = 'STUDENT' AND `student_id` IS NOT NULL AND `student_group_id` IS NULL) OR (`target_type` = 'GROUP' AND `student_id` IS NULL AND `student_group_id` IS NOT NULL)");
                    table.HasCheckConstraint("ck_work_schedules_status", "`status` IN ('ACTIVE', 'INACTIVE')");
                    table.HasCheckConstraint("ck_work_schedules_recurrence", "(`effective_start_date` IS NULL AND `recurrence_type` IS NULL AND `recurrence_weeks` IS NULL AND `effective_end_date` IS NULL) OR (`effective_start_date` IS NOT NULL AND `recurrence_type` IS NOT NULL AND ((`recurrence_type` = 'NONE' AND `recurrence_weeks` IS NOT NULL AND `recurrence_weeks` = 1 AND `effective_end_date` IS NOT NULL AND `effective_end_date` >= `effective_start_date`) OR (`recurrence_type` = 'WEEKLY' AND ((`recurrence_weeks` IS NULL AND `effective_end_date` IS NULL) OR (`recurrence_weeks` IS NOT NULL AND `recurrence_weeks` BETWEEN 1 AND 52 AND `effective_end_date` IS NOT NULL AND `effective_end_date` >= `effective_start_date`)))))");
                });
                entity.HasKey(schedule => schedule.Id);
                entity.Property(schedule => schedule.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(schedule => schedule.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
                entity.Property(schedule => schedule.TargetType).HasColumnName("target_type").HasMaxLength(20).IsRequired();
                entity.Property(schedule => schedule.StudentGroupId).HasColumnName("student_group_id");
                entity.Property(schedule => schedule.StudentId).HasColumnName("student_id");
                entity.Property(schedule => schedule.IsFlexible).HasColumnName("is_flexible").IsRequired();
                entity.Property(schedule => schedule.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
                entity.Property(schedule => schedule.EffectiveStartDate).HasColumnName("effective_start_date").HasColumnType("date");
                entity.Property(schedule => schedule.RecurrenceType).HasColumnName("recurrence_type").HasMaxLength(20);
                entity.Property(schedule => schedule.RecurrenceWeeks).HasColumnName("recurrence_weeks");
                entity.Property(schedule => schedule.EffectiveEndDate).HasColumnName("effective_end_date").HasColumnType("date");
                entity.Property(schedule => schedule.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)").IsRequired();
                entity.Property(schedule => schedule.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime(6)").IsRequired();
                entity.HasIndex(schedule => schedule.StudentGroupId);
                entity.HasIndex(schedule => schedule.StudentId);
                entity.HasOne(schedule => schedule.Group).WithMany(group => group.WorkSchedules)
                    .HasForeignKey(schedule => schedule.StudentGroupId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(schedule => schedule.Student).WithMany().HasForeignKey(schedule => schedule.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<WorkScheduleDay>(entity =>
            {
                entity.ToTable("work_schedule_days", table =>
                {
                    table.HasCheckConstraint("ck_work_schedule_days_weekday", "`day_of_week` BETWEEN 0 AND 6");
                    table.HasCheckConstraint("ck_work_schedule_days_time_order", "`start_time` < `end_time`");
                    table.HasCheckConstraint("ck_work_schedule_days_break_pair", "(`break_start` IS NULL AND `break_end` IS NULL) OR (`break_start` IS NOT NULL AND `break_end` IS NOT NULL AND `break_start` < `break_end` AND `break_start` >= `start_time` AND `break_end` <= `end_time`)");
                });
                entity.HasKey(day => day.Id);
                entity.Property(day => day.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(day => day.WorkScheduleId).HasColumnName("work_schedule_id").IsRequired();
                entity.Property(day => day.DayOfWeek).HasColumnName("day_of_week").IsRequired();
                entity.Property(day => day.StartTime).HasColumnName("start_time").HasColumnType("time(6)").IsRequired();
                entity.Property(day => day.EndTime).HasColumnName("end_time").HasColumnType("time(6)").IsRequired();
                entity.Property(day => day.BreakStart).HasColumnName("break_start").HasColumnType("time(6)");
                entity.Property(day => day.BreakEnd).HasColumnName("break_end").HasColumnType("time(6)");
                entity.HasIndex(day => new { day.WorkScheduleId, day.DayOfWeek }).IsUnique();
                entity.HasOne(day => day.WorkSchedule).WithMany(schedule => schedule.Days)
                    .HasForeignKey(day => day.WorkScheduleId).OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<LeaveRequest>(entity =>
            {
                entity.ToTable("leave_requests", table =>
                {
                    table.HasCheckConstraint("ck_leave_requests_date_order", "`end_date` >= `start_date`");
                    table.HasCheckConstraint("ck_leave_requests_status", "`status` IN ('PENDING', 'APPROVED', 'REJECTED')");
                });
                entity.HasKey(request => request.Id);
                entity.Property(request => request.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(request => request.StudentId).HasColumnName("student_id").IsRequired();
                entity.Property(request => request.StartDate).HasColumnName("start_date").HasColumnType("date").IsRequired();
                entity.Property(request => request.EndDate).HasColumnName("end_date").HasColumnType("date").IsRequired();
                entity.Property(request => request.Reason).HasColumnName("reason").HasMaxLength(2000).IsRequired();
                entity.Property(request => request.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
                entity.Property(request => request.ReviewedByUserId).HasColumnName("reviewed_by_user_id");
                entity.Property(request => request.ReviewedAt).HasColumnName("reviewed_at").HasColumnType("datetime(6)");
                entity.Property(request => request.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(1000);
                entity.Property(request => request.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)").IsRequired();
                entity.Property(request => request.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime(6)").IsRequired();
                entity.HasIndex(request => new { request.StudentId, request.Status, request.StartDate, request.EndDate });
                entity.HasIndex(request => request.ReviewedByUserId);
                entity.HasOne(request => request.Student).WithMany().HasForeignKey(request => request.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<User>().WithMany().HasForeignKey(request => request.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<Allowance>(entity =>
            {
                entity.ToTable("allowances", table =>
                {
                    table.HasCheckConstraint("ck_allowances_amount", "`amount` > 0 AND `paid_amount` >= 0 AND `paid_amount` <= `amount`");
                    table.HasCheckConstraint("ck_allowances_period", "`period_end` >= `period_start`");
                    table.HasCheckConstraint("ck_allowances_status", "`status` IN ('ACTIVE', 'CANCELLED')");
                });
                entity.HasKey(allowance => allowance.Id);
                entity.Property(allowance => allowance.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(allowance => allowance.StudentId).HasColumnName("student_id").IsRequired();
                entity.Property(allowance => allowance.AllowanceType).HasColumnName("allowance_type").HasMaxLength(100).IsRequired();
                entity.Property(allowance => allowance.Description).HasColumnName("description").HasMaxLength(1000);
                entity.Property(allowance => allowance.Amount).HasColumnName("amount").HasPrecision(12, 2).IsRequired();
                entity.Property(allowance => allowance.PaidAmount).HasColumnName("paid_amount").HasPrecision(12, 2).HasDefaultValue(0m).IsRequired();
                entity.Property(allowance => allowance.PeriodStart).HasColumnName("period_start").HasColumnType("date").IsRequired();
                entity.Property(allowance => allowance.PeriodEnd).HasColumnName("period_end").HasColumnType("date").IsRequired();
                entity.Property(allowance => allowance.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
                entity.Property(allowance => allowance.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)").IsRequired();
                entity.Property(allowance => allowance.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime(6)").IsRequired();
                entity.HasIndex(allowance => new { allowance.StudentId, allowance.AllowanceType, allowance.PeriodStart, allowance.PeriodEnd }).IsUnique();
                entity.HasIndex(allowance => new { allowance.StudentId, allowance.PeriodStart, allowance.PeriodEnd });
                entity.HasOne(allowance => allowance.Student).WithMany().HasForeignKey(allowance => allowance.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AllowancePayment>(entity =>
            {
                entity.ToTable("allowance_payments", table => table.HasCheckConstraint(
                    "ck_allowance_payments_amount", "`amount` > 0"));
                entity.HasKey(payment => payment.Id);
                entity.Property(payment => payment.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(payment => payment.AllowanceId).HasColumnName("allowance_id").IsRequired();
                entity.Property(payment => payment.Amount).HasColumnName("amount").HasPrecision(12, 2).IsRequired();
                entity.Property(payment => payment.PaidAt).HasColumnName("paid_at").HasColumnType("datetime(6)").IsRequired();
                entity.Property(payment => payment.RecordedByUserId).HasColumnName("recorded_by_user_id").IsRequired();
                entity.Property(payment => payment.Reference).HasColumnName("reference").HasMaxLength(150);
                entity.Property(payment => payment.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)").IsRequired();
                entity.HasIndex(payment => new { payment.AllowanceId, payment.PaidAt });
                entity.HasIndex(payment => payment.RecordedByUserId);
                entity.HasOne(payment => payment.Allowance).WithMany(allowance => allowance.Payments)
                    .HasForeignKey(payment => payment.AllowanceId).OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<User>().WithMany().HasForeignKey(payment => payment.RecordedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<SupportRequest>(entity =>
            {
                entity.ToTable("support_requests", table => table.HasCheckConstraint(
                    "ck_support_requests_status", "`status` IN ('PENDING', 'IN_PROGRESS', 'RESOLVED')"));
                entity.HasKey(request => request.Id);
                entity.Property(request => request.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(request => request.StudentId).HasColumnName("student_id").IsRequired();
                entity.Property(request => request.Subject).HasColumnName("subject").HasMaxLength(160).IsRequired();
                entity.Property(request => request.Description).HasColumnName("description").HasMaxLength(5000).IsRequired();
                entity.Property(request => request.Category).HasColumnName("category").HasMaxLength(80).IsRequired();
                entity.Property(request => request.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
                entity.Property(request => request.Response).HasColumnName("response").HasMaxLength(4000);
                entity.Property(request => request.HandledByUserId).HasColumnName("handled_by_user_id");
                entity.Property(request => request.HandledAt).HasColumnName("handled_at").HasColumnType("datetime(6)");
                entity.Property(request => request.CreatedAt).HasColumnName("created_at").HasColumnType("datetime(6)").IsRequired();
                entity.Property(request => request.UpdatedAt).HasColumnName("updated_at").HasColumnType("datetime(6)").IsRequired();
                entity.HasIndex(request => new { request.Status, request.CreatedAt });
                entity.HasIndex(request => new { request.StudentId, request.CreatedAt });
                entity.HasIndex(request => request.HandledByUserId);
                entity.HasOne(request => request.Student).WithMany().HasForeignKey(request => request.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<User>().WithMany().HasForeignKey(request => request.HandledByUserId)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<StudentProgramAuditEvent>(entity =>
            {
                entity.ToTable("student_program_audit_events", table =>
                {
                    table.HasCheckConstraint(
                        "ck_student_program_audit_events_type",
                        "`event_type` IN ('PARTICIPATION_STATUS_CHANGED', 'COMPLETION_CONFIRMED', 'COMPLETION_REVOKED')");
                    table.HasCheckConstraint(
                        "ck_student_program_audit_events_status",
                        "(`event_type` = 'PARTICIPATION_STATUS_CHANGED' AND `new_status` IS NOT NULL AND `new_status` IN ('UNVERIFIED', 'ACTIVE', 'ENDED', 'CANCELLED') AND (`previous_status` IS NULL OR `previous_status` IN ('UNVERIFIED', 'ACTIVE', 'ENDED', 'CANCELLED'))) OR (`event_type` IN ('COMPLETION_CONFIRMED', 'COMPLETION_REVOKED') AND `previous_status` IS NULL AND `new_status` IS NULL)");
                });
                entity.UseCollation("utf8mb4_unicode_ci");
                entity.HasKey(item => item.Id);
                entity.Property(item => item.Id).HasColumnName("id").ValueGeneratedOnAdd();
                entity.Property(item => item.StudentId).HasColumnName("student_id").IsRequired();
                entity.Property(item => item.ProgramId).HasColumnName("program_id").IsRequired();
                entity.Property(item => item.ActorUserId).HasColumnName("actor_user_id").IsRequired();
                entity.Property(item => item.EventType).HasColumnName("event_type").HasMaxLength(40).HasColumnType("varchar(40)").IsRequired().UseCollation("utf8mb4_unicode_ci");
                entity.Property(item => item.PreviousStatus).HasColumnName("previous_status").HasMaxLength(16).HasColumnType("varchar(16)").UseCollation("utf8mb4_unicode_ci");
                entity.Property(item => item.NewStatus).HasColumnName("new_status").HasMaxLength(16).HasColumnType("varchar(16)").UseCollation("utf8mb4_unicode_ci");
                entity.Property(item => item.Reason).HasColumnName("reason").HasMaxLength(500).HasColumnType("varchar(500)").UseCollation("utf8mb4_unicode_ci");
                entity.Property(item => item.OccurredAtUtc).HasColumnName("occurred_at_utc").HasColumnType("datetime(6)").IsRequired();
                entity.HasIndex(item => new { item.StudentId, item.ProgramId, item.Id })
                    .HasDatabaseName("ix_student_program_audit_events_student_program_id");
                entity.HasIndex(item => new { item.ProgramId, item.Id })
                    .HasDatabaseName("ix_student_program_audit_events_program_id");
                entity.HasOne<Student>().WithMany().HasForeignKey(item => item.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<InternshipProgram>().WithMany().HasForeignKey(item => item.ProgramId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne<User>().WithMany().HasForeignKey(item => item.ActorUserId)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        public override int SaveChanges() => SaveChanges(acceptAllChangesOnSuccess: true);

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            EnsureStudentProgramAuditEventsAppendOnly();
            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(
            bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            EnsureStudentProgramAuditEventsAppendOnly();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            EnsureStudentProgramAuditEventsAppendOnly();

            var entries = ChangeTracker.Entries()
                .Where(e => e.Entity is Role or User or Mentor or Student or Department or InternshipProgram &&
                           (e.State == EntityState.Added || e.State == EntityState.Modified));

            var now = DateTime.UtcNow;

            foreach (var entry in entries)
            {
                if (entry.State == EntityState.Added)
                {
                    if (entry.Property("CreatedAt").CurrentValue == null ||
                        (DateTime)entry.Property("CreatedAt").CurrentValue! == default)
                    {
                        entry.Property("CreatedAt").CurrentValue = now;
                    }
                    entry.Property("UpdatedAt").CurrentValue = now;
                }
                else if (entry.State == EntityState.Modified)
                {
                    entry.Property("UpdatedAt").CurrentValue = now;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }

        private void EnsureStudentProgramAuditEventsAppendOnly()
        {
            if (ChangeTracker.Entries<StudentProgramAuditEvent>()
                .Any(entry => entry.State is EntityState.Modified or EntityState.Deleted))
            {
                throw new InvalidOperationException("Student program audit events are append-only.");
            }
        }
    }
}
