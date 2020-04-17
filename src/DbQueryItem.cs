using System;

namespace SQLiteORMapper {
    public class DbQueryItem {
        #region Fields and Properties
        public string PropertyName {
            get;
            set;
        }
        public CompareOp Op {
            get;
            set;
        }
        public object Value {
            get;
            set;
        }
        #endregion
        public DbQueryItem() {
        }

        public DbQueryItem(string propName, CompareOp op, object value) {

            this.PropertyName = propName ?? throw new ArgumentNullException("PropertyName");
            this.Op = op;
            this.Value = value;
        }
    }
}
