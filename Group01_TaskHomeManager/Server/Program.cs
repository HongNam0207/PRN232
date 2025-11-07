using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text;
using Server.Models;

namespace Server // ✅ Thêm namespace bọc toàn bộ
{
    // ============================================================
    // 🔹 1️⃣ Class filter
    // ============================================================
    public class ODataQuerySwaggerFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Info.Description +=
                "\n\n📘 **OData supported:** `$filter`, `$orderby`, `$select`, `$top`, `$skip`, `$count`" +
                "\n\n🧩 Examples:" +
                "\n- `/api/Families?$orderby=CreatedAt desc`" +
                "\n- `/api/Families?$filter=Address eq 'Hanoi'`" +
                "\n- `/api/Tasks?$filter=Status eq 'Pending'`";
        }
    }

    // ============================================================
    // 🔹 2️⃣ Bắt đầu cấu hình app
    // ============================================================
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Controllers + OData
            builder.Services.AddControllers()
                .AddOData(opt => opt.Select().Filter().OrderBy().Expand().Count().SetMaxTop(100));

            // Database
            builder.Services.AddDbContext<HomeTaskManagementDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("MyCnn")));

            // CORS
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            });

            // JWT
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "DefaultSecretKey"))
                    };
                });

            builder.Services.AddAuthorization();

            // Swagger + OData info
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "🏠 HomeTaskManagement API",
                    Version = "v1",
                    Description = "API for family task management (OData + JWT + DTO)"
                });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "Nhập đầy đủ 'Bearer {token}' vào đây (VD: Bearer eyJhbGciOi...)",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey, // ✅ đổi từ Http → ApiKey
                    Scheme = "Bearer"
                });


                c.AddSecurityRequirement(new OpenApiSecurityRequirement
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

                c.DocumentFilter<ODataQuerySwaggerFilter>();
            });

            // ============================================================
            // 🔹 Build App
            // ============================================================
            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors("AllowAll");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.Run();
        }
    }
}
