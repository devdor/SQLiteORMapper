using System;
using System.IO;

namespace SQLiteORMapper {
    public class Util {        
        public static string ConvertCompareOp(CompareOp op) {

            switch (op) {

                case CompareOp.EqualTo: return "=";
                case CompareOp.GreaterThan: return ">";
                case CompareOp.GreaterThanEqualTo: return ">=";
                case CompareOp.LessThan: return "<";
                case CompareOp.LessThanEqualTo: return "<=";
                case CompareOp.NotEqualTo: return "!=";
            }

            throw new ArgumentException("Unknown CompareOp");
        }

        public static string GetSqliteConnectionString(FileInfo fileInfo) {

            if (fileInfo == null)
                throw new ArgumentNullException("FileInfo");

            return @$"Data Source={fileInfo.FullName}";
        }
    }
}
