using Microsoft.EntityFrameworkCore;
using TrackLink.Models;

namespace TrackLink.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Tracking> Trackings { get; set; }
}