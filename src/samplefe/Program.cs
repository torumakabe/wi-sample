// <directives>
using System;
using System.Threading;
using System.Net.Http.Headers;
// using Microsoft.Identity.Client; // not needed here; avoid LogLevel ambiguity
using Azure.Core;
using Azure;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;
using Microsoft.Data.SqlClient;
// <directives>

namespace samplefe
{
    public class Program
    {
        static async Task Main(string[] args)
        {
            string API_ENDPOINT = Environment.GetEnvironmentVariable("API_ENDPOINT") ?? "http://sampleapi/weatherforecast";
            string API_SCOPE = Environment.GetEnvironmentVariable("API_SCOPE") ?? "api://YOUR-API-APP-ID/.default";
            string SQL_SERVER = Environment.GetEnvironmentVariable("SQL_SERVER") ?? "";
            string SQL_DATABASE = Environment.GetEnvironmentVariable("SQL_DATABASE") ?? "master";

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddSimpleConsole(options =>
                    {
                        options.SingleLine = true;
                        options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ ";
                    });
            });
            var logger = loggerFactory.CreateLogger("samplefe");

            // Simplify: rely on Azure Workload Identity (AKS)
            // Environment vars are injected by the WI webhook: AZURE_CLIENT_ID / AZURE_TENANT_ID / AZURE_FEDERATED_TOKEN_FILE
            TokenCredential cred = new WorkloadIdentityCredential();
            var httpClient = new HttpClient();

            while (true)
            {
                logger.LogInformation("samplefe start: utc={UtcNow}, node={Node}", DateTime.UtcNow, Environment.MachineName);
                
                // Test SQL Database connection if configured
                if (!string.IsNullOrEmpty(SQL_SERVER))
                {
                    try
                    {
                        var sqlToken = await cred.GetTokenAsync(
                            new TokenRequestContext(scopes: ["https://database.windows.net/.default"]),
                            CancellationToken.None);
                        logger.LogInformation("SQL target: server={Server}, database={Database}", SQL_SERVER, SQL_DATABASE);

                        var connectionString = $"Server={SQL_SERVER}; Database={SQL_DATABASE}; Encrypt=True; TrustServerCertificate=False; Connection Timeout=30;";

                        using (var connection = new SqlConnection(connectionString))
                        {
                            connection.AccessToken = sqlToken.Token;
                            await connection.OpenAsync();
                            
                            var command = new SqlCommand("SELECT @@VERSION", connection);
                            var version = await command.ExecuteScalarAsync();
                            logger.LogInformation("SQL Database connected successfully. Version: {Version}", version?.ToString()?.Substring(0, 50) + "...");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Failed to connect to SQL Database");
                    }
                }
                
                // API call
                var token = await cred.GetTokenAsync(
                    new TokenRequestContext(scopes: [API_SCOPE]),
                    CancellationToken.None);

                var request = new HttpRequestMessage(HttpMethod.Get, API_ENDPOINT);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);

                using HttpResponseMessage response = await httpClient.SendAsync(request);
                response.EnsureSuccessStatusCode();

                logger.LogInformation("API response status: {Status}", response.StatusCode);
                var body = await response.Content.ReadAsStringAsync();
                logger.LogInformation("API response body: {Body}", body);
                logger.LogInformation("Entra ID token expires on: {ExpiresOn}", token.ExpiresOn);

                // sleep and retry periodically
                await Task.Delay(60000);
            }
        }
    }
}
