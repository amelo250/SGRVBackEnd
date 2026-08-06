using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Microsoft.AspNetCore.Identity;
using SGRVBackEnd.Models.Usuarios;

using SGRVBackEnd.Data;
using System.Text;
using SGRVBackEnd.Services.Reservations;
using SGRVBackEnd.Mappings;
using SGRVBackEnd.Services.Gastos;
using SGRVBackEnd.Services.Vehiculos;
using SGRVBackEnd.Services.Accesorios;
using SGRVBackEnd.Services.FotosVehiculo;
using SGRVBackEnd.Services.Configuracion;

var builder = WebApplication.CreateBuilder(args);

// Controladores
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();
builder.Services.AddControllers();
builder.Services.AddAutoMapper(_ => { }, typeof(RentaProfile));
builder.Services.AddScoped<IReservationAvailabilityService, ReservationAvailabilityService>();
builder.Services.AddScoped<IGastoService, GastoService>();
builder.Services.AddScoped<IVehiculoResumenService, VehiculoResumenService>();
builder.Services.AddScoped<IAccesorioService, AccesorioService>();
builder.Services.AddScoped<IFotoVehiculoService, FotoVehiculoService>();
builder.Services.AddScoped<IConfiguracionCatalogoService, ConfiguracionCatalogoService>();

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

const string FrontendCorsPolicy = "FrontendCorsPolicy";

builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5000",
                "http://localhost:8080",
                "http://localhost:3000"
            )
            .AllowAnyHeader()
            .AllowAnyMethod().WithExposedHeaders(
    "X-Total-Count",
    "X-Page-Number",
    "X-Page-Size");
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


if (!app.Environment.IsDevelopment())
{

    app.UseHttpsRedirection();
}

app.UseCors(FrontendCorsPolicy);

// El orden es importante.
app.UseMiddleware<SGRVBackEnd.Middleware.ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
