using Casino.Application.Application;
using Casino.Application.Interfaces;
using Casino.Application.Services;
using Casino.Infrastructure.Data;
using Casino.Infrastructure.Data.Repositories;
using Casino.Infrastructure.External;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("Casino.Infrastructure.Data")));

// (Opcional por ahora) Soporte para OpenAPI/Swagger
builder.Services.AddOpenApi();
builder.Services.AddControllers();

builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<IRenaperService, RenaperService>(); // A implementar realmente cuando se realize la API
builder.Services.AddScoped<IJwtProvider, JwtProvider>();

builder.Services.AddScoped<RegisterUserService>();
builder.Services.AddScoped<LoginUserService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        db.Database.Migrate();
        Console.WriteLine("Base de datos conectada y migrada con éxito.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Ocurrió un error al migrar la BD: {ex.Message}");
    }
}


if (app.Environment.IsDevelopment())
{
    //app.MapOpenApi(); // Para poder ver los endpoints después
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapControllers();
app.Run();