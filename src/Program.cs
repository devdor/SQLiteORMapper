using System;
using System.IO;
using SQLiteORMapper.Dto;

namespace SQLiteORMapper {
    class Program {
        static void Main(string[] args) {
            
            try {

                var conString = Util.GetSqliteConnectionString(
                    new FileInfo(@$"{Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)}\secure_dev.db"));

                /*
                // Create new item
                var id = Database.Create(conString, 
                    new Category() { 
                        Name = "Cat3" 
                    });
                 */

                /*
                // Read collection
                var tmpList = Database.ReadList<Category>(
                    Util.GetSqliteConnectionString(this.DbFileInfo), 
                    new List<DbQueryItem>() { 
                        new DbQueryItem("Id", CompareOp.GreaterThan, 1) 
                    }.ToArray());
                    */

                /*
                // Delete row
                int numRowsDeleted = Database.Delete<Category>(
                Util.GetSqliteConnectionString(this.DbFileInfo),
                new List<DbQueryItem>() {
                    new DbQueryItem("Id", CompareOp.EqualTo, 1)
                }.ToArray());*/

                // Update item
                var tmpItem = Database.Read<Category>(conString, 1);
                if (tmpItem != null) {

                    tmpItem.Name = "NewTestName";
                    int numRowsUpdated = Database.Update(conString, tmpItem);
                }
            }
            catch(Exception ex) {

                Console.Write(ex);
            }            
        }
    }
}
