
using Domain.Contract;
using Domain.Entities;
using LegalAI.Factories;
using LegalAI.MiddleWares;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Persistence.Data;
using Persistence.Repositories;
using Service.Abstraction.Contracts;
using Service.Implementations;
using Service.MappingProfiles;
using Service.PictureUrlResolver;
using Shared;
using Shared.Settings;
using StackExchange.Redis;
using System.Text;

namespace LegalAI {
    public class Program {
        public static async Task Main(string[] args) {
            var builder = WebApplication.CreateBuilder(args);

            #region ===== Add Services To The Container =====
            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            // Add Database Connection String =============
            builder.Services.AddDbContext<AppDbContext>(options => {
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnections"));
            });

            // Add Redis Connection Multiplexer To The Services
            builder.Services.AddSingleton<IConnectionMultiplexer>(
                _ => ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!)
            );

            // Add DI For DbInitilazer
            builder.Services.AddScoped<IDbInitilazer, DbInitilazer>();

            // Add DI For UnitOfWork
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

            //Add DI To ServiceManger =============
            builder.Services.AddScoped<IServiceManger, ServiceManger>();

            // Add DI To Attachment Service =============
            builder.Services.AddScoped<IAttachmentService, AttachmentService>();

            // Add DI To Cache Service =============
            builder.Services.AddScoped<ICacheService, CacheService>();

            // Add DI To Attachment Service =============
            builder.Services.AddScoped<IEmailService, EmailService>();

            // Add DI To Attachment Service =============
            builder.Services.AddScoped<ICacheRepository, CacheRepository>();

            // Add DI To Password Service =============
            builder.Services.AddScoped<IPasswordService, PasswordService>();

            // Add DI For File Storage Service
            builder.Services.AddScoped<IFileStorageService, FileStorageService>();

            // Add DI To Picture URL Resolver =============
            builder.Services.AddScoped<UserPictureUrlResolver>();

            // Add AutoMapper Profiles To Services =============
            builder.Services.AddAutoMapper(M => M.AddProfile(new UserProfile()));

            // Configure FileStorage Section In appsetting.json
            builder.Services.Configure<FileStorageSettings>(builder.Configuration.GetSection("FileStorage"));

            // Add Service For Bad Request Erorr Handling =============
            builder.Services.Configure<ApiBehaviorOptions>(options => {
                options.InvalidModelStateResponseFactory = ApiResponseFactory.CustomValidationErrorResponse;
            });

            // Add AI Service
            builder.Services.Configure<LegalAiOptions>(builder.Configuration.GetSection("LegalAiService"));

            builder.Services.AddHttpClient<ILegalAiService, LegalAiService>((sp, client) => {
                var options = sp.GetRequiredService<IOptions<LegalAiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            })
                .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler {
                    RequestHeaderEncodingSelector = (headerName, request) => Encoding.UTF8
                });

            // Configure The Identity Rules And Password =============
            builder.Services.AddIdentity<ApplicationUser, IdentityRole<int>>(options => {
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 8;
            }).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

            // Register JwtOptions in DI Container =============
            builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("JwtOptions"));

            var jwtOptions = builder.Configuration.GetSection("JwtOptions").Get<JwtOptions>();

            builder.Services.AddAuthentication(options => {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                // LOG OR NOT
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            }).AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,

                    ValidAudience = jwtOptions!.Audience,
                    ValidIssuer = jwtOptions.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),

                    ClockSkew = TimeSpan.Zero
                };

            });

            // Add Policies =============
            builder.Services.AddAuthorization(options => {
                // Add Policy For Admin Actions ============= 
                options.AddPolicy("SystemAdministrator", policy =>
                    policy.RequireRole("System Administrator"));

                // Add Policy For Legal Manger Actions =============
                options.AddPolicy("LegalManager", policy =>
                    policy.RequireRole("Legal Manager"));

                // Add Policy For Legal Advisor Actions =============
                options.AddPolicy("LegalAdvisor", policy =>
                    policy.RequireRole("Legal Advisor"));

                // Add Policy For Legal Advisor And Legal Manger Actions =============
                options.AddPolicy("AdvisorOrManger", policy =>
                    policy.RequireRole("Legal Advisor", "Legal Manager"));

                // Add Policy For Legal Advisor And Legal Manger Actions =============
                options.AddPolicy("LawyerOrAdvisorOrManger", policy =>
                    policy.RequireRole("Legal Advisor", "Legal Manager", "Lawyer"));

                // Add Policy For Legal Advisor And Lawyer Actions =============
                options.AddPolicy("AdvisorOrLawyer", policy =>
                    policy.RequireRole("Legal Advisor", "Lawyer"));
            });


            Console.OutputEncoding = System.Text.Encoding.UTF8;
            #endregion

            var app = builder.Build();

            #region ===== Kestrel MiddelWare =====
            // Add Custom MiddelWare
            app.UseMiddleware<GlobalErrorHandelingMiddleware>();

            // Initilaize Database
            using (var scope = app.Services.CreateScope()) {
                var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitilazer>();
                await dbInitializer.InitilazeAsync();
                await dbInitializer.InitilazeIdentityAsync();
            }

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment()) {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseStaticFiles();

            app.UseHttpsRedirection();

            app.UseAuthentication();

            app.UseAuthorization();

            app.MapControllers(); 
            #endregion

            app.Run();
        }
    }
}
