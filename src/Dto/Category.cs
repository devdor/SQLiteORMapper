using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLiteORMapper.Dto {
    [Table("category")]
    public class Category : AbstractDataItem {
        #region Fields and Properties
        [AppColumn("name")]
        public string Name {
            get;
            set;
        }
        #endregion

        public static Category Create(string name) {

            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException("Name");

            return new Category() {
                Name = name
            };
        }
    }
}
