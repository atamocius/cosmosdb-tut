using System.Text.Json;
using System.Text.Json.Serialization;

namespace cosmosdb_tut
{
    public static class DemoData
    {
        /// <summary>
        /// Create a family object for the Andersen family
        /// </summary>
        public static readonly Family AndersenFamily = new Family
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

        /// <summary>
        /// Create a family object for the Wakefield family
        /// </summary>
        public static readonly Family WakefieldFamily = new Family
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
    }

    public class Family
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }
        public string LastName { get; set; }
        public Parent[] Parents { get; set; }
        public Child[] Children { get; set; }
        public Address Address { get; set; }
        public bool IsRegistered { get; set; }

        public override string ToString() => JsonSerializer.Serialize(this);
    }

    public class Parent
    {
        public string FamilyName { get; set; }
        public string FirstName { get; set; }
    }

    public class Child
    {
        public string FamilyName { get; set; }
        public string FirstName { get; set; }
        public string Gender { get; set; }
        public int Grade { get; set; }
        public Pet[] Pets { get; set; }
    }

    public class Pet
    {
        public string GivenName { get; set; }
    }

    public class Address
    {
        public string State { get; set; }
        public string County { get; set; }
        public string City { get; set; }
    }
}
