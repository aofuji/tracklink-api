using Microsoft.EntityFrameworkCore;
using TrackLink.Services;
using TrackLink.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    ));    

builder.Services.AddScoped<TrackingService>();
var app = builder.Build();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();