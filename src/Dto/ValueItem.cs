using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace SQLiteORMapper.Dto {
    [Table("value_item")]
    public class ValueItem : AbstractDataItem {
        #region Fields and Properties
        [AppColumn("ref_category_item")]
        public Int64 RefCategoryItem {
            get;
            set;
        }

        [AppColumn("pos")]
        public int Pos {
            get;
            set;
        }

        [AppColumn("value_type")]
        public int ValueType {
            get;
            set;
        }

        [AppColumn("value")]
        public string Value {
            get;
            set;
        }
        #endregion

        public static ValueItem Create(Int64 refCategoryItem, int pos, SecValueType valueType, string value) {

            return new ValueItem() {
                RefCategoryItem = refCategoryItem,
                Pos = pos,
                ValueType = (int)valueType,
                Value = value
            };
        }
    }
}
