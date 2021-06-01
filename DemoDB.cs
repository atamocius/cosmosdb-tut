using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;

namespace cosmosdb_tut
{
    using static Console;

    public class DemoDB
    {
        private const string DATABASE_ID = "FamilyDatabase";
        private const string CONTAINER_ID = "FamilyContainer";

        private CosmosClient client;
        private Database database;
        private Container container;

        public DemoDB(string endpointUri, string primaryKey)
        {
            // Create a new instance of the Cosmos Client
            this.client = new CosmosClient(endpointUri, primaryKey);
        }

        /// <summary>
        /// Create the database if it does not exist
        /// </summary>
        public async Task CreateDatabase()
        {
            // Create a new database
            this.database =
                await this.client.CreateDatabaseIfNotExistsAsync(DATABASE_ID);

            WriteLine("Created Database: {0}\n", this.database.Id);
        }

        /// <summary>
        /// Create the container if it does not exist.
        ///
        /// Specifiy "/LastName" as the partition key since we're storing family
        /// information, to ensure good distribution of requests and storage.
        /// </summary>
        public async Task CreateContainer()
        {
            // Create a new container
            this.container =
                await this.database.CreateContainerIfNotExistsAsync(
                    CONTAINER_ID,
                    "/LastName");

            WriteLine("Created Container: {0}\n", this.container.Id);
        }

        /// <summary>
        /// Add Family items to the container
        /// </summary>
        public async Task AddItemsToContainer()
        {
            await this.AddItemToContainer2(DemoData.AndersenFamily);
            await this.AddItemToContainer2(DemoData.WakefieldFamily);
        }

        /// <summary>
        /// Run a query (using Azure Cosmos DB SQL syntax) against the container
        /// </summary>
        public async Task QueryItems()
        {
            var sqlQueryText = "SELECT * FROM c WHERE c.LastName = 'Andersen'";

            WriteLine("Running query: {0}\n", sqlQueryText);

            var queryDefinition = new QueryDefinition(sqlQueryText);
            var queryResultSetIterator =
                this.container.GetItemQueryIterator<Family>(queryDefinition);

            var families = new List<Family>();

            while (queryResultSetIterator.HasMoreResults)
            {
                var currentResultSet =
                    await queryResultSetIterator.ReadNextAsync();

                foreach (var family in currentResultSet)
                {
                    families.Add(family);
                    WriteLine("\tRead {0}\n", family);
                }
            }
        }

        /// <summary>
        /// Replace an item in the container
        /// </summary>
        public async Task ReplaceFamilyItem()
        {
            var wakefieldFamilyResponse =
                await this.container.ReadItemAsync<Family>(
                    "Wakefield.7",
                    new PartitionKey("Wakefield"));

            var itemBody = wakefieldFamilyResponse.Resource;
            // update registration status from false to true
            itemBody.IsRegistered = true;
            // update grade of child
            itemBody.Children[0].Grade = 6;

            // replace the item with the updated content
            wakefieldFamilyResponse =
                await this.container.ReplaceItemAsync(
                    itemBody,
                    itemBody.Id,
                    new PartitionKey(itemBody.LastName));

            WriteLine(
                "Updated Family [{0},{1}].\n \tBody is now: {2}\n",
                itemBody.LastName,
                itemBody.Id,
                wakefieldFamilyResponse.Resource);
        }

        /// <summary>
        /// Delete an item in the container
        /// </summary>
        public async Task DeleteFamilyItem()
        {
            var partitionKeyValue = "Wakefield";
            var familyId = "Wakefield.7";

            // Delete an item. Note we must provide the partition key value and
            // id of the item to delete
            var wakefieldFamilyResponse =
                await this.container.DeleteItemAsync<Family>(
                    familyId,
                    new PartitionKey(partitionKeyValue));

            WriteLine(
                "Deleted Family [{0},{1}]\n",
                partitionKeyValue,
                familyId);
        }

        /// <summary>
        /// Delete the database and dispose of the Cosmos Client instance
        /// </summary>
        public async Task DeleteDatabaseAndCleanup()
        {
            var databaseResourceResponse = await this.database.DeleteAsync();
            // Also valid: await this.client.Databases["FamilyDatabase"].DeleteAsync();

            WriteLine("Deleted Database: {0}\n", DATABASE_ID);

            //Dispose of CosmosClient
            this.client.Dispose();
        }

        private async Task AddItemToContainer(Family family)
        {
            try
            {
                // Read the item to see if it exists.
                var response =
                    await this.container.ReadItemAsync<Family>(
                        family.Id,
                        new PartitionKey(family.LastName));

                WriteLine(
                    "Item in database with id: {0} already exists\n",
                    response.Resource.Id);
            }
            catch (CosmosException ex)
                when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Create an item in the container representing the family. Note
                // we provide the value of the partition key for this item,
                // which is the last name
                var response =
                    await this.container.CreateItemAsync(
                        family,
                        new PartitionKey(family.LastName));

                // Note that after creating the item, we can access the body of
                // the item with the Resource property off the ItemResponse. We
                // can also access the RequestCharge property to see the amount
                // of RUs consumed on this request.
                WriteLine(
                    "Created item in database with id: {0} Operation consumed {1} RUs.\n",
                    response.Resource.Id,
                    response.RequestCharge);
            }
        }

        private async Task AddItemToContainer2(Family family)
        {
            var pk = new PartitionKey(family.LastName);

            var readResponse = await this.container.ReadItemStreamAsync(
                family.Id,
                pk);

            var statusCode = readResponse.StatusCode;

            using (readResponse)
            {
                if (readResponse.IsSuccessStatusCode)
                {
                    var f = await JsonSerializer.DeserializeAsync<Family>(
                        readResponse.Content);

                    WriteLine(
                        "Item in database with id: {0} already exists\n",
                        f.Id);

                    return;
                }
            }

            if (statusCode == HttpStatusCode.NotFound)
            {
                var createResponse = await this.container.CreateItemAsync(
                    family,
                    pk);

                WriteLine(
                    "Created item in database with id: {0} Operation consumed {1} RUs.\n",
                    createResponse.Resource.Id,
                    createResponse.RequestCharge);
            }
        }
    }
}
