using BlogApi.Installers;
using BlogApi.Seeding;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.InstallApiVersioning();
builder.Services.InstallDatabase(builder.Configuration);
builder.Services.InstallIdentity(builder.Configuration);
builder.Services.InstallOpenApi(builder.Configuration);
builder.Services.InstallDomainServices();
builder.Services.InstallBackgroundServices();
builder.Services.InstallRepositories();
builder.Services.InstallFluentValidation();
builder.Services.InstallExceptionHandlers();
builder.Services.InstallCustomModelBinders();
builder.Services.InstallAuthentication(builder.Configuration);
builder.Services.InstallAuthorization();
builder.Services.InstallCors(builder.Configuration);
builder.Services.AddControllers();

WebApplication app = builder.Build();
if (app.Environment.IsDevelopment())
{
    await app.MigrateDatabase();
}

DatabaseSeedingResult databaseSeedingResult = await app.SeedDatabase();

app.InstallExceptionHandlers();
app.InstallScalar(databaseSeedingResult.DevAccessToken);
app.UseHttpsRedirection();
app.UseRouting();
app.InstallCors();
app.UseAuthorization();
app.MapControllers();

app.Run();