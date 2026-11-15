using Microsoft.EntityFrameworkCore;
using Tajan.Workflows.Extensions.Data.ArchiveEntities;
using Tajan.Workflows.Extensions.Data.MainEntities;
using Tajan.Workflows.Extensions.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is required.");

builder.Services.AddDbContext<ElsaMainDataDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddDbContext<ElsaArchiveDataDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IWorkflowInstanceService, WorkflowInstanceService>();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
