using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text.RegularExpressions;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Preprocessors;
using DbUp.Helpers;
using MySql.Data.MySqlClient;

namespace DbUp.MySql
{
    /// <summary>
    /// A standard implementation of the IScriptExecutor interface that executes against a SQL Server 
    /// database.
    /// </summary>
    public sealed class MySqlScriptExecutor : IScriptExecutor
    {
        private readonly Func<IDbConnection> connectionFactory;
        private readonly Func<IUpgradeLog> log;
        private readonly IEnumerable<IScriptPreprocessor> scriptPreprocessors;
        private readonly Func<bool> variablesEnabled;

        /// <summary>
        /// SQLCommand Timeout in seconds. If not set, the default SQLCommand timeout is not changed.
        /// </summary>
        public int? ExecutionTimeoutSeconds { get; set; }

        /// <summary>
        /// Initializes an instance of the <see cref="MySqlScriptExecutor"/> class.
        /// </summary>
        /// <param name="connectionFactory">The connection factory.</param>
        /// <param name="log">The logging mechanism.</param>
        /// <param name="variablesEnabled">Function that returns <c>true</c> if variables should be replaced, <c>false</c> otherwise.</param>
        /// <param name="scriptPreprocessors">Script Preprocessors in addition to variable substitution</param>
        public MySqlScriptExecutor(Func<IDbConnection> connectionFactory, Func<IUpgradeLog> log, Func<bool> variablesEnabled, IEnumerable<IScriptPreprocessor> scriptPreprocessors)
        {
            this.connectionFactory = connectionFactory;
            this.log = log;
            Schema = null;
            this.variablesEnabled = variablesEnabled;
            this.scriptPreprocessors = scriptPreprocessors;
        }

        /// <summary>
        /// Database Schema, should be null if database does not support schemas
        /// </summary>
        public string Schema { get; set; }

        /// <summary>
        /// Executes the specified script against a database at a given connection string.
        /// </summary>
        /// <param name="script">The script.</param>
        public void Execute(SqlScript script)
        {
            Execute(script, null);
        }

        /// <summary>
        /// Verifies the existence of targeted schema. If schema is not verified, will check for the existence of the dbo schema.
        /// </summary>
        public void VerifySchema()
        {
            //MySQL has no concept of schemas...
        }

        /// <summary>
        /// Executes the specified script against a database at a given connection string.
        /// </summary>
        /// <param name="script">The script.</param>
        /// <param name="variables">Variables to replace in the script</param>
        public void Execute(SqlScript script, IDictionary<string, string> variables)
        {
            if (variables == null)
                variables = new Dictionary<string, string>();
            if (Schema != null && !variables.ContainsKey("schema"))
                variables.Add("schema", Schema);

            log().WriteInformation("Executing SQL Server script '{0}'", script.Name);

            var contents = script.Contents;
            if (string.IsNullOrEmpty(Schema))
                contents = new StripSchemaPreprocessor().Process(contents);
            if (variablesEnabled())
                contents = new VariableSubstitutionPreprocessor(variables).Process(contents);
            contents = (scriptPreprocessors??new IScriptPreprocessor[0])
                .Aggregate(contents, (current, additionalScriptPreprocessor) => additionalScriptPreprocessor.Process(current));


            /*
            var scriptStatements = SplitByGoStatements(contents);
            var index = -1;
            try
            {
                using (var connection = connectionFactory())
                {
                    connection.Open();

                    foreach (var statement in scriptStatements)
                    {
                        index++;
                        var command = connection.CreateCommand();
                        command.CommandText = statement;
                        if (ExecutionTimeoutSeconds != null)
                            command.CommandTimeout = ExecutionTimeoutSeconds.Value;
                        command.ExecuteNonQuery();
                    }
                }
            }*/
            try
            {
                using (var connection = connectionFactory())
                {
                    connection.Open();

                        var command = connection.CreateCommand();
                        command.CommandText = contents;
                        if (ExecutionTimeoutSeconds != null)
                            command.CommandTimeout = ExecutionTimeoutSeconds.Value;
                        command.ExecuteNonQuery();
                }
            }
            catch (MySqlException sqlException)
            {
                log().WriteInformation("SQL exception has occured in script: '{0}'", script.Name);
                log().WriteError("Number {0}; Message: {1}", sqlException.Number, sqlException.Message);
                log().WriteError(sqlException.ToString());
                throw;
            }
            catch (DbException sqlException)
            {
                log().WriteInformation("DB exception has occured in script: '{0}'", script.Name);
                log().WriteError("Error code {0}; Message: {1}", sqlException.ErrorCode, sqlException.Message);
                log().WriteError(sqlException.ToString());
                throw;
            }
            catch (Exception ex)
            {
                log().WriteInformation("Exception has occured in script: '{0}'", script.Name);
                log().WriteError(ex.ToString());
                throw;
            }
        }

    }
}
