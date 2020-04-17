using System;

namespace SQLiteORMapper.Dto {
    public abstract class AbstractDataItem {
        #region Fields and Properties
        [AppColumn("id", false, false)]
        public Int64 Id {
            get;
            set;
        }

        [AppColumn("created", true, false)]
        public DateTime Created {
            get;
            set;
        }

        [AppColumn("updated", false, true)]
        public DateTime? Updated {
            get;
            set;
        }

        [AppColumn("state")]
        public int State {
            get;
            set;
        }
        #endregion
    }
}
