using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MotoRentalApi.Entities;

namespace MotoRentalApi.Data
{
    public class ApiDbContext : IdentityDbContext
    {
        public ApiDbContext(DbContextOptions<ApiDbContext> options) : base (options)
        {
            
        }

        public DbSet<Moto> Motos { get; set; }
        public DbSet<Deliverer> Deliverers { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<Deliverer>().ToTable("Deliverers");

            builder.Entity<Deliverer>().HasBaseType<IdentityUser>();

            builder.Entity<Deliverer>()
                .HasIndex(d => d.CNPJ)
                .IsUnique();

            builder.Entity<Deliverer>()
                .HasIndex(d => d.DriverLicenseNumber)
                .IsUnique();

            builder.Entity<Deliverer>()
                .HasOne<IdentityUser>()
                .WithOne()
                .HasForeignKey<Deliverer>(d => d.Id)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
