using Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Concrete
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<TimeCapsuleMessage> TimeCapsuleMessages { get; set; }
    }
}
