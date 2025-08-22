// MyBooks.Common/Configuration/ConfigurationBuilderExtensions.cs
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MyBooks.Common.Configuration;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddMyBooksDefaultProviders(
        this IConfigurationBuilder cfg, IHostEnvironment env)
    {
        // appsettings.json & appsettings.{Environment}.json
        cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
           .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);

        // Environment variables
        cfg.AddEnvironmentVariables();

        // User Secrets in Development
        if (env.IsDevelopment())
        {
            // optional: no-op if no user-secrets defined
            cfg.AddUserSecrets(typeof(ConfigurationBuilderExtensions).Assembly, optional: true);
        }

        // Docker/K8s secrets mounted under /run/secrets
        cfg.AddKeyPerFile(directoryPath: "/run/secrets", optional: true, reloadOnChange: true);

        return cfg;
    }
}
