using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLiteORMapper.Dto {
    [Table("category_item")]
    public class CategoryItem : AbstractDataItem {
        #region Fields and Properties
        [AppColumn("ref_category")]
        public Int64 RefCategory {
            get;
            set;
        }

        [AppColumn("name")]
        public string Name {
            get;
            set;
        }
        #endregion
        public static CategoryItem Create(Int64 refCategory, string name) {
            if (String.IsNullOrEmpty(name))
                throw new ArgumentNullException("Name");

            return new CategoryItem() {
                RefCategory = refCategory,
                Name = name
            };
        }
    }
}
