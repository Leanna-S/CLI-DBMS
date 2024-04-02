using CLI_DBMS.Data;
using CLI_DBMS.Models;
using DBMS_Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Options;
using Microsoft.VisualBasic;
using System;
using System.ComponentModel.Design;
using System.Xml.Linq;

namespace CLI_DBMS
{
    /*
     * Leanna's Database Management System (DBMS)
     *
     * The Features:
     *
     * Generic
     * Written to work on any arbitrary database, so connecting this system with any other database structure would still work
     * Any relationships, tables, and datatypes are all handled by this system
     * This is done by accessing system reflections
     * I wanted to make it so i didnt need switch or if statements everywhere that would manually have to be edited if a table was added or removed
     *
     * Error handlings
     * There are try-catches everywhere, very hard to get an error past
     *
     * Create
     * Create a record for any table
     * Verifies the correct datatypes used for each property
     *
     * Read
     * Read the data from any table with any set of properties
     * Excludes virtual properties (navingation properties)
     * The read entries table include pagination so you can see the data in managable chunks
     *
     * Update
     * Update any record on any table
     * Modify any property on the selected record
     * Verifies correct datatype being submitted
     *
     * Delete
     * Delete any record on any table
     * Simple, and works
     *
     * Insert From File
     * Insert as many entries as you want into any table
     * Simply provide the file path
     * Works the similarly to create with datatype verification
     * See Sample_Insert_Data.txt for the proper way to format a file to be inserted
     *
     * How to use:
     *
     * Please run update-database to get the latest migrations
     * Or create your own database - as long as the dbcontext is named ApplicationDBContext, it should work :)
     * Once complete, hit the play button at the top of VS code OR run DBMS.MainMenu()
     * A console will open and you can navigate through the options using the numbers on the left
     * Enjoy!
     *
     */

    public static class DBMS
    {
        private static readonly List<MenuItem> StartUpOptions =
        [
            new ("Create new entry for a table", DoCreateTableEntry),
            new ("Read entries of a table", DoReadTable),
            new ("Update entry of a table", DoUpdateTableEntry),
            new ("Delete entry from a table", DoDeleteEntry),
            new ("Insert data from file", DoImportDataFromFile),
            new ("Exit", Exit),
        ];

        private const int itemsOnPage = 10;
        private const string AcceptOption = "Yes";
        private const string DenyOption = "No";
        private const string DisplayPreviousOption = "Display Previous";
        private const string DisplayNextOption = "Display Next";
        private const string DisplayingPreviousOption = "Displaying Previous";
        private const string DisplayingNextOption = "Displaying Next";
        private const string ReturnOption = "Return";
        private const string TableNameAttribute = "TableName";

        public static void Exit()
        {
            Environment.Exit(0);
        }

        public static string GetSelectedTableName()
        {
            using var context = new ApplicationDBContext();

            Console.WriteLine("Please select a table:");

            // get all table names + back option
            var options = context.Model
                .GetEntityTypes()
                .Select(entity => entity.GetTableName())
                .ToList();
            options.Add(ReturnOption);

            // prompt user and get picked item
            var tableSelected = UserInputManager.GetSelectedItem(options);

            if (tableSelected == null)
            {
                throw new ArgumentException(nameof(tableSelected));
            }

            // return selected item
            return tableSelected;
        }

