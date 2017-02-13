using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace TFSAdapter
{
    public enum QueryType
    {
        NullType,
        TeamQuery,
        ChartGroupQuery,
        WiqSqlQuery,
        SignalBugQuery
    }

    /// <summary>
    /// This class contains the expression needs to query TFS for one team. Should be updated each time update expresion.
    /// The expressions need are,
    /// 1. Team for query.
    /// 2. Bug type for query.
    /// 3. Bug status for query.
    /// 4. BugDateType for query. (Optional)
    /// </summary>
    public class ExpressionController
    {
        #region For QueryType.WiqSqlQuery
        protected String sqlFromQuery;
        public String SQLFromQuery
        {
            get { return sqlFromQuery; }
            set { sqlFromQuery = value; }
        }

        protected Boolean useTFSFieldsFromAdamma;
        public Boolean UseTFSFieldsFromAdamma
        {
            get { return useTFSFieldsFromAdamma; }
            set { useTFSFieldsFromAdamma = value; }
        }
        #endregion

        #region  For QueryType.SignalBugQuery
        protected int bugID;
        public int BugID
        {
            get { return bugID; }
            set { bugID = value; }
        }

        protected Boolean isTFSField;
        public Boolean IsTFSField
        {
            get { return isTFSField; }
            set { isTFSField = value; }
        }
        #endregion

        #region For QueryType.TeamQuery
        protected Dictionary<String, String> teamMembers;
        public Dictionary<String, String> TeamMembers
        {
            get { return teamMembers; }
            set { teamMembers = value; }
        }

        protected BugType bugTypes;
        public BugType BugTypes
        {
            get { return bugTypes; }
            set { bugTypes = value; }
        }

        protected BugStatus bugState;
        public BugStatus BugState
        {
            get { return bugState; }
            set { bugState = value; }
        }

        protected BugDateTypeCLS bugDateType;
        public BugDateTypeCLS BugDateType
        {
            get { return bugDateType; }
            set { bugDateType = value; }
        }

        #endregion

        public QueryType CurQueryType;

        public ExpressionController(QueryType _queryType)
        {
            CurQueryType = _queryType;
        }

        public void setExpressionForWiqSqlQuery(String _sQLFromQuery, Boolean _useTFSFieldsFromAdamma = true)
        {
            if (CurQueryType != QueryType.WiqSqlQuery)
                Debug.Assert(CurQueryType != QueryType.WiqSqlQuery, "Wrong implement method setExpressionForWiqSqlQuery");

            this.sqlFromQuery = _sQLFromQuery;
            this.useTFSFieldsFromAdamma = _useTFSFieldsFromAdamma;
        }

        public void setExpressionForSingalBug(int _bugID, Boolean _isTFSID = true)
        {
            if (CurQueryType != QueryType.SignalBugQuery)
                Debug.Assert(CurQueryType != QueryType.SignalBugQuery, "Wrong implement method setExpressionForSingalBug");

            this.bugID = _bugID;
            this.isTFSField = _isTFSID;
        }

        /// <summary>
        /// If true the tfs fields used for query will only contians "AcceptDate", "Priority", "VendDaysCredit."
        /// </summary>
        /// <param name="_teamMembers"></param>
        /// <param name="_bugTypes"></param>
        /// <param name="_bugState"></param>
        /// <param name="_bugDateType"></param>
        /// <param name="_forPureSLACalculateOnly"></param>
        public void setExpression(Dictionary<String, String> _teamMembers,
                                 BugType _bugTypes,
                                 BugStatus _bugState,
                                 BugDateTypeCLS _bugDateType)
        {
            if (CurQueryType != QueryType.TeamQuery)
                Debug.Assert(CurQueryType != QueryType.TeamQuery, "Wrong implement method setExpressionForTeamQuery");

            this.teamMembers = _teamMembers;
            this.bugTypes = _bugTypes;
            this.bugState = _bugState;
            this.bugDateType = _bugDateType;
        }
    }
}
