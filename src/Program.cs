using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SQLiteORMapper.Dto;
using System;
using System.Collections.Generic;
using System.IO;

namespace SQLiteORMapper {
    class Program {
        static void Main(string[] args) {

            var serviceCollection = new ServiceCollection()
                    .AddLogging(builder => {
                        builder.SetMinimumLevel(LogLevel.Trace);
                        builder.AddConsole();
                    });

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger<Program>();

            try {                

                var dbFileInfo = new FileInfo(@$"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\secure_dev.db");
                if (!dbFileInfo.Exists) {

                    logger.LogWarning($"Database {dbFileInfo.Name} not found.");
                    return;
                }

                var db = new Database(loggerFactory,
                    new FileInfo(@$"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\secure_dev.db"));

                logger.LogInformation($"Database {dbFileInfo.Name} initialized.");


                // Read collection
                var catList = db.ReadList<Category>(
                    new List<DbQueryItem>() {
                        new DbQueryItem("Id", CompareOp.GreaterThan, 0)
                    }.ToArray());

                if (!DictionaryUtils.IsNullOrEmpty(catList)) {

                    logger.LogInformation($"ReadList categories received {catList.Count} rows");

                    // Create new Category
                    var catId = db.Create(
                        new Category() {
                            Name = $"CategoryName {catList.Count + 1}"
                    });

                    if (catId > 1) {

                        logger.LogInformation($"New category [Id {catId}] created");

                        // Update category
                        var cat = db.Read<Category>(catId);
                        if (cat != null) {

                            cat.Name = $"CategoryName {catList.Count +1}";
                            int numRowsUpdated = db.Update(cat);

                            if (numRowsUpdated == 1) {

                                logger.LogInformation($"Category [Id {catId}] updated");

                                // Delete newly create item
                                if (db.Delete<Category>(cat.Id) == 1) {

                                    logger.LogInformation($"Category [Id {catId}] deleted");
                                }
                            }
                        }
                    }
                }

                Console.ReadKey();
            }
            catch(Exception ex) {

                logger.LogError(ex.Message);
            }            
        }

        static void InitDefault(Database db) {

            if (db == null)
                throw new ArgumentNullException("Database");

            var catId = db.Create(Category.Create("Favorites"));

            if (catId >= 0) {

                var catItemId = db.Create(CategoryItem.Create(catId, "Github"));

                if (catItemId > 0) {

                    db.Create(ValueItem.Create(catItemId, 0, SecValueType.Url, "https://github.com"));
                    db.Create(ValueItem.Create(catItemId, 1, SecValueType.Username, "My Username"));
                    db.Create(ValueItem.Create(catItemId, 2, SecValueType.Password, "My Password"));                }

                catItemId = db.Create(CategoryItem.Create(catId, "Emailaccount"));

                if (catItemId > 0) {

                    db.Create(ValueItem.Create(catItemId, 0, SecValueType.Url, "https://somewhere.com"));
                    db.Create(ValueItem.Create(catItemId, 1, SecValueType.Username, "My Username"));
                    db.Create(ValueItem.Create(catItemId, 2, SecValueType.Password, "My Password"));
                }
            }
        }
    }
}
