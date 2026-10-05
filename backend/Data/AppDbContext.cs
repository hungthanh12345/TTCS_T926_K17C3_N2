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
        public DbSet<InternshipTask> Tasks => Set<InternshipTask>();
        public DbSet<WeeklyReport> WeeklyReports => Set<WeeklyReport>();
        public DbSet<MentorFeedback> MentorFeedbacks => Set<MentorFeedback>();
        public DbSet<InternshipEvaluation> InternshipEvaluations => Set<InternshipEvaluation>();
        public DbSet<UserNotification> Notifications => Set<UserNotification>();

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
                entity.Property(e => e.MentorId).HasColumnName("mentor_id");
                entity.Property(e => e.ProgramId).HasColumnName("program_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAdd();
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp").HasDefaultValueSql("CURRENT_TIMESTAMP").ValueGeneratedOnAddOrUpdate();

                entity.HasIndex(e => e.StudentCode).IsUnique();
                entity.HasIndex(e => e.UserId).IsUnique();
                entity.HasIndex(e => e.University);
                entity.HasIndex(e => e.Major);
                entity.HasIndex(e => e.MentorId);
                entity.HasIndex(e => e.ProgramId);

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
                entity.HasIndex(e => new { e.StudentId, e.UploadedAt });
                entity.HasOne(e => e.Student)
                    .WithMany()
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Cascade);
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
                    .OnDelete(DeleteBehavior.Cascade);
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
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
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
    }
}
