using budget_tracker_backend.Data;
using budget_tracker_backend.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Подключаем EF Core и MS SQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

app.UseCors("AllowReact"); // Включаем CORS

app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        context.Response.StatusCode = 500;
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (exception != null)
        {
            Console.WriteLine($"Ошибка на сервере: {exception.Message}");
            Console.WriteLine($"StackTrace: {exception.StackTrace}");
        }
        await context.Response.WriteAsync("An unexpected error occurred.");
    });
});

app.Run();
