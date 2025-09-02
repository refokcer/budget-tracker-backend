using budget_tracker_backend.Data;
using budget_tracker_backend.Services.Accounts;
using budget_tracker_backend.Services.BudgetPlans;
using budget_tracker_backend.Services.BudgetPlanItems;
using budget_tracker_backend.Services.Categories;
using budget_tracker_backend.Services.Components;
using budget_tracker_backend.Services.Currencies;
using budget_tracker_backend.Services.Events;
using budget_tracker_backend.Services.Pages;
using budget_tracker_backend.Services.Transactions;
using budget_tracker_backend.Services.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using Microsoft.OpenApi.Models;
using System.Text;
using MediatR;

var builder = WebApplication.CreateBuilder(args);
var currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();


builder.Services.AddScoped<IAccountManager, AccountManager>();
builder.Services.AddScoped<IBudgetPlanManager, BudgetPlanManager>();
builder.Services.AddScoped<IBudgetPlanItemManager, BudgetPlanItemManager>();
builder.Services.AddScoped<ICategoryManager, CategoryManager>();
builder.Services.AddScoped<IComponentManager, ComponentManager>();
builder.Services.AddScoped<ICurrencyManager, CurrencyManager>();
builder.Services.AddScoped<IEventManager, EventManager>();
builder.Services.AddScoped<ITransactionManager, TransactionManager>();
builder.Services.AddScoped<IPageManager, PageManager>();
builder.Services.AddScoped<ITokenService, TokenService>();
// Ïîäêëþ÷àåì EF Core è MS SQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IApplicationDbContext>(sp =>
    sp.GetRequiredService<ApplicationDbContext>());
builder.Services.AddHttpContextAccessor();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!))
    };
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Budget Tracker API",
        Version = "v1"
    });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Input your JWT to access this API"
    };

    options.AddSecurityDefinition("Bearer", securityScheme);

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

builder.Services.AddMediatR(currentAssemblies);
builder.Services.AddAutoMapper(currentAssemblies);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",
        builder => builder.WithOrigins("http://localhost:3000") 
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.UseCors("AllowReact"); // Âêëþ÷àåì CORS

app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        context.Response.StatusCode = 500;
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (exception != null)
        {
            Console.WriteLine($"Îøèáêà íà ñåðâåðå: {exception.Message}");
            Console.WriteLine($"StackTrace: {exception.StackTrace}");
        }
        await context.Response.WriteAsync("An unexpected error occurred.");
    });
});

app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    if (response.StatusCode == 401)
    {
        response.ContentType = "application/json";
        await response.WriteAsJsonAsync(new { status = 401, error = "Unauthorized" });
    }
    else if (response.StatusCode == 403)
    {
        response.ContentType = "application/json";
        await response.WriteAsJsonAsync(new { status = 403, error = "Forbidden" });
    }
});

app.Run();
