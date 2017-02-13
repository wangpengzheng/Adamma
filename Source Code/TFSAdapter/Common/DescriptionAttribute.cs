using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TFSAdapter
{
    public class DescriptionAttribute : Attribute
    {
        private string descriptionStr = null;

        public string DescriptionStr
        {
            get { return descriptionStr; }
            set { descriptionStr = value; }
        }

        public DescriptionAttribute()
        { }

        public DescriptionAttribute(string _description)
        {
            descriptionStr = _description;
        }
    }
}
