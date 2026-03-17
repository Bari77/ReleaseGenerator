using Api.Configuration;
using Api.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<ReleaseGeneratorOptions>(
    builder.Configuration.GetSection(ReleaseGeneratorOptions.SectionName));
builder.Services.AddSingleton<IChangelogParser, ChangelogParser>();
builder.Services.AddHttpClient<IOllamaReleaseService, OllamaReleaseService>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<ReleaseGeneratorOptions>>().Value;
    client.Timeout = TimeSpan.FromSeconds(options.Ollama.TimeoutSeconds);
});
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
