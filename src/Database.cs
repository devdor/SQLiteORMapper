using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using SQLiteORMapper.Dto;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.IO;
using System.Linq;

namespace SQLiteORMapper {
    public class Database {
        #region Fields and Properties
        public string ConString {
            get;
            private set;
        }

        ILogger<Database> _logger;
        #endregion

        public Database(ILoggerFactory loggerFactory, FileInfo dbFileName) {

            if (loggerFactory == null)
                throw new ArgumentNullException("LoggerFactory");

            if (dbFileName == null)
                throw new ArgumentNullException("DbFileName");

            if (!dbFileName.Exists)
                throw new FileNotFoundException("DbFileName");

            this._logger = loggerFactory.CreateLogger<Database>();
            this.ConString = @$"Data Source={dbFileName.FullName}";
        }

        public T Read<T>(Int64 id) {
            
            var tmpList = ReadList<T>(new DbQueryItem("Id", CompareOp.EqualTo, id));
            if (!DictionaryUtils.IsNullOrEmpty(tmpList)) {

                return tmpList.FirstOrDefault();
            }

            return default(T);
        }

        public T Read<T>(params DbQueryItem[] args) {
            if (String.IsNullOrEmpty(this.ConString))
                throw new ArgumentNullException("ConnectionString");

            var tmpList = ReadList<T>(args);
            if (!DictionaryUtils.IsNullOrEmpty(tmpList)) {

                return tmpList.FirstOrDefault();
            }

            return default(T);
        }
        
        public IList<T> ReadList<T>(params DbQueryItem[] args) {
            if (String.IsNullOrEmpty(this.ConString))
                throw new MissingFieldException("ConnectionString");

            Type dstType = typeof(T);
            var propList = dstType.GetProperties();

            if (GetTableInfo(dstType, out string tblName, out List<Tuple<string,Type>> colNameList)) {

                using (var con = new SqliteConnection(this.ConString)) {

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
                                            $"{colName} {ConvertCompareOp(arg.Op)} @{colName}");

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

                        this._logger.LogDebug(cmd.CommandText);
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

        public int Delete<T>(Int64 id) {

            return Delete<T>(
                new DbQueryItem("Id", CompareOp.EqualTo, id));
        }

        public int Delete<T>(DbQueryItem queryItem) {

            return Delete<T>(
                new List<DbQueryItem>() {
                    queryItem
                }.ToArray());
        }

        public int Delete<T>(params DbQueryItem[] args) {

            if (String.IsNullOrEmpty(this.ConString))
                throw new MissingFieldException("ConnectionString");

            Type dstType = typeof(T);
            var propList = dstType.GetProperties();

            if (GetTableInfo(dstType, out string tblName, out List<Tuple<string, Type>> colNameList)) {

                using (var con = new SqliteConnection(this.ConString)) {

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
                                            $"{colName} {ConvertCompareOp(arg.Op)} @{colName}");

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

                        this._logger.LogDebug(cmd.CommandText);
                        return cmd.ExecuteNonQuery();
                    }
                }
            }

            return 0;
        }

        public Int64 Create(AbstractDataItem item) {

            if (String.IsNullOrEmpty(this.ConString))
                throw new MissingFieldException("ConnectionString");

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

                    using (var con = new SqliteConnection(this.ConString)) {

                        using (var cmd = con.CreateCommand()) {

                            cmd.CommandText = $"INSERT INTO {tblName} ({String.Join(',', valueList.Select(obj => obj.Item1))}) " +
                                $"VALUES ({String.Join(',', valueList.Select(obj => "@" + obj.Item1))})";

                            foreach (var kvItem in valueList) {

                                cmd.Parameters.Add(new SqliteParameter($"@{kvItem.Item1}", kvItem.Item2));
                            }

                            con.Open();

                            this._logger.LogDebug(cmd.CommandText);
                            int result = cmd.ExecuteNonQuery();
                            if (result > 0) {

                                cmd.CommandText = "select last_insert_rowid()";

                                this._logger.LogDebug(cmd.CommandText);
                                return (Int64)cmd.ExecuteScalar();
                            }
                        }
                    }       
                }
            }
            return 0;
        }

        public int Update(AbstractDataItem item) {

            if (String.IsNullOrEmpty(this.ConString))
                throw new MissingFieldException("ConnectionString");

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

                    using (var con = new SqliteConnection(this.ConString)) {

                        using (var cmd = con.CreateCommand()) {

                            var tmpList = new List<string>();
                            foreach (var kvItem in valueList) {

                                tmpList.Add($"{kvItem.Item1}=@{kvItem.Item1}");
                                cmd.Parameters.Add(new SqliteParameter($"@{kvItem.Item1}", kvItem.Item2));                                
                            }

                            cmd.CommandText = $"UPDATE {tblName} SET {String.Join(',', tmpList)} " +
                                $"WHERE id={item.Id}";

                            con.Open();

                            this._logger.LogDebug(cmd.CommandText);
                            return cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            return 0;
        }

        string ConvertCompareOp(CompareOp op) {

            switch (op) {

                case CompareOp.EqualTo:
                    return "=";
                case CompareOp.GreaterThan:
                    return ">";
                case CompareOp.GreaterThanEqualTo:
                    return ">=";
                case CompareOp.LessThan:
                    return "<";
                case CompareOp.LessThanEqualTo:
                    return "<=";
                case CompareOp.NotEqualTo:
                    return "!=";
            }

            throw new ArgumentException("Unknown CompareOp");
        }

        bool GetTableInfo(Type type, out string table, out List<Tuple<string, Type>> colNameList) {

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
