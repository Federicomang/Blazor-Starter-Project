using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StarterProject.Database.Entities;
using StarterProject.Database.Entities.OpenIddict;

namespace StarterProject.Database
{
    public class ApplicationDbContext(DbContextOptions options) : IdentityDbContext<User, IdentityRole, string>(options)
    {
        private const string IDENTITY_SCHEMA = "identity";

        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<AuditLogDetail> AuditLogDetails { get; set; }
        public DbSet<Identifier> Identifiers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(entity =>
            {
                // Relazione con IdentityUserRole (tabella di join)
                entity.HasMany(e => e.UserRoles)
                    .WithOne()
                    .HasForeignKey(ur => ur.UserId)
                    .IsRequired()
                    .OnDelete(DeleteBehavior.Cascade);

                // Relazione many-to-many con IdentityRole
                entity.HasMany(e => e.Roles)
                    .WithMany()
                    .UsingEntity<IdentityUserRole<string>>(
                        userRole => userRole.HasOne<IdentityRole>()
                            .WithMany()
                            .HasForeignKey(ur => ur.RoleId)
                            .OnDelete(DeleteBehavior.Cascade),
                        userRole => userRole.HasOne<User>()
                            .WithMany(u => u.UserRoles)
                            .HasForeignKey(ur => ur.UserId)
                            .OnDelete(DeleteBehavior.Cascade)
                    );
                entity.ToTable("AspNetUsers", IDENTITY_SCHEMA);
            });
            builder.Entity<IdentityRole>().ToTable("AspNetRoles", IDENTITY_SCHEMA);
            builder.Entity<IdentityUserClaim<string>>().ToTable("AspNetUserClaims", IDENTITY_SCHEMA);
            builder.Entity<IdentityUserRole<string>>().ToTable("AspNetUserRoles", IDENTITY_SCHEMA);
            builder.Entity<IdentityUserLogin<string>>().ToTable("AspNetUserLogins", IDENTITY_SCHEMA);
            builder.Entity<IdentityRoleClaim<string>>().ToTable("AspNetRoleClaims", IDENTITY_SCHEMA);
            builder.Entity<IdentityUserToken<string>>().ToTable("AspNetUserTokens", IDENTITY_SCHEMA);
            builder.Entity<OpenIddictApplication>().ToTable("OpenIddictApplications", IDENTITY_SCHEMA);
            builder.Entity<OpenIddictAuthorization>().ToTable("OpenIddictAuthorizations", IDENTITY_SCHEMA);
            builder.Entity<OpenIddictScope>().ToTable("OpenIddictScopes", IDENTITY_SCHEMA);
            builder.Entity<OpenIddictToken>().ToTable("OpenIddictTokens", IDENTITY_SCHEMA);
            builder.Entity<Identifier>().ToTable(nameof(Identifiers), IDENTITY_SCHEMA);
        }
    }
}
