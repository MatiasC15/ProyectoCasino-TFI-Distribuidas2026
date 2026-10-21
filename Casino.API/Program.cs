using Casino.Application.Application;
using Casino.Application.Interfaces;
using Casino.Application.Services;
using Casino.Infrastructure.Data;
using Casino.Infrastructure.Data.Repositories;
using Casino.Infrastructure.External;
using Casino.API.Realtime;
using Casino.API.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        b => b.MigrationsAssembly("Casino.Infrastructure.Data")
    )
);
builder.Services.AddSignalR();
var frontendOrigins = builder.Configuration.GetSection("Frontend:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    if (frontendOrigins.Length > 0)
        policy.WithOrigins(frontendOrigins);

    policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));

// 2. Controladores
builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("admin", "superadmin"));
    options.AddPolicy("SuperAdminOnly", policy => policy.RequireRole("superadmin"));
});

// 3. Inyección de Dependencias
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IRenaperService, RenaperService>();
builder.Services.AddScoped<IJwtProvider, JwtProvider>();
builder.Services.AddScoped<RegisterUserService>();
builder.Services.AddScoped<LoginUserService>();
builder.Services.AddScoped<IWalletService, WalletService>();
builder.Services.AddScoped<DepositService>();
builder.Services.AddScoped<WithdrawService>();
builder.Services.AddScoped<IRouletteService, RouletteService>();
builder.Services.AddSingleton<IRouletteEventStream, RouletteEventStream>();
builder.Services.AddHostedService<RouletteRoundsWorker>();
builder.Services.AddHostedService<RouletteRealtimeWorker>();

//  Configuración de Autenticación JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        RoleClaimType = "rol",

        ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
        ValidAudience = builder.Configuration["JwtSettings:Audience"],

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(
                builder.Configuration["JwtSettings:Secret"]!
            )
        )
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs/roulette"))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});

// Configuración de Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Autorización JWT. Pegá tu token acá directamente.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

var app = builder.Build();

// Migraciones Automáticas
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();

        await db.Database.MigrateAsync();
        await InitialSuperAdminBootstrapper.EnsureCreatedAsync(
            db, builder.Configuration, services.GetRequiredService<ILoggerFactory>().CreateLogger("AdminBootstrap"));

        Console.WriteLine(
            "Base de datos conectada y migrada con éxito."
        );
    }
    catch (Exception ex)
    {
        Console.WriteLine(
            $"Ocurrió un error al migrar la BD: {ex.Message}"
        );
        throw;
    }
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<RouletteHub>("/hubs/roulette");

app.Run();
