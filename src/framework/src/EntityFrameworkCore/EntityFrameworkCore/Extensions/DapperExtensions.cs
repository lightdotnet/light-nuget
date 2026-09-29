using Dapper;
using Light.EntityFrameworkCore.Extensions;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using System.Data.Common;

namespace Light.EntityFrameworkCore.Extensions;

/// <summary>
///     Raw SQL query via Dapper
/// </summary>
/// <remarks>
///     All overloads run on the context's connection and enlist in the context's current transaction
///     (<c>context.Database.CurrentTransaction</c>) when one is active.
/// </remarks>
public static class DapperExtensions
{
    public static async Task<IEnumerable<T>> QueryAsync<T>(this DbContext context,
        string query, Func<DbDataReader, T> map,
        CancellationToken cancellationToken = default)
    {
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = query;
        command.CommandType = CommandType.Text;
        command.Transaction = context.Database.CurrentTransaction?.GetDbTransaction();

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
            await context.Database.CloseConnectionAsync().ConfigureAwait(false);
        }
    }

    public static Task<IEnumerable<T>> QueryAsync<T>(this DbContext context,
        string query, object? param = null, CommandType commandType = CommandType.Text)
        => context.QueryAsync<T>(query, param, commandType, CancellationToken.None);

    /// <summary>
    ///     Executes <paramref name="query"/> via Dapper with cancellation support.
    /// </summary>
    public static async Task<IEnumerable<T>> QueryAsync<T>(this DbContext context,
        string query, object? param, CommandType commandType, CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            query,
            param,
            transaction: context.Database.CurrentTransaction?.GetDbTransaction(),
            commandType: commandType,
            cancellationToken: cancellationToken);

        return await context.Database.GetDbConnection().QueryAsync<T>(command).ConfigureAwait(false);
    }
}
