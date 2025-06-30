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
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
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
// Ïîäêëþ÷àåì EF Core è MS SQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IApplicationDbContext>(sp =>
    sp.GetRequiredService<ApplicationDbContext>());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.Run();