        public static void DoDeleteEntry()
        {
            try
            {
                Console.WriteLine("Deleting an entry:");
                using var context = new ApplicationDBContext();

                // get users to pick a table
                string tableName = GetSelectedTableName();

                // see if user wants to go back
                if (tableName == ReturnOption)
                {
                    return;
                }
                Console.WriteLine("You selected the table: " + tableName);

                Console.WriteLine("Please pick an entry to delete:");

                // display table entries
                List<string> options = GetTableEntriesAsStrings(tableName);
                options.Add(ReturnOption);

                // get user to pick index of entry to delete

                var option = UserInputManager.GetSelectedItem(options);

                if (option == ReturnOption)
                {
                    return;
                }

                var index = options.IndexOf(option);

                var dbSet = context
                    .GetType()
                    .GetProperty(tableName)?
                    .GetValue(context, null) as IQueryable<object>
                    ?? throw new ArgumentNullException("Invalid table name or database context.");

                // get entry to delete
                var entryToRemove = dbSet
                    .ElementAtOrDefault(index)
                    ?? throw new ArgumentNullException();

                // remove entry and save changes
                context.Remove(entryToRemove);
                context.SaveChanges();
                Console.WriteLine("Entry successfully removed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            UserInputManager.GetSelectedItem(new[] { ReturnOption });
        }

        public static void DoUpdateTableEntry()
        {
            try
            {
                Console.WriteLine("Update an entry:");
                using var context = new ApplicationDBContext();

                string tableName = GetSelectedTableName();

                if (tableName == ReturnOption)
                {
                    return;
                }
                Console.WriteLine("You selected the table: " + tableName);

                Console.WriteLine("Please pick an entry to update:");

                List<string> options = GetTableEntriesAsStrings(tableName);
                options.Add(ReturnOption);

                var option = UserInputManager.GetSelectedItem(options);

                if (option == ReturnOption)
                {
                    return;
                }

                var index = options.IndexOf(option);

                var dbSet = context
                    .GetType()
                    .GetProperty(tableName)?
                    .GetValue(context, null) as IQueryable<object>
                    ?? throw new ArgumentNullException();

                var entryToUpdate = dbSet
                    .ElementAtOrDefault(index)
                    ?? throw new ArgumentNullException();

                var entityType = context.Model
                    .GetEntityTypes()
                    .FirstOrDefault(entity => entity.GetTableName() == tableName)
                    ?? throw new ArgumentNullException();

                UpdateEntryProperties(entryToUpdate, entityType);
                context.SaveChanges();
                Console.WriteLine("Successfully updated entry.");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            UserInputManager.GetSelectedItem(new[] { ReturnOption });
        }

        private static void UpdateEntryProperties(object entryToUpdate, IEntityType entityType)
        {
            bool doneModifying = false;
            do
            {
                Console.WriteLine("Please pick a property to modify:");

                // get property name
                string propertyName = UserInputManager.GetSelectedItem(entityType.GetProperties().Select(property => property.Name));

                Console.WriteLine("Please enter the new " + propertyName);

                // update the selected property
                bool isValid = false;
                do
                {
                    string input = UserInputManager.GetUserInput();
                    // get property info
                    var propertyInfo = entityType.ClrType
                        .GetProperty(propertyName)
                        ?? throw new ArgumentNullException();

                    try
                    {
                        var converted = Convert.ChangeType(input, propertyInfo.PropertyType);
                        propertyInfo.SetValue(entryToUpdate, converted);
                        isValid = true;
                    }
                    catch
                    {
                        Console.WriteLine($"Input not correct type, expecting {propertyInfo.PropertyType}");
                    }
                } while (!isValid);

                // see if the user has any more changes they'd like to make
                Console.WriteLine("Are there any more changes you'd like to make?");
                var result = UserInputManager.GetSelectedItem(new[] { AcceptOption, DenyOption });
                if (result == DenyOption)
                {
                    doneModifying = true;
                }
            } while (!doneModifying);
        }

        public static void DoReadTable()
        {
            Console.WriteLine("Reading a table:");
            try
            {
                //prompt user to pick table
                string tableName = GetSelectedTableName();

                // check if option is to go back
                if (tableName == ReturnOption)
                {
                    return;
                }
                Console.WriteLine("You selected the table: " + tableName);

                // get all table entries
                List<string> entityStrings = GetTableEntriesAsStrings(tableName);

                int totalPages = (int)Math.Ceiling(entityStrings.Count / (decimal)itemsOnPage);
                int currentPage = 1;
                string selectedOption;

                do
                {
                    // display a page of entries to the console
                    int paginationIndex = (currentPage - 1) * itemsOnPage;

                    UserInputManager.DisplayGroupToConsole(entityStrings.GetRange(paginationIndex, GetAmountOfItemsCanShow(currentPage, totalPages, entityStrings.Count)));

                    // get the pagination options
                    List<string> options = GeneratePaginationOptions(currentPage, totalPages);

                    Console.WriteLine("What would you like to do?");
                    // get the users selected option
                    selectedOption = UserInputManager.GetSelectedItem(options);

                    // handle pangination options
                    HandleSelectedPaginationOption(selectedOption, ref currentPage, itemsOnPage);
                } while (selectedOption != ReturnOption);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static int GetAmountOfItemsCanShow(int currentPage, int totalPages, int itemAmount)
        {
            int amountCanShow;
            if (currentPage == totalPages && itemAmount % itemsOnPage != 0)
            {
                amountCanShow = itemAmount % itemsOnPage;
            }
            else
            {
                amountCanShow = itemsOnPage;
            }
            return amountCanShow;
        }

        private static List<string> GeneratePaginationOptions(int currentPage, int totalPages)
        {
            List<string> options = [];

            // get pagination options
            if (currentPage > 1)
            {
                options.Add(DisplayPreviousOption);
            }
            if (currentPage < totalPages)
            {
                options.Add(DisplayNextOption);
            }

            options.Add(ReturnOption);
            return options;
        }

        private static void HandleSelectedPaginationOption(string selectedOption, ref int currentPage, int pageSize)
        {
            // Method to handle selected option
            if (selectedOption == DisplayPreviousOption)
            {
                Console.WriteLine(DisplayingPreviousOption);
                currentPage--;
            }
            if (selectedOption == DisplayNextOption)
            {
                Console.WriteLine(DisplayingNextOption);
                currentPage++;
            }
        }

        public static List<string> GetTableEntriesAsStrings(string tableName)
        {
            using var context = new ApplicationDBContext();

            var tableEntityType = context.Model
                .GetEntityTypes()
                .FirstOrDefault(entity => entity.GetTableName() == tableName);

            if (tableEntityType == null)
            {
                throw new ArgumentNullException(nameof(tableEntityType));
            }

            var dbSet = context
                .GetType()
                .GetProperty(tableName)?
                .GetValue(context, null) as IQueryable<object>
                ?? throw new ArgumentNullException();

            // Print entries to the console
            List<string> options = [];

            foreach (var entry in dbSet)
            {
                var entryProperties = entry.GetType().GetProperties().Where(property =>
                {
                    var prop = property.GetGetMethod();
                    if (prop == null)
                    {
                        return false;
                    }
                    return !prop.IsVirtual;
                });

                string entryText = "";

                // Print property values
                foreach (var property in entryProperties)
                {
                    var value = property.GetValue(entry);

                    entryText += $"{property.Name}: {value} ";
                }

                options.Add(entryText);
            }
            return options;
        }

        public static void DoCreateTableEntry()
        {
            try
            {
                Console.WriteLine("Creating an entry:");
                using var context = new ApplicationDBContext();

                // get user to select table
                string tableName = GetSelectedTableName();

                // check if option is to go back
                if (tableName == ReturnOption)
                {
                    return;
                }
                Console.WriteLine("You selected the table: " + tableName);

                // get entity type
                var tableEntityType = context.Model
                    .GetEntityTypes()
                    .FirstOrDefault(entity => entity.GetTableName() == tableName)
                    ?? throw new ArgumentNullException();

                // get attributes
                var attributes = GetAttributesForTable(tableEntityType);

                // create entity to add
                var entity = CreateEntityWithAttributes(tableEntityType, attributes);

                // add entity and save changes
                context.Add(entity);
                context.SaveChanges();
                Console.WriteLine("Successfully inserted data into table");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            UserInputManager.GetSelectedItem(new[] { ReturnOption });
        }

        private static Dictionary<string, object> GetAttributesForTable(IEntityType table)
        {
            // create dictionary of attributes
            var attributes = new Dictionary<string, object>();

            // get the users input for each property
            foreach (var property in table.GetProperties())
            {
                Console.WriteLine("Please enter the " + property.Name);
                bool isValid = false;

                do
                {
                    string input = UserInputManager.GetUserInput();
                    try
                    {
                        // try to convert the value the user inputed to the property type
                        var converted = Convert.ChangeType(input, property.ClrType);

                        // add the property to list
                        attributes.Add(property.Name, converted);
                        isValid = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                } while (!isValid);
            }
            return attributes;
        }

        private static object CreateEntityWithAttributes(IEntityType table, Dictionary<string, object> attributes)
        {
            // create instance of the table type
            var entity = Activator.CreateInstance(table.ClrType);

            if (entity == null)
            {
                throw new ArgumentNullException();
            }

            // set properties of entity
            foreach (var attribute in attributes)
            {
                // add property to instance
                var property = table.ClrType.GetProperty(attribute.Key);
                if (property != null)
                {
                    property.SetValue(entity, attribute.Value);
                }
                else
                {
                    Console.WriteLine($"Property {attribute.Key} not found in entity {table.GetTableName()}.");
                }
            }

            // return that entity
            return entity;
        }

        private static void AddEntriesFromFile(List<Dictionary<string, object>> entitiesAttibutes)
        {
            // for every entity
            foreach (var entityAttributes in entitiesAttibutes)
            {
                try
                {
                    using var context = new ApplicationDBContext();
                    // get the table the entity is being inserted into
                    var tableEntityType = context.Model
                        .GetEntityTypes()
                        .FirstOrDefault(entity => entity.GetTableName() == (string)entityAttributes[TableNameAttribute])
                        ?? throw new ArgumentNullException();

                    // remove the table attribute
                    entityAttributes.Remove(TableNameAttribute);

                    // create the entity
                    var entity = CreateEntityWithAttributes(tableEntityType, entityAttributes);

                    // add entity and save changes
                    context.Add(entity);
                    context.SaveChanges();
                    Console.WriteLine("Successfully inserted data into table");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        public static void DoImportDataFromFile()
        {
            using var context = new ApplicationDBContext();

            Console.WriteLine("Please input the file path:");
            string filePath = UserInputManager.GetUserInput();

            // Initialize variables
            List<Dictionary<string, object>> entitiesToAdd = new List<Dictionary<string, object>>();
            Dictionary<string, object> currentEntityAttributes = new Dictionary<string, object>();

            try
            {
                if (File.Exists(filePath))
                {
                    // Read the data from the text file
                    string[] lines = File.ReadAllLines(filePath);

                    string currentTableName = string.Empty;

                    // Parse the lines
                    foreach (string line in lines)
                    {
                        if (line.Trim() == "")
                        {
                            // Empty line indicates the end of a record
                            if (currentEntityAttributes.Count > 0)
                            {
                                entitiesToAdd.Add(currentEntityAttributes);
                                currentEntityAttributes = new();
                                currentTableName = string.Empty;
                            }
                        }
                        else
                        {
                            // Split the line into key and value
                            string[] parts = line.Split(':');
                            string key = parts[0].Trim();
                            string value = parts[1].Trim();

                            if (key == TableNameAttribute)
                            {
                                currentTableName = value;
                                currentEntityAttributes.Add(key, value);
                                continue;
                            }
                            try
                            {
                                if (string.IsNullOrEmpty(currentTableName))
                                {
                                    throw new Exception($"No {TableNameAttribute} defined for entry");
                                }
                                var tableEntityType = context.Model
                                    .GetEntityTypes()
                                    .FirstOrDefault(entity => entity.GetTableName() == currentTableName)
                                    ?? throw new ArgumentNullException($"Couldn't find table {currentTableName}");

                                try
                                {
                                    // try to convert the value the user inputed to the property type
                                    var convertedValue = Convert.ChangeType(value, tableEntityType.GetProperty(key).ClrType);

                                    // add the property to list
                                    currentEntityAttributes.Add(key, convertedValue);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.Message);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine(ex.Message);
                            }
                        }
                    }

                    // add final entry to list
                    if (currentEntityAttributes.Count > 0)
                    {
                        entitiesToAdd.Add(currentEntityAttributes);
                    }
                    AddEntriesFromFile(entitiesToAdd);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Cannot export data: {ex.Message}");
            }

            UserInputManager.GetSelectedItem(new[] { ReturnOption });
        }

        public static void MainMenu()
        {
            MenuItem selection;
            do
            {
                Console.Clear();
                Console.WriteLine("Welcome to the DBMS");

                selection = UserInputManager.GetSelectedItem(StartUpOptions);
                Console.Clear();
                selection.Action();
            } while (selection.Option != "Exit");
        }
    }

    internal class Program
    {
        private static void Main(string[] args)
        {
            DBMS.MainMenu();
        }
    }
}