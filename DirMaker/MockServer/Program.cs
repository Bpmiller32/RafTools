using System.Text;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<StatusReporter>();

// CORS
builder.Services.AddCors(options => options.AddPolicy("FrontEnd", pb => pb.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("FrontEnd");
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});


app.UseHttpsRedirection();

app.MapGet("/toggle", (StatusReporter statusReporter) =>
{
    statusReporter.ToggleStatus();
    return "Toggled Status";
});

// Status
app.MapGet("/status", async (HttpContext context, StatusReporter statusReporter) =>
{
    context.Response.Headers.Add("Content-Type", "text/event-stream");

    for (var i = 0; true; i++)
    {
        string message = statusReporter.UpdateReport();
        byte[] bytes = Encoding.ASCII.GetBytes($"data: {message}\r\r");

        await context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        await context.Response.Body.FlushAsync();
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
