using Microsoft.Extensions.Configuration;
using Npgsql;

namespace AnbuFight.Infrastructure.Persistence;

/// <summary>
/// Descobre a string de conexão do Postgres aceitando os dois formatos que aparecem na prática:
/// o formato nativo do Npgsql (<c>Host=...;Database=...</c>) e a URI que provedores gerenciados
/// como o Railway e o Heroku injetam em <c>DATABASE_URL</c> (<c>postgresql://usuario:senha@host/db</c>).
///
/// O driver não entende a URI, então converter aqui evita ter que montar a string à mão nas
/// variáveis de ambiente do provedor.
/// </summary>
public static class ConnectionStringResolver
{
    private const string ConnectionStringName = "Default";

    private const string DatabaseUrlKey = "DATABASE_URL";

    public static string Resolve(IConfiguration configuration)
    {
        var configured = configuration.GetConnectionString(ConnectionStringName);

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return Normalize(configured);
        }

        var databaseUrl = configuration[DatabaseUrlKey];

        if (!string.IsNullOrWhiteSpace(databaseUrl))
        {
            return Normalize(databaseUrl);
        }

        throw new InvalidOperationException(
            $"Nenhum banco configurado. Defina \"ConnectionStrings:{ConnectionStringName}\" " +
            $"ou a variável de ambiente {DatabaseUrlKey}.");
    }

    public static bool LooksLikeUri(string value) =>
        value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

    private static string Normalize(string value) =>
        LooksLikeUri(value) ? FromUri(value) : value;

    private static string FromUri(string value)
    {
        var uri = new Uri(value);
        var credentials = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = uri.AbsolutePath.Trim('/'),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null,

            // Provedores gerenciados costumam usar certificado próprio no endpoint interno.
            // Para exigir validação de certificado, informe a string completa em
            // ConnectionStrings:Default em vez de depender da URI.
            SslMode = SslMode.Prefer,
            TrustServerCertificate = true
        };

        return builder.ConnectionString;
    }
}
