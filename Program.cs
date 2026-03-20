using ApplicationInsights.Middleware;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Azure Test API",
        Version = "v1",
        Description = "API for testing logs, responses, and Application Insights"
    });
});

builder.Services.AddApplicationInsightsTelemetry();

builder.Services.AddHttpLogging(logging =>
{
    logging.LoggingFields =
        Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestMethod |
        Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestPath |
        Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestQuery |
        Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.RequestBody |
        Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseStatusCode |
        Microsoft.AspNetCore.HttpLogging.HttpLoggingFields.ResponseBody;

    logging.RequestBodyLogLimit = 4096;
    logging.ResponseBodyLogLimit = 4096;
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpLogging();
app.UseMiddleware<RequestResponseLoggingMiddleware>();

app.UseAuthorization();

app.MapControllers();

app.Run();