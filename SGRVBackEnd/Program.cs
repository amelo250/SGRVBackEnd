using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

using SGRVBackEnd.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Controladores
builder.Services.AddControllers();

// Entity Framework Core + SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
?? throw new InvalidOperationException(
    "No se encontró la cadena de conexión 'DefaultConnection' en la configuración."
);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Configuración JWT
var jwtSettings = builder.Configuration.GetSection("Jwt");

var jwtKey = jwtSettings["Key"]
    ?? throw new InvalidOperationException(
        "No se encontró la configuración Jwt:Key."
    );

var key = Encoding.UTF8.GetBytes(jwtKey);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(key),

            // Evita aceptar tokens vencidos durante minutos adicionales.
            ClockSkew = TimeSpan.Zero
        };
    });

// Autorización
builder.Services.AddAuthorization();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Introduce solamente el token JWT.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document =>
        new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = []
        });
});

var app = builder.Build();

// Swagger en desarrollo
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "SGRV API v1"
        );
    });
}

app.UseHttpsRedirection();

// El orden es importante.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
