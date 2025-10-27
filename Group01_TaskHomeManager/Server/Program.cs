using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Microsoft.OData.ModelBuilder;
using Microsoft.AspNetCore.OData;
using AutoMapper;
using Server.Models; // Chứa các entity được scaffold

var builder = WebApplication.CreateBuilder(args);

// ==============================
// 1️⃣ Add services to container
// ==============================

// Add Controllers + OData support
builder.Services.AddControllers()
    .AddOData(options =>
    {
        options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(100);
    });

// Add EF Core (SQL Server)
builder.Services.AddDbContext<HomeTaskManagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

// Add AutoMapper (map DTO ↔ Entity)
builder.Services.AddAutoMapper(typeof(Program));

// Add Swagger (with JWT Authorization)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Home Task Management API",
        Version = "v1",
        Description = "API for managing users, families, and tasks."
    });

    // Add JWT to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer {token}'"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ==============================
// 2️⃣ Configure Authentication & Authorization (JWT)
// ==============================
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // 🔹 Tạo cấu hình JWT cơ bản
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,   // có thể bật true khi có Issuer
            ValidateAudience = false, // có thể bật true khi có Audience
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes("ThisIsASecretKeyForJwtAuthentication123!")) // khóa bí mật
        };
    });

builder.Services.AddAuthorization();

// ==============================
// 3️⃣ Build app
// ==============================
var app = builder.Build();

// ==============================
// 4️⃣ Middleware pipeline
// ==============================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Authentication & Authorization middlewares
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
