using Microsoft.Data.Sqlite;
using SQLiteORMapper.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Linq;

namespace SQLiteORMapper {
    public class Database {

        public static void InitDefault(string conString) {

            if (String.IsNullOrEmpty(conString))
                throw new ArgumentNullException("ConnectionString");

            var catId = Create(conString,
                Category.Create("Favorites"));

            if (catId >= 0) {

                var catItemId = Create(conString,
                    CategoryItem.Create(catId, "Github"));

                if (catItemId > 0) {

                    Create(conString,
                        ValueItem.Create(catItemId, 0, SecValueType.Url, "https://github.com"));

                    Create(conString,
                        ValueItem.Create(catItemId, 1, SecValueType.Username, "My Username"));

                    Create(conString,
                        ValueItem.Create(catItemId, 2, SecValueType.Password, "My Password"));
                }

                catItemId = Create(conString,
                    CategoryItem.Create(catId, "Emailaccount"));

                if (catItemId > 0) {

                    Create(conString,
                        ValueItem.Create(catItemId, 0, SecValueType.Url, "https://somewhere.com"));

                    Create(conString,
                        ValueItem.Create(catItemId, 1, SecValueType.Username, "My Username"));

                    Create(conString,
                        ValueItem.Create(catItemId, 2, SecValueType.Password, "My Password"));
                }
            }
        }

        public static T Read<T>(string conString, Int64 id) {
            if (String.IsNullOrEmpty(conString))
                throw new ArgumentNullException("ConnectionString");

            var tmpList = ReadList<T>(conString, new DbQueryItem("Id", CompareOp.EqualTo, id));
            if (!DictionaryUtils.IsNullOrEmpty(tmpList)) {

                return tmpList.FirstOrDefault();
            }

            return default(T);
        }

        public static T Read<T>(string conString, params DbQueryItem?[] args) {
            if (String.IsNullOrEmpty(conString))
                throw new ArgumentNullException("ConnectionString");

            var tmpList = ReadList<T>(conString, args);
            if (!DictionaryUtils.IsNullOrEmpty(tmpList)) {

                return tmpList.FirstOrDefault();
            }

            return default(T);
        }
        
