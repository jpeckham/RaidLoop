using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Options;
using RaidLoop.Client;
using RaidLoop.Client.Configuration;
using RaidLoop.Client.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.Configure<SupabaseOptions>(builder.Configuration.GetSection(SupabaseOptions.SectionName));
builder.Services.AddScoped<SupabaseAuthService>();
builder.Services.AddScoped<ISupabaseSessionProvider>(sp => sp.GetRequiredService<SupabaseAuthService>());
builder.Services.AddScoped<IProfileApiClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<SupabaseOptions>>().Value;
    return new ProfileApiClient(
        new HttpClient
        {
            BaseAddress = new Uri($"{options.Url.TrimEnd('/')}/functions/v1/")
        },
        sp.GetRequiredService<ISupabaseSessionProvider>(),
        options);
});
builder.Services.AddScoped<IGameActionApiClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<SupabaseOptions>>().Value;
    return new GameActionApiClient(
        new HttpClient
        {
            BaseAddress = new Uri($"{options.Url.TrimEnd('/')}/functions/v1/")
        },
        sp.GetRequiredService<ISupabaseSessionProvider>(),
        options);
});

await builder.Build().RunAsync();
