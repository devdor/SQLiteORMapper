using System.ComponentModel.DataAnnotations.Schema;

namespace SQLiteORMapper {
    class AppColumnAttribute : ColumnAttribute {
        #region Fields and Properties
        public bool CanInsert {
            get;
            set;
        }

        public bool CanUpdate {
            get;
            set;
        }
        #endregion
        public AppColumnAttribute(string name, bool canInsert = true, bool canUpdate = true)
            : base(name) {

            this.CanInsert = canInsert;
            this.CanUpdate = canUpdate;
        }
    }
}
