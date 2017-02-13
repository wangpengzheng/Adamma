using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace TFSAdapter
{
    public enum BugStatus
    {
        EmptyBugStatus,

        ActiveBugs,
        ResolvedColsedBug,

        [Description("Pre-Triage")]
        PreTriage,
        [Description("Repro")]
        Repro,
        [Description("Investigate")]
        Investigate,
        [Description("Fixing")]
        Fixing,
        [Description("Code Review")]
        CodeReview,
        [Description("Code Impact Review")]
        CodeImpactReview,
        [Description("Private Test")]
        PrivateTest,
        [Description("PM Review")]
        PMReview,
        [Description("Test Spec Review")]
        TestSpecReview,
        [Description("Checkin and Build")]
        CheckinandBuild,
        [Description("Official Test")]
        OfficialTest,
        [Description("Active")]
        Active,
        [Description("Release")]
        Release,
        [Description("Closed")]
        Closed
    }

    public class BugStatusCLS
    {
        public static string GetBugStatusFieldFromEnum(BugStatus _bugStatus)
        {
            if (_bugStatus == BugStatus.ActiveBugs || _bugStatus == BugStatus.ResolvedColsedBug)
                return "";

            #region Solution 1
            //string strField = "";
            //switch (_bugStatus)
            //{
            //    case BugStatus.PreTriage: strField = "Pre-Triage"; break;
            //    case BugStatus.Repro: strField = "Repro"; break;
            //    case BugStatus.Investigate: strField = "Investigate"; break;
            //    case BugStatus.Fixing: strField = "Fixing"; break;
            //    case BugStatus.CodeReview: strField = "Code Review"; break;
            //    case BugStatus.CodeImpactReview: strField = "Code Impact Review"; break;
            //    case BugStatus.PrivateTest: strField = "Private Test"; break;
            //    case BugStatus.PMReview: strField = "PM Review"; break;
            //    case BugStatus.TestSpecReview: strField = "Test Spec Review"; break;
            //    case BugStatus.CheckinandBuild: strField = "Checkin and Build"; break;
            //    case BugStatus.OfficialTest: strField = "Official Test"; break;
            //    case BugStatus.Active: strField = "Active"; break;
            //    case BugStatus.Release: strField = "Release"; break;
            //    case BugStatus.Closed: strField = "Closed"; break;
            //}

            //return strField;
            #endregion 

            #region Solution 2
            //var type = typeof(BugStatus);
            //var memInfo = type.GetMember(_bugStatus.ToString());
            //var attributes = memInfo[0].GetCustomAttributes(typeof(BugStatus), false);
            //var description = ((DescriptionAttribute)attributes[0]).DescriptionStr;

            //return description.ToString();
            #endregion 

            #region Solution 2
            FieldInfo fi = _bugStatus.GetType().GetField(_bugStatus.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes[0].DescriptionStr.ToString();
            #endregion
        }
    }
}
