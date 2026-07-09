using BlogApi.Installers;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.InstallApiVersioning();
builder.Services.InstallDatabase(builder.Configuration);
builder.Services.InstallIdentity();
builder.Services.InstallOpenApi(builder.Configuration);
builder.Services.InstallDomainServices();
builder.Services.AddControllers();

WebApplication app = builder.Build();
if (app.Environment.IsDevelopment())
{
    await app.DoDatabaseMigration();
}

await app.DoDatabaseSeeding();

app.InstallScalar();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();