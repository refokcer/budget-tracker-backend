using System.Text;
using budget_tracker_backend.Data;
using budget_tracker_backend.Exceptions;
using budget_tracker_backend.Models;
using budget_tracker_backend.Services.Accounts;
using budget_tracker_backend.Services.AdminData;
using budget_tracker_backend.Services.Algorithms.Analytics;
using budget_tracker_backend.Services.Algorithms.BudgetPlanning;
using budget_tracker_backend.Services.Algorithms.FinancialGoals;
using budget_tracker_backend.Services.Auth;
using budget_tracker_backend.Services.BudgetPlanItems;
using budget_tracker_backend.Services.BudgetPlans;
using budget_tracker_backend.Services.Categories;
using budget_tracker_backend.Services.ChatGpt;
using budget_tracker_backend.Services.Components;
using budget_tracker_backend.Services.Currencies;
using budget_tracker_backend.Services.FinancialGoals;
using budget_tracker_backend.Services.Pages;
using budget_tracker_backend.Services.Transactions;
using budget_tracker_backend.Services.UserSettings;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddUserSecrets<Program>(optional: true);
var currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();

builder.Services.AddScoped<IAccountManager, AccountManager>();
builder.Services.AddScoped<IBudgetPlanManager, BudgetPlanManager>();
builder.Services.AddScoped<IBudgetPlanItemManager, BudgetPlanItemManager>();
builder.Services.AddScoped<ICategoryManager, CategoryManager>();
builder.Services.AddScoped<IComponentManager, ComponentManager>();
builder.Services.AddScoped<ICurrencyManager, CurrencyManager>();
builder.Services.AddScoped<ITransactionManager, TransactionManager>();
builder.Services.AddScoped<IPageManager, PageManager>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IFinancialGoalManager, FinancialGoalManager>();
builder.Services.AddScoped<IUserSettingsManager, UserSettingsManager>();
builder.Services.AddScoped<IAdminDataManager, AdminDataManager>();
builder.Services.AddScoped<IAdminDataTemplateProvider, AdminDataTemplateProvider>();
builder.Services.AddScoped<IAdminDataSampleBuilder, AdminDataSampleBuilder>();
builder.Services.AddScoped<IAutoBudgetPlanAlgorithm, AutoBudgetPlanAlgorithm>();
builder.Services.AddScoped<IFinancialStabilityAlgorithm, FinancialStabilityAlgorithm>();
builder.Services.AddScoped<IBehavioralScoreAlgorithm, BehavioralScoreAlgorithm>();
builder.Services.AddScoped<IFinancialGoalForecastAlgorithm, FinancialGoalForecastAlgorithm>();
builder.Services.AddScoped<IFinancialGoalBudgetAdjustmentAlgorithm, FinancialGoalBudgetAdjustmentAlgorithm>();
builder.Services.AddHttpClient<IChatGptService, ChatGptService>();

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

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var error = ApiErrorFactory.FromModelState(
                context.ModelState,
                context.HttpContext.TraceIdentifier);

            return ApiErrorFactory.ToObjectResult(error);
        };
    });
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
        policy => policy.WithOrigins("http://localhost:3000")
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

app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (exception != null)
        {
            var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
            logger.LogError(exception, "Unhandled API exception");
        }

        var error = exception == null
            ? ApiErrorFactory.FromStatusCode(StatusCodes.Status500InternalServerError, context.TraceIdentifier)
            : ApiErrorFactory.FromException(exception, context.TraceIdentifier);

        context.Response.StatusCode = error.Status;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(error);
    });
});

app.UseStatusCodePages(async context =>
{
    var response = context.HttpContext.Response;
    if (!response.HasStarted && response.StatusCode is StatusCodes.Status401Unauthorized
        or StatusCodes.Status403Forbidden
        or StatusCodes.Status404NotFound)
    {
        var error = ApiErrorFactory.FromStatusCode(
            response.StatusCode,
            context.HttpContext.TraceIdentifier);

        response.ContentType = "application/json";
        await response.WriteAsJsonAsync(error);
    }
});

app.UseHttpsRedirection();
app.UseCors("AllowReact");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
