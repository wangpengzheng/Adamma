using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;

namespace TFSAdapter
{
    /// <summary>
    /// http://msdn.microsoft.com/en-us/library/cc138362.aspx
    /// </summary>
    [Flags]
    public enum BugType
    {
        [Description("Empty Bug Type")]
        EmptyBugType = 0x0,
        [Description("Request for Hotfix")] 
        RequestforHotfix = 0x1,
        [Description("Code Defect")]
        CodeDefect = 0x2,
        [Description("Over Layering Issue")] 
        OverLayeringIssue = 0x4,
        [Description("Collaboration Request")] 
        CollaborationRequest = 0x8,
        [Description("Design Change Request")] 
        DesignChangeRequest = 0x10,
        [Description("Test")] 
        Test = 0x20
    }

    public class BugTypeCLS
    {
        /// <summary>
        /// http://stackoverflow.com/questions/1799370/getting-attributes-of-enums-value
        /// http://www.codekeep.net/snippets/c5117033-256f-4993-9b59-0dd5f48f4c18.aspx
        /// </summary>
        /// <param name="_bugType"></param>
        /// <returns></returns>
        public static string GetBugStatusFieldFromEnum(BugType _bugType)
        {
            #region Solution 1
            //string strField = "";
            //switch (_bugType)
            //{
            //    case BugType.RequestforHotfix: strField = "Request for Hotfix"; break;
            //    case BugType.CodeDefect: strField = "Code Defect"; break;
            //    case BugType.OverLayeringIssue: strField = "Over Layering Issue"; break;
            //    case BugType.CollaborationRequest: strField = "Collaboration Request"; break;
            //    case BugType.DesignChangeRequest: strField = "Design Change Request"; break;
            //    case BugType.Test: strField = "Test"; break;
            //}

            //return strField;
            #endregion

            #region Solution 2   Exist Wrong. 
            //var type = typeof(BugType);
            //var memInfo = type.GetMember(_bugType.ToString());
            //var attributes = memInfo[0].GetCustomAttributes(typeof(BugType), false);
            //var description = ((DescriptionAttribute)attributes[0]).DescriptionStr;

            //return description.ToString();
            #endregion

            #region Solution 3
            FieldInfo fi = _bugType.GetType().GetField(_bugType.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes[0].DescriptionStr.ToString();
            #endregion
        }

        /// <summary>
        /// Convert a bugtype to a formate : 000101
        /// 0 and 1 represent enable or not to each enum status.
        /// </summary>
        /// <returns></returns>
        public static String ConvertBugTypesToStrings(BugType _bugTypes)
        {
            string bugTypeStr = "";

            if (_bugTypes == BugType.EmptyBugType)
            {
                bugTypeStr = "1000000";
            }
            else
            {
                foreach (BugType bugType in Enum.GetValues(typeof(BugType)))
                {
                    if (((_bugTypes & bugType) == bugType) && (bugType != BugType.EmptyBugType))
                    {
                        bugTypeStr += "1";
                    }
                    else
                    {
                        bugTypeStr += "0";
                    }
                }
            }

            return bugTypeStr;
        }
    }
}
