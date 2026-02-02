using Microsoft.AspNetCore.DataProtection;
using Amazon.DynamoDBv2;
using StarTracker.Core;
using StarTracker.Core.Services;
using StarTracker.Infrastructure.Repositories;
using StarTracker.Infrastructure.Encryption;

namespace StarTracker.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStarTrackerServices(this IServiceCollection services, IConfiguration config)
    {
        // Data Protection for dev encryption of sensitive payloads
        services.AddDataProtection();

        // Configure encryption provider via configuration
        // Default: DataProtection. To enable AWS Encryption SDK provider set "Encryption:UseAwsSdk" = "true"
        var useAwsSdk = config.GetValue<bool>("Encryption:UseAwsSdk");
        if (useAwsSdk)
        {
            // Register fake envelope encryptor for now (replace with real SDK-backed implementation when enabled)
            services.AddSingleton<IAwsEnvelopeEncryptor, FakeAwsEnvelopeEncryptor>();
            services.AddSingleton<IEncryptionService, KmsEncryptionService>();
        }
        else
        {
            services.AddSingleton<IEncryptionService, DataProtectionEncryptionService>();
        }

        // Guidance service
        services.AddSingleton<IGuidanceService, SimpleGuidanceService>();

        // Configure repository (InMemory or DynamoDB)
        var repoType = config.GetValue<string>("Repository:Type") ?? "InMemory";
        if (repoType.Equals("DynamoDB", StringComparison.OrdinalIgnoreCase))
        {
            // DynamoDB repository
            services.AddSingleton<IAmazonDynamoDB>(sp =>
            {
                var dynamoDbConfig = config.GetSection("Repository:DynamoDB");
                var serviceUrl = dynamoDbConfig.GetValue<string>("ServiceUrl");
                var region = dynamoDbConfig.GetValue<string>("Region") ?? "us-east-1";

                var clientConfig = new AmazonDynamoDBConfig
                {
                    RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(region)
                };

                if (!string.IsNullOrEmpty(serviceUrl))
                {
                    clientConfig.ServiceURL = serviceUrl;
                    // For local DynamoDB, use dummy credentials
                    return new AmazonDynamoDBClient(
                        new Amazon.Runtime.BasicAWSCredentials("local", "local"),
                        clientConfig);
                }

                return new AmazonDynamoDBClient(clientConfig);
            });

            services.AddSingleton<IObservationRepository>(sp =>
            {
                var dynamoDb = sp.GetRequiredService<IAmazonDynamoDB>();
                var encryption = sp.GetRequiredService<IEncryptionService>();
                var tableName = config.GetValue<string>("Repository:DynamoDB:TableName") ?? "observations";
                return new DynamoDbObservationRepository(dynamoDb, encryption, tableName);
            });
        }
        else
        {
            // Default: InMemory repository
            services.AddSingleton<IObservationRepository, InMemoryObservationRepository>();
        }

        return services;
    }
}
