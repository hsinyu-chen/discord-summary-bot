using Microsoft.EntityFrameworkCore;

namespace SummaryAndCheck.Models
{
    public class SummaryAndCheckDbContext(DbContextOptions<SummaryAndCheckDbContext> options) : DbContext(options)
    {
    public DbSet<AppUser> AppUsers { get; set; }
    public DbSet<DiscordUser> DiscordUsers { get; set; }
    public DbSet<SystemConfig> SystemConfigs { get; set; }
    public DbSet<CreditHistory> CreditHistories { get; set; }
    public DbSet<StoredRegistrationOptions> StoredRegistrationOptions { get; set; }
    public DbSet<StoredAuthenticationOptions> StoredAuthenticationOptions { get; set; }
    public DbSet<PasskeyStorage> PasskeyStorage { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DiscordUser>().ToTable("DiscordUser");
            modelBuilder.Entity<SystemConfig>().ToTable("SystemConfig");
            modelBuilder.Entity<DiscordUserAccountBinding>().ToTable("DiscordUserAccountBinding");

            modelBuilder.Entity<CreditHistory>(entity =>
            {
                entity.ToTable("CreditHistory");
                entity.HasDiscriminator(x => x.HistoryType)
                      .HasValue<CreditTopUpHistory>(CreditHistoryType.CreditTopUp)
                      .HasValue<CreditUseHistory>(CreditHistoryType.CreditUse)
                      .HasValue<CreditAdminHistory>(CreditHistoryType.AdminAdjustment);

                entity.HasOne(ch => ch.DiscordUser)
                      .WithMany(du => du.CreditHistories)
                      .HasForeignKey(ch => ch.DiscordUserId)
                      .IsRequired();

                entity.HasIndex(ch => ch.DiscordUserId); 
            });

            modelBuilder.Entity<CreditTopUpHistory>(entity =>
            {
                entity.HasOne(cth => cth.AccountBinding)
                      .WithMany(duab => duab.CreditTopUpHistories)
                      .HasForeignKey(cth => cth.AccountBindingId)
                      .IsRequired();

                entity.HasIndex(cth => cth.AccountBindingId);
            });

            modelBuilder.Entity<DiscordUser>(entity =>
            {
                entity.Property(u => u.Id)
                      .ValueGeneratedNever();
            });

            modelBuilder.Entity<SystemConfig>();
            modelBuilder.Entity<DiscordUserAccountBinding>();

            base.OnModelCreating(modelBuilder);
        }
    }
}