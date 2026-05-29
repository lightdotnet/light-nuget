using Dapper;
using System.Data;
using System.Data.Common;

namespace Light.EntityFrameworkCore.Extensions;

/// <summary>
/// Raw SQL query via Dapper
/// </summary>
public static class DapperExtensions
{
    /// <summary>
    /// Execute a raw SQL query and map results using a custom reader function
    /// </summary>
    public static async Task<IEnumerable<T>> QueryAsync<T>(this DbContext context,
        string query, Func<DbDataReader, T> map,
        CancellationToken cancellationToken = default)
    {
        var connection = context.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;

        using var command = connection.CreateCommand();
        command.CommandText = query;
        command.CommandType = CommandType.Text;

        if (!wasOpen)
            await context.Database.OpenConnectionAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            using var result = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            var entities = new List<T>();
            while (await result.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                entities.Add(map(result));
            }
            return entities;
        }
        finally
        {
            if (!wasOpen)
                await context.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Execute a raw SQL query via Dapper with optional parameters
    /// </summary>
    public static async Task<IEnumerable<T>> QueryAsync<T>(this DbContext context,
        string query, object? param = null, CommandType commandType = CommandType.Text)
    {
        var connection = context.Database.GetDbConnection();
        var wasOpen = connection.State == ConnectionState.Open;

        if (!wasOpen)
            await context.Database.OpenConnectionAsync().ConfigureAwait(false);

        try
        {
            return await connection
                .QueryAsync<T>(query, param, commandType: commandType)
                .ConfigureAwait(false);
        }
        finally
        {
            if (!wasOpen)
                await context.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }
}