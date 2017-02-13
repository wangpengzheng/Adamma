using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Reflection;

namespace TFSAdapter
{
    [Flags]
    public enum DateQueryFields
    {
        [Description("EmptyField")]
        EmptyField,

        // Common Fields
        [Description("Created Date")]
        CreatedDate,
        [Description("Accept Date")]
        AcceptDate,
        [Description("ETA")]
        ETADate,
        [Description("Resolved Date")]
        ResolvedDate,
        [Description("Closed Date")]
        ClosedDate,

        // Not open used.
        [Description("State Change Date")]
        StateChangeDate,
        [Description("Activated Date")]
        ActivatedDate,
        [Description("Changed Date")]
        ChangedDate,
        [Description("GoLive Date")]
        GoLiveDate,
        [Description("Revised Date")]
        RevisedDate,
        [Description("SR Opened Date")]
        SROpenedDate,
        [Description("State Transition Date")]
        StateTransitionDate,
    }

    public class BugDateTypeCLS
    {
        public static string GetBugStatusFieldFromEnum(DateQueryFields _dateQueryFields)
        {
            FieldInfo fi = _dateQueryFields.GetType().GetField(_dateQueryFields.ToString());
            DescriptionAttribute[] attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);

            return attributes[0].DescriptionStr.ToString();
        }

        private DateQueryFields curDateQueryField;
        public DateQueryFields CurDateQueryField
        {
            get { return curDateQueryField; }
            set
            {
                if (value == DateQueryFields.EmptyField)
                {
                    curFromDate = DateTime.MinValue;
                    curToDate = DateTime.MinValue;
                }
            }
        }

        private DateTime curFromDate;
        public DateTime CurFromDate
        {
            get { return curFromDate; }
            set { curFromDate = value; }
        }

        private DateTime curToDate;
        public DateTime CurToDate
        {
            get { return curToDate; }
            set { curToDate = value; }
        }

        private Dictionary<DateQueryFields, String> bugDateTypeMap;
        public Dictionary<DateQueryFields, String> BugDateTypeMap
        {
            set { bugDateTypeMap = value; }
            get { return bugDateTypeMap; }
        }

        public BugDateTypeCLS(DateQueryFields _curDateQueryField, DateTime _curFromDate, DateTime _curToDate)
        {
            curFromDate = _curFromDate;
            curToDate = _curToDate;

            curDateQueryField = _curDateQueryField;

            bugDateTypeMap = new Dictionary<DateQueryFields, string>();
            bugDateTypeMap.Add(DateQueryFields.AcceptDate, "[Microsoft.Dynamics.AcceptDate]");
            bugDateTypeMap.Add(DateQueryFields.ActivatedDate, "[Microsoft.VSTS.Common.ActivatedDate]");
            bugDateTypeMap.Add(DateQueryFields.ChangedDate, "[System.ChangedDate]");
            bugDateTypeMap.Add(DateQueryFields.ClosedDate, "[Microsoft.VSTS.Common.ClosedDate]");
            bugDateTypeMap.Add(DateQueryFields.CreatedDate, "[System.CreatedDate]");
            bugDateTypeMap.Add(DateQueryFields.ETADate, "[Microsoft.Dynamics.ETADate]");
            bugDateTypeMap.Add(DateQueryFields.GoLiveDate, "[Microsoft.Dynamics.GoLiveDate]");
            bugDateTypeMap.Add(DateQueryFields.ResolvedDate, "[Microsoft.VSTS.Common.ResolvedDate]");
            bugDateTypeMap.Add(DateQueryFields.RevisedDate, "[System.RevisedDate]");
            bugDateTypeMap.Add(DateQueryFields.SROpenedDate, "[Microsoft.Dynamics.SE.SROpenedDate]");
            bugDateTypeMap.Add(DateQueryFields.StateChangeDate, "[Microsoft.VSTS.Common.StateChangeDate]");
            bugDateTypeMap.Add(DateQueryFields.StateTransitionDate, "[Microsoft.Dynamics.StateTransitionDate]");
        }
    }
}
