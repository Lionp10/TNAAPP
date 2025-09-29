using Microsoft.EntityFrameworkCore;
using TNA.DAL.Entities;

namespace TNA.DAL.DbContext
{
    public class TNADbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public TNADbContext(DbContextOptions<TNADbContext> options) : base(options) { }

        public DbSet<Clan> Clans { get; set; } = null!;
        public DbSet<ClanMember> ClanMembers { get; set; } = null!;
        public DbSet<ClanMemberSocialMedia> ClanMemberSocialMedias { get; set; } = null!;
        public DbSet<Match> Matches { get; set; } = null!;
        public DbSet<PlayerMatch> PlayerMatches { get; set; } = null!;
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Role> Roles { get; set; } = null!;
        public DbSet<PlayerLifetime> PlayerLifetimes { get; set; } = null!;
        public DbSet<RecentGamesStats> RecentGamesStats { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Clan>(b =>
            {
                b.Property(p => p.ClanId).HasMaxLength(128).IsRequired();
                b.Property(p => p.ClanName).HasMaxLength(50).IsRequired();
                b.Property(p => p.ClanTag).HasMaxLength(10).IsRequired();
                b.Property(p => p.DateOfUpdate).IsRequired();
                b.Property(p => p.Enabled).IsRequired();
            });

            modelBuilder.Entity<ClanMember>(b =>
            {
                b.Property(p => p.Nickname).HasMaxLength(50).IsRequired();
                b.Property(p => p.PlayerId).HasMaxLength(128).IsRequired();
                b.Property(p => p.ClanId).HasMaxLength(128).IsRequired();
                b.Property(p => p.ProfileImage).HasColumnType("nvarchar(max)");
            });

            modelBuilder.Entity<ClanMemberSocialMedia>(b =>
            {
                b.Property(p => p.SocialMediaId).HasColumnType("char(2)").IsRequired();
                b.Property(p => p.SocialMediaUrl).IsRequired();
                b.Property(p => p.Enabled).IsRequired();
            });

            modelBuilder.Entity<PlayerMatch>(b =>
            {
                b.Property(p => p.DamageDealt).HasColumnType("decimal(18,2)").IsRequired();
                b.Property(p => p.TimeSurvived).HasColumnType("decimal(18,2)").IsRequired();
                b.Property(p => p.MatchCreatedAt).HasMaxLength(50).IsRequired();
            });

            modelBuilder.Entity<Role>(b =>
            {
                b.Property(p => p.Description).IsRequired();
            });

            modelBuilder.Entity<User>(b =>
            {
                b.Property(p => p.Nickname).HasMaxLength(50).IsRequired();
                b.Property(p => p.Email).HasMaxLength(50).IsRequired();
                b.Property(p => p.PasswordHash).HasColumnType("nvarchar(max)").IsRequired();
                b.Property(p => p.MemberId).IsRequired(false);
                b.Property(p => p.CreatedAt).IsRequired(false);
                b.Property(p => p.Enabled).IsRequired();
                b.HasIndex(p => p.Email).IsUnique();

                b.HasOne(u => u.Role)
                 .WithMany()
                 .HasForeignKey(u => u.RoleId)
                 .OnDelete(DeleteBehavior.Restrict);

                b.HasOne(u => u.Member)
                 .WithMany()
                 .HasForeignKey(u => u.MemberId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<PlayerLifetime>(b =>
            {
                b.Property(p => p.PlayerId).HasMaxLength(128).IsRequired();
                b.Property(p => p.DateOfUpdate).IsRequired();
                b.Property(p => p.LifetimeJson).HasColumnType("nvarchar(max)");
                b.HasIndex(p => p.PlayerId);
            });

            modelBuilder.Entity<RecentGamesStats>(b =>
            {
                b.Property(p => p.PlayerId).HasMaxLength(128).IsRequired();
                b.Property(p => p.DateOfUpdate).IsRequired();

                b.Property(p => p.MatchId).HasMaxLength(128).IsRequired(false);
                b.Property(p => p.CreatedAt).HasMaxLength(50).IsRequired(false);
                b.Property(p => p.MapName).HasMaxLength(50).IsRequired(false);
                b.Property(p => p.GameMode).HasMaxLength(50).IsRequired(false);
                b.Property(p => p.IsCustomMatch).IsRequired();

                b.Property(p => p.DamageDealt).HasColumnType("decimal(18,2)").IsRequired();
                b.Property(p => p.LongestKill).HasColumnType("decimal(18,2)").IsRequired();
                b.Property(p => p.RideDistance).HasColumnType("decimal(18,2)").IsRequired();
                b.Property(p => p.SwimDistance).HasColumnType("decimal(18,2)").IsRequired();
                b.Property(p => p.WalkDistance).HasColumnType("decimal(18,2)").IsRequired();
                b.Property(p => p.TimeSurvived).HasColumnType("decimal(18,2)").IsRequired();

                b.HasIndex(p => p.PlayerId);
            });
        }
    }
}
