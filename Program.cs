using TrackLink.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSingleton<TrackingService>();
var app = builder.Build();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();