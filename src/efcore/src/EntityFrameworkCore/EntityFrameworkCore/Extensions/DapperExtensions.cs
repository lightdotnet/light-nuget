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
        // await RawSqlQuery(query, x => new T { Prop0 = (string)x[0], Prop1 = (string)x[1] });
        using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText = query;
        command.CommandType = CommandType.Text;

        await context.Database.OpenConnectionAsync(cancellationToken);

        using var result = await command.ExecuteReaderAsync(cancellationToken);
        var entities = new List<T>();

        while (result.Read())
        {
            entities.Add(map(result));
        }

        return entities;
    }

    public static async Task<IEnumerable<T>> QueryAsync<T>(this DbContext context,
        string query, object? param = null, CommandType commandType = CommandType.Text)
    {
        if (param is not null)
            return await context.Database.GetDbConnection().QueryAsync<T>(query, param,
                commandType: commandType);
        else
            return await context.Database.GetDbConnection().QueryAsync<T>(query,
                commandType: commandType);
    }
}
