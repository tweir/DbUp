using System;
using MySql.Data;
using System.IO;
using MySql.Data.MySqlClient;
using NUnit.Framework;

namespace DbUp.Specification
{
    [TestFixture]
    public class MySqlSupportTests
    {
        [Test]
        public void CanUseMySql()
        {
            string connectionString = "server=localhost;user=root;port=3306;password=;";

            using (var conn = new MySqlConnection(connectionString))
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();
                cmd.CommandText = "CREATE DATABASE IF NOT EXISTS `dbuptest`;";
                cmd.ExecuteNonQuery();
            }
            connectionString = "server=localhost;user=root;port=3306;password=;database=dbuptest";
            var upgrader = DeployChanges.To
                .MySqlDatabase(connectionString)
                .WithScript("Script0001", "create table $schema$.Foo (Id int)")
                .Build();

            var result = upgrader.PerformUpgrade();

            Assert.IsTrue(result.Successful);
        }
    }
}
