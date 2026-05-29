using MiddleMan.Infrastructure;
using MiddleMan.Services.JsonProvider;
using MiddleMan.Services.Booking;
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddScoped<IJsonProviderService, BookingJsonProviderService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddControllers((options) =>
{
    options.Conventions.Add(new GlobalRoutePrefixConvention("api"));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapGet("/health", () => Results.Ok("Healthy"));
app.MapControllers();

app.Run();
