using Ui.Core.Services;
using Serilog;
using Serilog.Events;
using Serilog.Templates;
using Ui.Core.Data;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog logging
builder.Host.UseSerilog();
// Add services to the container.
builder.Services.AddControllers(o => o.InputFormatters.Insert(o.InputFormatters.Count, new SocketMessageInputFormatter()));
builder.Services.AddSingleton<NetworkStreams>();
builder.Services.AddHostedService<SocketServer>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors();

var app = builder.Build();
app.UseCors(options => options.WithOrigins("http://localhost:8080").AllowAnyMethod());

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();


app.Run();
