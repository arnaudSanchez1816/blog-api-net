using BlogApi.Installers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.InstallOpenApi(builder.Configuration);

WebApplication app = builder.Build();

app.InstallScalar();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();