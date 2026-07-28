using Dapper;
using Light.EntityFrameworkCore.Extensions;
using System.Data;
using System.Data.Common;

namespace Light.EntityFrameworkCore.Extensions;

/// <summary>
///     Raw SQL query via Dapper
/// </summary>
public static class DapperExtensions
{
    public static async Task<IEnumerable<T>> QueryAsync<T>(this DbContext context,
        string query, Func<DbDataReader, T> map,
        CancellationToken cancellationToken = default)
    {
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = query;
        command.CommandType = CommandType.Text;

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

    public static async Task<IEnumerable<T>> QueryAsync<T>(this DbContext context,
        string query, object? param = null, CommandType commandType = CommandType.Text)
    {
        if (param is not null)
            return await context.Database.GetDbConnection().QueryAsync<T>(query, param,
                commandType: commandType).ConfigureAwait(false);
        else
            return await context.Database.GetDbConnection().QueryAsync<T>(query,
                commandType: commandType).ConfigureAwait(false);
    }
}