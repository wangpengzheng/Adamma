using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace TFSAdapter
{
    public class QuerySQLGenerator
    {
        private Boolean preLoadingSingleBugInfo = false;
        private List<String> tfsFieldsForSingleBug;
        public List<String> tfsFieldsToBeSetInvisiableInGridView;

        private Dictionary<String, int> tfsFieldsWithWidth;
        private List<String> tfsFieldsWithoutWidth;

        private readonly string statusField = "[System.State]";
        private readonly string issueField = "[Microsoft.VSTS.Common.Issue]";

        /// <summary>
        /// Add this fields mandantory, need use it to calculate SLA day.
        /// </summary>
        public List<String> MandantoryFields
        { get; set; }

        //public List<int> AllEnabledFieldsWidthInOrder
        //{ get; set; }

        /// <summary>
        ///  Construct nothing only for the pure SLA query.
        /// </summary>
        public QuerySQLGenerator()
        { }


        public QuerySQLGenerator(Dictionary<String, int> _wiqlFields)
        {
            tfsFieldsWithWidth = _wiqlFields;
            this.initializeValues();
        }

        public QuerySQLGenerator(Dictionary<String, int> _wiqlFields, List<String> _wiqlFieldsForSingleBug, Boolean _preLoadingSingleBugsInfo)
        {
            preLoadingSingleBugInfo = _preLoadingSingleBugsInfo;
            tfsFieldsForSingleBug = _wiqlFieldsForSingleBug;

            tfsFieldsWithWidth = _wiqlFields;
            this.initializeValues();
        }

        public QuerySQLGenerator(List<String> _wiqlFieldsForSignalBug)
        {
            tfsFieldsWithoutWidth = _wiqlFieldsForSignalBug;
            this.initializeValues();
        }

        private void initializeValues()
        {
            //AllEnabledFieldsWidthInOrder = new List<int>();
            tfsFieldsToBeSetInvisiableInGridView = new List<string>();
            MandantoryFields = new List<string>();
            MandantoryFields.Add("System.Id");
            MandantoryFields.Add("System.State");
            MandantoryFields.Add("Microsoft.Dynamics.AcceptDate");
            MandantoryFields.Add("Microsoft.VSTS.Common.Priority");
            MandantoryFields.Add("Microsoft.Dynamics.VendorDaysCredit");

            // For Dev & Test Chart base on the data in grid view.
            MandantoryFields.Add("Microsoft.Dynamics.TestAssigned");
            MandantoryFields.Add("Microsoft.Dynamics.DevAssigned");
        }

        private string headerSQL()
        {
            return "SELECT ";
        }

        #region TFS fields part.
        /// <summary>
        /// Will add the mandatory fields to calculate SLA.
        /// </summary>
        /// <returns></returns>
        private string tfsFieldsSQLFromQuery(String _queryStrFromQuery)
        {
            string fieldsValue = "";

            String orignalFieldsFromQuery = _queryStrFromQuery.Substring(_queryStrFromQuery.IndexOf("SELECT ") + 6, _queryStrFromQuery.IndexOf("FROM") - _queryStrFromQuery.IndexOf("SELECT ") - 7);
            String[] tfsFieldsFromQuery = orignalFieldsFromQuery.Split(',');
            
            string[] mFields = MandantoryFields.ToArray();
            // Add the mandantory fields for SLA calculation.
            foreach (String field in mFields)
            {
                if (!tfsFieldsFromQuery.Contains<String>(" [" + field + "]"))
                {
                    fieldsValue += "[" + field + "], ";
                    tfsFieldsToBeSetInvisiableInGridView.Add(field);
                    //MandantoryFields[field] = true;

                    // Add the mandatory fields 100 each. 
                    //AllEnabledFieldsWidthInOrder.Add(100);
                }
            }

            // Add the fields for singal bug if user choosen the loading mode to preLoading.
            if (this.preLoadingSingleBugInfo)
            {
                String[] sinFields = tfsFieldsForSingleBug.ToArray();
                foreach (String field in sinFields)
                {
                    if (!tfsFieldsWithWidth.Keys.Contains(field) && !MandantoryFields.Contains(field))
                    {
                        fieldsValue += "[" + field + "],";
                        tfsFieldsToBeSetInvisiableInGridView.Add(field);

                        // Add the singal bug fields 100 each. 
                        //AllEnabledFieldsWidthInOrder.Add(100);
                    }
                }
            }

            // Insert the new fields and return.
            return _queryStrFromQuery.Insert(_queryStrFromQuery.IndexOf("SELECT") + 7, fieldsValue);
        }

        /// <summary>
        /// To tfs fields, the conditions are as following,
        /// 1. User defined fields.
        /// 2. Mandantory fields for SLA.
        /// 3. Mandantory fields for Dev & Test Chart.
        /// 
        /// </summary>
        /// <returns></returns>
        private string tfsFieldsSQLFromAdamma()
        {
            string fieldsValue = "";

            string[] mFields = MandantoryFields.ToArray();
            // Add the mandantory fields for SLA calculation if didn't exist in user definition.
            foreach (String field in mFields)
            {
                if (!tfsFieldsWithWidth.Keys.Contains(field))
                {
                    fieldsValue += "[" + field + "],";
                    tfsFieldsToBeSetInvisiableInGridView.Add(field);
                    //MandantoryFields[field] = true;

                    // Add the mandatory fields 100 each. 
                    //AllEnabledFieldsWidthInOrder.Add(100);
                }
            }

            // Add the fields user choosen by default.
            foreach (String field in tfsFieldsWithWidth.Keys.ToArray())
            {
                fieldsValue += "[" + field + "],";
                //AllEnabledFieldsWidthInOrder.Add(tfsFieldsWithWidth[field]);
            }

            // Add the fields for singal bug if user choosen the loading mode to preLoading.
            if (this.preLoadingSingleBugInfo)
            {
                String[] sinFields = tfsFieldsForSingleBug.ToArray();
                foreach (String field in sinFields)
                {
                    if (!tfsFieldsWithWidth.Keys.Contains(field) && !MandantoryFields.Contains(field))
                    {
                        fieldsValue += "[" + field + "],";
                        tfsFieldsToBeSetInvisiableInGridView.Add(field);

                        // Add the singal bug fields 100 each. 
                        //AllEnabledFieldsWidthInOrder.Add(100);
                    }
                }
            }

            fieldsValue = fieldsValue.Remove(fieldsValue.Count() - 1);
            fieldsValue += " FROM WorkItems WHERE ";
            return fieldsValue;
        }

        private string tfsFieldsSQLForPureSLA()
        {
            List<String> fieldsForPureSLA = new List<string>();
            //fieldsForPureSLA.Add("System.Id");
            fieldsForPureSLA.Add("System.State");
            fieldsForPureSLA.Add("Microsoft.Dynamics.AcceptDate");
            fieldsForPureSLA.Add("Microsoft.VSTS.Common.Priority");
            fieldsForPureSLA.Add("Microsoft.Dynamics.VendorDaysCredit");

            string fieldsValue = "";

            foreach (String field in fieldsForPureSLA)
            {
                fieldsValue += "[" + field + "],";
            }

            fieldsValue = fieldsValue.Remove(fieldsValue.Count() - 1);
            fieldsValue += " FROM WorkItems WHERE ";

            return fieldsValue;
        }

        private string tfsFieldsSQLForSingleBug()
        {
            string fieldsValue = "";

            foreach (String field in tfsFieldsWithoutWidth)
            {
                fieldsValue += "[" + field + "],";
            }

            fieldsValue = fieldsValue.Remove(fieldsValue.Count() - 1);
            fieldsValue += " FROM WorkItems WHERE ";
            return fieldsValue;
        }
        #endregion TFS fields part.

        #region Expression part.
        private string expressionSQLFromTeam(Dictionary<String, String> _teamMembers)
        {
            if (_teamMembers == null || _teamMembers.Count == 0)
                return "";

            string memberExpression = "";

            foreach (String member in _teamMembers.Keys)
            {
                switch (_teamMembers[member].ToUpper())
                {
                    case "DEV":
                        {
                            memberExpression += "[Microsoft.Dynamics.DevAssigned] = '" + member + "'";
                            break;
                        }
                    case "TEST":
                    case "TESTER":
                        {
                            memberExpression += "[Microsoft.Dynamics.TestAssigned] = '" + member + "'";
                            break;
                        }
                    case "PM":
                        {
                            memberExpression += "[Microsoft.Dynamics.PMAssigned] = '" + member + "'";
                            break;
                        }
                }

                memberExpression += " or ";
            }
            memberExpression = memberExpression.Remove(memberExpression.Count() - 4);
            memberExpression = "(" + memberExpression + ") ";

            return memberExpression;
        }

        private string addtionalExpressionOfTypeAndStates(BugType _bugTypes, BugStatus _bugState, BugDateTypeCLS _bugDateType)
        {
            string expressionStr = "";
            string bugTypeStr = "";
            string bugStatesStr = "";
            string bugDateStr = "";

            if (_bugTypes != BugType.EmptyBugType)
            {
                foreach (BugType bugType in Enum.GetValues(typeof(BugType)))
                {
                    if (bugType != BugType.EmptyBugType)
                    {
                        if ((_bugTypes & bugType) == bugType)
                        {
                            bugTypeStr += issueField + " = '" + BugTypeCLS.GetBugStatusFieldFromEnum(bugType) + "' or ";
                        }
                    }
                }
                bugTypeStr = bugTypeStr.Remove(bugTypeStr.Count() - 3);
                expressionStr = "and (" + bugTypeStr + ")";
            }

            if (_bugState != BugStatus.EmptyBugStatus)
            {
                switch (_bugState)
                {
                    case BugStatus.ActiveBugs:
                        {
                            bugStatesStr = "[System.State] <> 'Resolved' and [System.State] <> 'Closed'";
                            break;
                        }
                    case BugStatus.ResolvedColsedBug:
                        {
                            bugStatesStr = "[System.State] = 'Closed' or [System.State] = 'Resolved'";
                            break;
                        }
                    default:
                        {
                            bugStatesStr = statusField + " = '" + BugStatusCLS.GetBugStatusFieldFromEnum(_bugState) + "'";
                            break;
                        }
                }
                expressionStr += " and (" + bugStatesStr + ")";
            }

            if (_bugDateType != null && _bugDateType.CurDateQueryField != DateQueryFields.EmptyField)
            {
                if (_bugDateType.CurFromDate != DateTime.MinValue)
                {
                    bugDateStr += _bugDateType.BugDateTypeMap[_bugDateType.CurDateQueryField] + " >= '" +
                            _bugDateType.CurFromDate + "'";
                }

                if (_bugDateType.CurToDate != DateTime.MinValue)
                {
                    if (bugDateStr.Trim() != "")
                        bugDateStr += " and ";

                    bugDateStr += _bugDateType.BugDateTypeMap[_bugDateType.CurDateQueryField] + " <= '" +
                            _bugDateType.CurToDate + "'";
                }

                if (bugDateStr != "")
                    expressionStr += " and (" + bugDateStr + ")";
            }

            return expressionStr;
        }

        private string expressionSQLFromWiql(String _wiqlStr)
        {
            return _wiqlStr.Substring(_wiqlStr.IndexOf("WHERE") + 5, _wiqlStr.IndexOf("ORDER") - _wiqlStr.IndexOf("WHERE") - 6);
        }

        private string expressionSQLByBugID(int _bugID, Boolean _isTFSID = true)
        {
            String bugIdExpression;

            if (_isTFSID)
            {
                bugIdExpression = "[System.Id] ='" + _bugID.ToString() + "'";
            }
            else
            {
                bugIdExpression = "[Microsoft.Dynamics.BugID] ='" + _bugID.ToString() + "'";
            }

            return bugIdExpression;
        }
        #endregion Expression part.

        #region Order by part.
        private string orderBySQL()
        {
            return " ORDER BY [System.Id]";
        }
        #endregion Order by part.

        private string ConstructSQLTextFromQuery(String _sQLFromQuery, Boolean _useTFSFieldsFromAdamma = true)
        {
            if (!_useTFSFieldsFromAdamma)
            {
                // Add mandantory fields for SLA calculation.
                return this.tfsFieldsSQLFromQuery(_sQLFromQuery);
            }
            else
            {
                return this.headerSQL()
                    + this.tfsFieldsSQLFromAdamma()
                    + this.expressionSQLFromWiql(_sQLFromQuery)
                    + this.orderBySQL();
            }
        }

        private string ConstructSQLTextFromTeam(Dictionary<String, String> _teamMembers, BugType _bugTypes, BugStatus _bugState, BugDateTypeCLS _bugDateType)
        {
            return this.headerSQL()
                + this.tfsFieldsSQLFromAdamma()
                + this.expressionSQLFromTeam(_teamMembers)
                + this.addtionalExpressionOfTypeAndStates(_bugTypes, _bugState, _bugDateType)
                + this.orderBySQL();
        }

        private string ConstructSQLTextFromChartGroup(Dictionary<String, String> _teamMembers, BugType _bugTypes, BugStatus _bugState, BugDateTypeCLS _bugDateType)
        {
            return this.headerSQL()
                + this.tfsFieldsSQLForPureSLA()
                + this.expressionSQLFromTeam(_teamMembers)
                + this.addtionalExpressionOfTypeAndStates(_bugTypes, _bugState, _bugDateType)
                + this.orderBySQL();
        }

        private string ConstructSQLTextByBugID(int _bugID, Boolean _isTFSID = true)
        {
            return this.headerSQL()
                + this.tfsFieldsSQLForSingleBug()
                + this.expressionSQLByBugID(_bugID, _isTFSID);
        }

        public string ConstructSQLFromTypeAndExpression(QueryType _queryType, ExpressionController _expression)
        {
            if (_queryType == QueryType.NullType || _expression == null)
            {
                Debug.Assert(_expression != null, "Expression object is null!");
                return "";
            }

            string targetStr = "";

            switch (_queryType)
            {
                case QueryType.WiqSqlQuery:
                    {
                        targetStr = this.ConstructSQLTextFromQuery(
                        _expression.SQLFromQuery,
                        _expression.UseTFSFieldsFromAdamma);
                        break;
                    }
                case QueryType.TeamQuery:
                    {
                        targetStr = this.ConstructSQLTextFromTeam(
                        _expression.TeamMembers,
                        _expression.BugTypes,
                        _expression.BugState,
                        _expression.BugDateType);
                        break;
                    }
                case QueryType.ChartGroupQuery:
                    {
                        targetStr = this.ConstructSQLTextFromChartGroup(
                        _expression.TeamMembers,
                        _expression.BugTypes,
                        _expression.BugState,
                        _expression.BugDateType);
                        break;
                    }
                case QueryType.SignalBugQuery:
                    {
                        targetStr = this.ConstructSQLTextByBugID(_expression.BugID,
                        _expression.IsTFSField);
                        break;
                    }
                default: return "";
            }

            return targetStr;
        }

        public List<string> _wiqlFieldsForSingleBug { get; set; }
    }
}
