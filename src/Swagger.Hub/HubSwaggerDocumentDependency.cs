using System;
using System.Linq;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using PrimeFuncPack;

namespace GarageGroup.Infra;

public static class HubSwaggerDocumentDependency
{
    public static Dependency<IHubSwaggerDocumentProvider> UseHubSwaggerDocumentProvider(
        this Dependency<HttpMessageHandler, SwaggerHubOption> dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.With(ResolveLoggerFactory).Fold<IHubSwaggerDocumentProvider>(HubSwaggerDocumentProvider.Create);
    }

    public static Dependency<IHubSwaggerDocumentProvider> UseHubSwaggerDocumentProvider(
        this Dependency<HttpMessageHandler> dependency, string sectionName = "Swagger")
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.With(ResolveOption).With(ResolveLoggerFactory).Fold<IHubSwaggerDocumentProvider>(HubSwaggerDocumentProvider.Create);

        SwaggerHubOption ResolveOption(IServiceProvider serviceProvider)
            =>
            serviceProvider.GetRequiredService<IConfiguration>().GetSwaggerHubOption(sectionName);
    }

    private static SwaggerHubOption GetSwaggerHubOption(this IConfiguration configuration, string sectionName)
    {
        return new(
            option: configuration.GetSwaggerOption(sectionName),
            documents: configuration.GetSection(sectionName).GetSection("Documents").GetChildren().Select(GetDocumentOption).ToFlatArray());

        static SwaggerDocumentOption GetDocumentOption(IConfigurationSection documentSection)
            =>
            new(
                baseAddress: documentSection.GetUri("BaseAddressUrl"),
                documentUrl: documentSection["DocumentUrl"])
            {
                UrlSuffix = documentSection["UrlSuffix"],
                IsDirectCall = documentSection.GetBoolean("IsDirectCall"),
                Parameters = documentSection.GetSection("Parameters").GetChildren().Select(GetOrThrow<OpenApiParameter>).ToFlatArray()
            };
    }

    private static ILoggerFactory? ResolveLoggerFactory(IServiceProvider serviceProvider)
        =>
        serviceProvider.GetService<ILoggerFactory>();

    private static Uri GetUri(this IConfigurationSection section, string key)
    {
        var value = section[key];
        if (string.IsNullOrEmpty(value))
        {
            throw new InvalidOperationException($"Configuration path '{section.Path}:{key}' value must be specified");
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) is false)
        {
            throw new InvalidOperationException($"Configuration path '{section.Path}:{key}' value '{value}' must be a valid absolute URI");
        }

        return uri;
    }

    private static bool GetBoolean(this IConfigurationSection section, string key)
    {
        var value = section[key];
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return string.Equals("true", value, StringComparison.InvariantCultureIgnoreCase);
    }

    private static T GetOrThrow<T>(this IConfigurationSection section)
        =>
        section.Get<T>() ?? throw new InvalidOperationException($"Configuration path '{section.Path}' value must be a '{typeof(T)}' value");
}