        public static IList<T> ReadList<T>(string conString, params DbQueryItem?[] args) {
            if (String.IsNullOrEmpty(conString))
                throw new ArgumentNullException("ConnectionString");

            Type dstType = typeof(T);
            var propList = dstType.GetProperties();

            if (GetTableInfo(dstType, out string tblName, out List<Tuple<string,Type>> colNameList)) {

                using (var con = new SqliteConnection(conString)) {

                    using (var cmd = con.CreateCommand()) {

                        string cmdText = $"SELECT {String.Join(',', colNameList.Select(t => t.Item1))} FROM {tblName}";
                        if (!DictionaryUtils.IsNullOrEmpty(args)) {

                            List<string> whereItemList = null;
                            foreach (var arg in args) {
                                
                                if (whereItemList == null)
                                    whereItemList = new List<string>();

                                var prop = propList.FirstOrDefault(obj => obj.Name.Equals(arg.PropertyName));
                                if (prop != null) {

                                    string colName = null;
                                    var attrList = prop.GetCustomAttributes(true);                                    
                                    if (!DictionaryUtils.IsNullOrEmpty(attrList)) {

                                        foreach (var attr in attrList) {

                                            if (attr is AppColumnAttribute) {

                                                colName = ((AppColumnAttribute)attr).Name;
                                            }
                                        }
                                    }

                                    if (!String.IsNullOrEmpty(colName)) {

                                        whereItemList.Add(
                                            $"{colName} {Util.ConvertCompareOp(arg.Op)} @{colName}");

                                        cmd.Parameters.Add(new SqliteParameter($"@{colName}", arg.Value));
                                    }                                    
                                }
                            }

                            if (!DictionaryUtils.IsNullOrEmpty(whereItemList)) {

                                cmdText += $" WHERE {String.Join(" AND ", whereItemList)}";
                            }
                        }

                        cmd.CommandText = cmdText;
                        con.Open();

                        using (var sqlReader = cmd.ExecuteReader()) {

                            var schemaInfo = sqlReader.GetColumnSchema();
                            List<List<Tuple<string, object>>> rows = new List<List<Tuple<string, object>>>();
                            while (sqlReader.Read()) {

                                List<Tuple<string, object>> row = new List<Tuple<string, object>>();
                                foreach (var col in schemaInfo) {

                                    row.Add(
                                        new Tuple<string, object>(col.ColumnName, sqlReader[col.ColumnName]));
                                }
                                rows.Add(row);
                            }

                            if (!DictionaryUtils.IsNullOrEmpty(rows)) {

                                List<T> result = new List<T>();
                                foreach (var row in rows) {

                                    object dto = null;
                                    if (dstType == typeof(Category)) {
                                        dto = new Category();
                                    }
                                    else if (dstType == typeof(CategoryItem)) {
                                        dto = new CategoryItem();
                                    }
                                    else if (dstType == typeof(ValueItem)) {
                                        dto = new ValueItem();
                                    }

                                    if (dto != null) {
                                        
                                        foreach (var prop in propList) {

                                            var attrList = prop.GetCustomAttributes(true);
                                            if (!DictionaryUtils.IsNullOrEmpty(attrList)) {

                                                foreach (var attr in attrList) {

                                                    if (attr is AppColumnAttribute) {

                                                        var collAttr = attr as AppColumnAttribute;
                                                        if (!String.IsNullOrEmpty(collAttr.Name)) {

                                                            var tmp = row.FirstOrDefault(obj => obj.Item1.Equals(collAttr.Name));
                                                            if (tmp != null) {

                                                                if (prop.PropertyType == typeof(String)) {

                                                                    prop.SetValue(dto, tmp.Item2);
                                                                }
                                                                else if (prop.PropertyType == typeof(Int32)) {

                                                                    if (tmp.Item2 != null
                                                                        && Int32.TryParse(tmp.Item2.ToString(), out Int32 intVal)) {

                                                                        prop.SetValue(dto, intVal);
                                                                    }
                                                                }
                                                                else if (prop.PropertyType == typeof(Int64)) {

                                                                    if (tmp.Item2 != null
                                                                        && Int64.TryParse(tmp.Item2.ToString(), out Int64 intVal)) {

                                                                        prop.SetValue(dto, intVal);
                                                                    }
                                                                }
                                                                else if (prop.PropertyType == typeof(DateTime)
                                                                    || prop.PropertyType == typeof(DateTime?)) {

                                                                    if (tmp.Item2 != null
                                                                        && DateTime.TryParse(tmp.Item2.ToString(), out DateTime dtVal)) {

                                                                        prop.SetValue(dto, dtVal);
                                                                    }
                                                                }
                                                                else {

                                                                    prop.SetValue(dto, tmp.Item2);
                                                                }                                                                
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }

                                        result.Add((T)dto);
                                    }                                    
                                }                               

                                return result as List<T>;
                            }
                        }
                    }
                }
            }

            return default(IList<T>);
        }

        public static int Delete<T>(string conString, params DbQueryItem?[] args) {

            if (String.IsNullOrEmpty(conString))
                throw new ArgumentNullException("ConnectionString");

            Type dstType = typeof(T);
            var propList = dstType.GetProperties();

            if (GetTableInfo(dstType, out string tblName, out List<Tuple<string, Type>> colNameList)) {

                using (var con = new SqliteConnection(conString)) {

                    using (var cmd = con.CreateCommand()) {

                        string cmdText = $"DELETE FROM {tblName}";
                        if (!DictionaryUtils.IsNullOrEmpty(args)) {

                            List<string> whereItemList = null;
                            foreach (var arg in args) {

                                if (whereItemList == null)
                                    whereItemList = new List<string>();

                                var prop = propList.FirstOrDefault(obj => obj.Name.Equals(arg.PropertyName));
                                if (prop != null) {

                                    string colName = null;
                                    var attrList = prop.GetCustomAttributes(true);
                                    if (!DictionaryUtils.IsNullOrEmpty(attrList)) {

                                        foreach (var attr in attrList) {

                                            if (attr is AppColumnAttribute) {

                                                colName = ((AppColumnAttribute)attr).Name;
                                            }
                                        }
                                    }

                                    if (!String.IsNullOrEmpty(colName)) {

                                        whereItemList.Add(
                                            $"{colName} {Util.ConvertCompareOp(arg.Op)} @{colName}");

                                        cmd.Parameters.Add(new SqliteParameter($"@{colName}", arg.Value));
                                    }
                                }
                            }

                            if (!DictionaryUtils.IsNullOrEmpty(whereItemList)) {

                                cmdText += $" WHERE {String.Join(" AND ", whereItemList)}";
                            }
                        }

                        cmd.CommandText = cmdText;
                        con.Open();

                        return cmd.ExecuteNonQuery();
                    }
                }
            }

            return 0;
        }

        public static Int64 Create(string conString, AbstractDataItem item) {

            if (String.IsNullOrEmpty(conString))
                throw new ArgumentNullException("ConnectionString");

            if (item == null)
                throw new ArgumentNullException("IdItem");

            item.State = (int)DataItemState.Default;
            item.Created = DateTime.UtcNow;
            item.Updated = null;

            Type dstType = item.GetType();
            var propList = dstType.GetProperties();

            if (GetTableInfo(dstType, out string tblName, out List<Tuple<string, Type>> colNameList)) {

                List<Tuple<string, object>> valueList = null;
                foreach (var prop in propList) {

                    var attrList = prop.GetCustomAttributes(true);
                    if (attrList != null) {

                        foreach (var attr in attrList) {

                            if (attr is AppColumnAttribute) {

                                var appAttr = attr as AppColumnAttribute;
                                if (appAttr.CanInsert) {

                                    if (valueList == null)
                                        valueList = new List<Tuple<string, object>>();

                                    valueList.Add(new Tuple<string, object>(
                                        appAttr.Name, prop.GetValue(item)));
                                }
                                break;
                            }
                        }
                    }
                }

                if (!DictionaryUtils.IsNullOrEmpty(valueList)) {

                    using (var con = new SqliteConnection(conString)) {

                        using (var cmd = con.CreateCommand()) {

                            cmd.CommandText = $"INSERT INTO {tblName} ({String.Join(',', valueList.Select(obj => obj.Item1))}) " +
                                $"VALUES ({String.Join(',', valueList.Select(obj => "@" + obj.Item1))})";

                            foreach (var kvItem in valueList) {

                                cmd.Parameters.Add(new SqliteParameter($"@{kvItem.Item1}", kvItem.Item2));
                            }

                            con.Open();
                            int result = cmd.ExecuteNonQuery();
                            if (result > 0) {

                                cmd.CommandText = "select last_insert_rowid()";

                                return (Int64)cmd.ExecuteScalar();
                            }
                        }
                    }       
                }
            }
            return 0;
        }

        public static int Update(string conString, AbstractDataItem item) {

            if (String.IsNullOrEmpty(conString))
                throw new ArgumentNullException("ConnectionString");

            if (item == null)
                throw new ArgumentNullException("IdItem");

            if (item.Id < 0)
                throw new ArithmeticException("InvalidId");

            item.Updated = DateTime.UtcNow;

            Type dstType = item.GetType();
            var propList = dstType.GetProperties();

            if (GetTableInfo(dstType, out string tblName, out List<Tuple<string, Type>> colNameList)) {

                List<Tuple<string, object>> valueList = null;
                foreach (var prop in propList.Where(
                    obj => !obj.Name.Equals("Id") 
                    && !obj.Name.Equals("Created"))) {

                    var attrList = prop.GetCustomAttributes(true);
                    if (attrList != null) {

                        foreach (var attr in attrList) {

                            if (attr is AppColumnAttribute) {

                                var appAttr = attr as AppColumnAttribute;
                                if (appAttr.CanUpdate) {

                                    if (valueList == null)
                                        valueList = new List<Tuple<string, object>>();

                                    valueList.Add(new Tuple<string, object>(
                                        appAttr.Name, prop.GetValue(item)));

                                    break;
                                }
                            }
                        }
                    }
                }

                if (!DictionaryUtils.IsNullOrEmpty(valueList)) {

                    using (var con = new SqliteConnection(conString)) {

                        using (var cmd = con.CreateCommand()) {

                            var tmpList = new List<string>();
                            foreach (var kvItem in valueList) {

                                tmpList.Add($"{kvItem.Item1}=@{kvItem.Item1}");
                                cmd.Parameters.Add(new SqliteParameter($"@{kvItem.Item1}", kvItem.Item2));                                
                            }

                            cmd.CommandText = $"UPDATE {tblName} SET {String.Join(',', tmpList)} " +
                                $"WHERE id={item.Id}";

                            con.Open();
                            return cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            return 0;
        }

        static bool GetTableInfo(Type type, out string table, out List<Tuple<string, Type>> colNameList) {

            table = null;
            colNameList = null;

            Type typeObj = null;
            if (type == typeof(Category)) {
                typeObj = typeof(Category);
            }
            else if (type == typeof(CategoryItem)) {
                typeObj = typeof(CategoryItem);
            }
            else if (type == typeof(ValueItem)) {
                typeObj = typeof(ValueItem);
            }

            if (typeObj != null) {

                var tmpClassAttrList = typeObj.GetCustomAttributes(true);
                if (!DictionaryUtils.IsNullOrEmpty(tmpClassAttrList)) {

                    foreach (var attr in tmpClassAttrList) {

                        if (attr is TableAttribute) {

                            table = ((TableAttribute)attr).Name;
                            break;
                        }
                    }

                    if (!String.IsNullOrEmpty(table)) {

                        var propList = typeObj.GetProperties();
                        if (!DictionaryUtils.IsNullOrEmpty(propList)) {

                            foreach (var prop in propList) {

                                var fieldAttrList = prop.GetCustomAttributes(true);
                                if (!DictionaryUtils.IsNullOrEmpty(fieldAttrList)) {

                                    foreach (var fieldAttr in fieldAttrList) {

                                        if (fieldAttr is AppColumnAttribute) {

                                            if (colNameList == null)
                                                colNameList = new List<Tuple<string, Type>>();

                                            colNameList.Add(
                                                new Tuple<string, Type>(((AppColumnAttribute)fieldAttr).Name, prop.PropertyType));
                                        }
                                    }
                                }
                            }
                        }

                        return !DictionaryUtils.IsNullOrEmpty(colNameList);
                    }
                }
            }

            return false;
        }

        /*
        CREATE TABLE "category" (
	        "id"	INTEGER NOT NULL DEFAULT 1000 PRIMARY KEY AUTOINCREMENT UNIQUE,
	        "created"	TIMESTAMP NOT NULL,
	        "updated"	TIMESTAMP,
	        "state"	INTEGER NOT NULL,
	        "name"	TEXT NOT NULL UNIQUE)

        CREATE TABLE "category_item" (
	        "id"	INTEGER NOT NULL DEFAULT 1000 PRIMARY KEY AUTOINCREMENT UNIQUE,
	        "created"	TIMESTAMP NOT NULL,
	        "updated"	TIMESATMP,
	        "state"	INTEGER NOT NULL,
	        "ref_category"	INTEGER NOT NULL,
	        "name"	TEXT NOT NULL UNIQUE)

        CREATE TABLE "value_item" (
	        "id"	INTEGER NOT NULL DEFAULT 1000 PRIMARY KEY AUTOINCREMENT UNIQUE,
	        "created"	TIMESTAMP NOT NULL,
	        "updated"	TIMESTAMP,
	        "state"	INTEGER NOT NULL,
	        "ref_category_item"	INTEGER NOT NULL,
	        "pos"	INTEGER NOT NULL,
	        "value_type"	INTEGER NOT NULL,
	        "value"	TEXT)
        */
    }
}
