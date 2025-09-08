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
                b.Property(p => p.Email).HasMaxLength(50);
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

            // Ajusta relaciones/índices según necesites.
        }
    }
}
