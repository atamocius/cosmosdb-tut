using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;

namespace cosmosdb_tut
{
    using static Console;

    class Program
    {
        static async Task Main(string[] args)
        {
            var root = Directory.GetCurrentDirectory();
            var dotenv = Path.Combine(root, ".env");
            DotEnv.Load(dotenv);

            var config =
                new ConfigurationBuilder()
                    .AddEnvironmentVariables()
                    .Build();

            var ENDPOINT_URI = config["ENDPOINT_URI"];
            var PRIMARY_KEY = config["PRIMARY_KEY"];

            await GetStartedDemo(ENDPOINT_URI, PRIMARY_KEY);
        }

        private static async Task GetStartedDemo(
            string endpointUri,
            string primaryKey)
        {
            try
            {
                WriteLine("Beginning operations...\n");

                var db = new DemoDB(endpointUri, primaryKey);
                await db.CreateDatabase();
                await db.CreateContainer();
                await db.AddItemsToContainer();
                await db.QueryItems();
                await db.ReplaceFamilyItem();
                // await db.DeleteFamilyItem();
                // await db.DeleteDatabaseAndCleanup();
            }
            catch (CosmosException de)
            {
                var baseException = de.GetBaseException();
                WriteLine("{0} error occurred: {1}", de.StatusCode, de);
            }
            catch (Exception e)
            {
                WriteLine("Error: {0}", e);
            }
            finally
            {
                WriteLine("End of demo, press any key to exit.");
                // ReadKey();
            }
        }
    }
}
