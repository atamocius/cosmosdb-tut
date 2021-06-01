using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text.Json;
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
            // Create a family object for the Andersen family
            var andersenFamily = new Family
            {
                Id = "Andersen.1",
                LastName = "Andersen",
                Parents = new[]
                {
                    new Parent { FirstName = "Thomas" },
                    new Parent { FirstName = "Mary Kay" },
                },
                Children = new[]
                {
                    new Child
                    {
                        FirstName = "Henriette Thaulow",
                        Gender = "female",
                        Grade = 5,
                        Pets = new []
                        {
                            new Pet { GivenName = "Fluffy" },
                        },
                    },
                },
                Address = new Address
                {
                    State = "WA",
                    County = "King",
                    City = "Seattle",
                },
                IsRegistered = false,
            };

            try
            {
                // Read the item to see if it exists.
                var andersenFamilyResponse =
                    await this.container.ReadItemAsync<Family>(
                        andersenFamily.Id,
                        new PartitionKey(andersenFamily.LastName));

                WriteLine(
                    "Item in database with id: {0} already exists\n",
                    andersenFamilyResponse.Resource.Id);
            }
            catch (CosmosException ex)
                when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Create an item in the container representing the Andersen
                // family. Note we provide the value of the partition key for
                // this item, which is "Andersen"
                var andersenFamilyResponse =
                    await this.container.CreateItemAsync(
                        andersenFamily,
                        new PartitionKey(andersenFamily.LastName));

                // Note that after creating the item, we can access the body of
                // the item with the Resource property off the ItemResponse. We
                // can also access the RequestCharge property to see the amount
                // of RUs consumed on this request.
                WriteLine(
                    "Created item in database with id: {0} Operation consumed {1} RUs.\n",
                    andersenFamilyResponse.Resource.Id,
                    andersenFamilyResponse.RequestCharge);
            }

            // Create a family object for the Wakefield family
            var wakefieldFamily = new Family
            {
                Id = "Wakefield.7",
                LastName = "Wakefield",
                Parents = new[]
                {
                    new Parent {
                        FamilyName = "Wakefield",
                        FirstName = "Robin",
                    },
                    new Parent {
                        FamilyName = "Miller",
                        FirstName = "Ben",
                    },
                },
                Children = new[]
                {
                    new Child
                    {
                        FamilyName = "Merriam",
                        FirstName = "Jesse",
                        Gender = "female",
                        Grade = 8,
                        Pets = new []
                        {
                            new Pet { GivenName = "Goofy" },
                            new Pet { GivenName = "Shadow" },
                        },
                    },
                    new Child
                    {
                        FamilyName = "Miller",
                        FirstName = "Lisa",
                        Gender = "female",
                        Grade = 1,
                    },
                },
                Address = new Address
                {
                    State = "NY",
                    County = "Manhattan",
                    City = "NY",
                },
                IsRegistered = true,
            };

            try
            {
                // Read the item to see if it exists
                var wakefieldFamilyResponse =
                    await this.container.ReadItemAsync<Family>(
                        wakefieldFamily.Id,
                        new PartitionKey(wakefieldFamily.LastName));

                WriteLine(
                    "Item in database with id: {0} already exists\n",
                    wakefieldFamilyResponse.Resource.Id);
            }
            catch (CosmosException ex)
                when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Create an item in the container representing the Wakefield
                // family. Note we provide the value of the partition key for
                // this item, which is "Wakefield"
                var wakefieldFamilyResponse =
                    await this.container.CreateItemAsync(
                        wakefieldFamily,
                        new PartitionKey(wakefieldFamily.LastName));

                // Note that after creating the item, we can access the body of
                // the item with the Resource property off the ItemResponse. We
                // can also access the RequestCharge property to see the amount
                // of RUs consumed on this request.
                WriteLine(
                    "Created item in database with id: {0} Operation consumed {1} RUs.\n",
                    wakefieldFamilyResponse.Resource.Id,
                    wakefieldFamilyResponse.RequestCharge);
            }
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
    }
}
