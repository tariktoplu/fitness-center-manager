using Web.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Web.Data;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, int>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // --- İŞTE EKSİK OLAN DBSET TANIMLARI ---
    public DbSet<GymBranch> GymBranches { get; set; }
    public DbSet<GymWorkingHour> GymWorkingHours { get; set; }
    public DbSet<ServiceCategory> ServiceCategories { get; set; }
    public DbSet<Service> Services { get; set; }
    public DbSet<GymService> GymServices { get; set; }
    public DbSet<Specialization> Specializations { get; set; }
    public DbSet<Trainer> Trainers { get; set; }
    public DbSet<TrainerService> TrainerServices { get; set; }
    public DbSet<TrainerSpecialization> TrainerSpecializations { get; set; }
    public DbSet<TrainerAvailability> TrainerAvailabilities { get; set; }
    public DbSet<Member> Members { get; set; }
    public DbSet<MembershipType> MembershipTypes { get; set; }
    public DbSet<MembershipPlan> MembershipPlans { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<BodyMeasurement> BodyMeasurements { get; set; }
    public DbSet<AIRecommendation> AIRecommendations { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Tablo İsimlendirmeleri
        builder.Entity<AppUser>().ToTable("Users");
        builder.Entity<AppRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<int>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<int>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<int>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<int>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<int>>().ToTable("UserTokens");

        // İlişkiler ve Kısıtlamalar
        builder.Entity<GymService>().HasIndex(x => new { x.GymBranchId, x.ServiceId }).IsUnique();
        builder.Entity<GymWorkingHour>().HasIndex(x => new { x.GymBranchId, x.DayOfWeek }).IsUnique();
        builder.Entity<TrainerService>().HasIndex(x => new { x.TrainerId, x.ServiceId }).IsUnique();
        builder.Entity<TrainerSpecialization>().HasIndex(x => new { x.TrainerId, x.SpecializationId }).IsUnique();

        builder.Entity<Trainer>()
            .HasOne(t => t.User).WithOne(u => u.TrainerProfile)
            .HasForeignKey<Trainer>(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Member>()
            .HasOne(m => m.User).WithOne(u => u.MemberProfile)
            .HasForeignKey<Member>(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // PostgreSQL için BMI Hesaplama Formülü
        builder.Entity<BodyMeasurement>()
            .Property(p => p.BMI)
            .HasComputedColumnSql("\"Weight\" / ((\"Height\" / 100.0) * (\"Height\" / 100.0))", stored: true);

        // Check Constraints
        builder.Entity<TrainerAvailability>(e => e.HasCheckConstraint("CHK_TrainerAvail_Time", "\"EndTime\" > \"StartTime\""));
        builder.Entity<Appointment>(e => e.HasCheckConstraint("CHK_Appointments_Time", "\"EndTime\" > \"StartTime\""));
        builder.Entity<MembershipPlan>(e => e.HasCheckConstraint("CHK_MembershipPlans_Date", "\"EndDate\" >= \"StartDate\""));
        
        // Decimal Ayarları
        var decimalProps = builder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?));

        foreach (var property in decimalProps)
        {
            property.SetPrecision(10);
            property.SetScale(2);
        }
    }
}