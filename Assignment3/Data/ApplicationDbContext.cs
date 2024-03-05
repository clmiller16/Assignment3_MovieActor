using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Assignment3.Models;

namespace Assignment3.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Assignment3.Models.Movie> Movie { get; set; } = default!;
        public DbSet<Assignment3.Models.Actor> Actor { get; set; } = default!;
        public DbSet<Assignment3.Models.ActorMovie> ActorMovie { get; set; } = default!;
    }
}
