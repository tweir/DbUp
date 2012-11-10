using System;
using DbUp.Builder;
using DbUp.MySql;
using DbUp.Support.SqlServer;
using MySql.Data.MySqlClient;

/// <summary>
/// Configuration extension methods for MySql.
/// </summary>
// NOTE: DO NOT MOVE THIS TO A NAMESPACE
// Since the class just contains extension methods, we leave it in the root so that it is always discovered
// and people don't have to manually add using statements.
// ReSharper disable CheckNamespace
public static class MySqlExtensions
// ReSharper restore CheckNamespace
{
    /// <summary>
    /// Creates an upgrader for MySql databases.
    /// </summary>
    /// <param name="supported">Fluent helper type.</param>
    /// <param name="connectionFactory">The connection factory.</param>
    /// <returns>
    /// A builder for a database upgrader designed for MySql databases.
    /// </returns>
    public static UpgradeEngineBuilder MySqlDatabase(this SupportedDatabases supported, Func<MySqlConnection> connectionFactory)
    {
        var builder = new UpgradeEngineBuilder();
        builder.Configure(c => c.ConnectionFactory = connectionFactory);
        builder.Configure(c => c.ScriptExecutor = new MySqlScriptExecutor(c.ConnectionFactory, () => c.Log, () => c.VariablesEnabled, c.ScriptPreprocessors));
        builder.Configure(c => c.Journal = new MySqlTableJournal(c.ConnectionFactory, "SchemaVersions", c.Log));
        builder.WithPreprocessor(new MySqlPreprocessor());
        return builder;
    }

    /// <summary>
    /// Creates an upgrader for SQL CE databases.
    /// </summary>
    /// <param name="supported">Fluent helper type.</param>
    /// <param name="connectionString">The connection string.</param>
    /// <returns>
    /// A builder for a database upgrader designed for SQL Server databases.
    /// </returns>
    public static UpgradeEngineBuilder MySqlDatabase(this SupportedDatabases supported, string connectionString)
    {
        return supported.MySqlDatabase(() => new MySqlConnection(connectionString));
    }
}
