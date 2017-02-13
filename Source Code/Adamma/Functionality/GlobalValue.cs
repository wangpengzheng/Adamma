using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using TFSAdapter;
using WorkShop;
using System.IO;
using ZedGraph;

namespace Adamma
{
    public enum QueryProduct
    {
        [Description("DAXSE")]
        DAXSE,
        [Description("AXSE")]
        AXSE,
        [Description("AX6")]
        AX6
    }

    public enum AsyncLoadingType
    {
        TeamBugs,
        QueryBugs,
        SignleBug
    }

    public class GlobalValue
    {
        public static String curSelectedTFSID = "";
        public static String curSelectedPSID = "";
        public static Boolean isTFSID = true;

        // To map the absolutly address to its related address
        public static Dictionary<String, String> absoluteToRelatedQueryFileAddressMap;

        // For go to bug, history setting part.
        public static QueryProduct historyQueryProduct = QueryProduct.DAXSE;

        public GlobalValue(Setting _setupInfo)
        {
            breakSLA = 0;
            lessThanTen = 0;
            lessThanTwenty = 0;
            lessThanSixty = 0;
            noSLABugNum = 0;
            ClosedOrResolvedBugNum = 0;

            lookUpForGridBug = new LookUpController();
            teamChartGroupControl = new TeamChartGroupController();
            GridViewBugsDT = new DataTable();
            ChartDT = new DataTable();
            CharGroupDT = new DataTable();

            // For chart group 
            ChartGroupBartype = BarType.Cluster;

            #region default fields to one bug in Adamma,
            WiqFieldsForFormControls = new List<String>();
            DisFieldsForFormControls = new List<String>();
            absoluteToRelatedQueryFileAddressMap = new Dictionary<string, string>();

            // Page 1, Common Part.
            WiqFieldsForFormControls.Add("System.Title");
            WiqFieldsForFormControls.Add("System.AreaPath");
            WiqFieldsForFormControls.Add("System.Id");
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.BugID");
            WiqFieldsForFormControls.Add("System.History");
            WiqFieldsForFormControls.Add("System.State");
            WiqFieldsForFormControls.Add("Microsoft.VSTS.Common.Issue");
            WiqFieldsForFormControls.Add("Microsoft.VSTS.Common.Priority");
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.AcceptDate");
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.VendorDaysCredit");
            WiqFieldsForFormControls.Add("Microsoft.VSTS.Common.Version");
            
            WiqFieldsForFormControls.Add("System.AssignedTo");
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.PMAssigned");
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.TestAssigned");
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.DevAssigned");
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.EscalationEngineerAssigned");
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.TestReviewer");
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.CodeReviewer");

            // Page 2, EE part.
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.BusinessImpact");
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.BusinessProblemDescription");
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.TechnicalDescription");

            //Dev side
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.BusinessProblem");            
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.Solution");

            WiqFieldsForFormControls.Add("Microsoft.Dynamics.RiskCategory");
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.RiskDescription");
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.FixImpact");
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.FixImpactDescription");            
            WiqFieldsForFormControls.Add("Microsoft.Dynamics.SE.TestRecommendation");
            
            DisFieldsForFormControls = CommonUtilities.GetDisplayNamesThoughFieldName(_setupInfo.TFSFieldsValue, WiqFieldsForFormControls, true);
            #endregion

            CurBugStatus = _setupInfo.AdammaSetting.HistorySetting.HistoryLoadingBugStatus;
            //CurBugType = _setupInfo.AdammaSetting.HistorySetting.HistoryLoadingBugType;

            // Control the loading process from Configure.
            if (_setupInfo.AdammaSetting.ProSetting.CustomizeGridViewLoading)
            {
                if (_setupInfo.AdammaSetting.ProSetting.LoadingFromDefaultTeam)
                {
                    CurAsyncLoadingType = AsyncLoadingType.TeamBugs;
                    CurrentTeam = _setupInfo.AdammaSetting.ProSetting.DefaultTeamNameForLoad;
                }
                else if (_setupInfo.AdammaSetting.ProSetting.LoadingFromQuery)
                {
                    CurAsyncLoadingType = AsyncLoadingType.QueryBugs;
                    CurrentQueryFileLocation = _setupInfo.AdammaSetting.ProSetting.DefaultQueryForLoad;
                    CurrentTeam = _setupInfo.AdammaSetting.ProSetting.DefaultTeamNameForLoad;

                    // Reset the query if query file didn't exist.
                    if (!File.Exists(CurrentQueryFileLocation))
                    {
                        CurAsyncLoadingType = AsyncLoadingType.TeamBugs;
                        CurrentTeam = _setupInfo.AdammaSetting.HistorySetting.HistoryLoadingTeam;
                        CurrentQueryFileLocation = "";
                        _setupInfo.AdammaSetting.ProSetting.DefaultQueryForLoad = "";
                        _setupInfo.AdammaSetting.ProSetting.CustomizeGridViewLoading = false;
                    }
                }
                else
                {
                    CurAsyncLoadingType = AsyncLoadingType.TeamBugs;
                    CurrentTeam = _setupInfo.AdammaSetting.HistorySetting.HistoryLoadingTeam;
                }
            }
            else
            {
                // If user do not customized the loading team, load the last team user selected by default.
                CurAsyncLoadingType = AsyncLoadingType.TeamBugs;
                CurrentTeam = _setupInfo.AdammaSetting.HistorySetting.HistoryLoadingTeam;
            }            
        }

        public AsyncLoadingType CurAsyncLoadingType
        { get; set; }

        /// <summary>
        /// Add this fields mandantory, need use it to calculate SLA day. And Identify a bug(Id). 
        /// true means the add without user setup.
        /// </summary>
        public List<String> MandantoryFields
        { get; set; }

        public List<String> TfsFieldsToBeSetInvisiableInGridView
        {get; set;}

        public List<String> WiqFieldsForFormControls
        { get; set; }

        public List<String> DisFieldsForFormControls
        { get; set; }

        //public List<int> AllEnabledFieldsWidthInOrder
        //{ get; set; }

        public static Boolean loadDataFinish = false;
        public String CurrentTeam;
        public String CurrentQueryFileLocation = "";
        public BugType CurBugType;
        public BugStatus CurBugStatus;
        public BugDateTypeCLS CurBugDateType;

        #region this datatable is from the orign queryDT, it used to display in datagridview.
        // Synchornize the data in lookup class to ensure look up info is up-to-date.
        public LookUpController lookUpForGridBug;
        public TeamChartGroupController teamChartGroupControl;

        public DataTable TmpGridviewBugDTBeforeSort;
        private DataTable gridViewBugsDT;
        public DataTable GridViewBugsDT
        {
            // Update the source data in the filter.
            set 
            {
                gridViewBugsDT = value;
                lookUpForGridBug.GridViewBugsDT = value; 
            }
            get { return gridViewBugsDT; }
        }

        public DataTable ChartDT;
        public DataTable CharDTForSignalBug;
        public DataTable CharGroupDT;

        public BarType ChartGroupBartype;
        #endregion
        
        #region Query expression user choosen
        public BugType BugTypeChosen
        { get; set; }

        public BugStatus BugStatusChosen
        { get; set; }

        public String TeamChosen
        { get; set; }
        #endregion

        #region The number of bugs of each status.
        public int breakSLA
        { get; set; }

        public int lessThanTen
        { get; set; }

        public int lessThanTwenty
        { get; set; }

        public int lessThanSixty
        { get; set; }

        public int noSLABugNum
        { get; set; }

        public int ClosedOrResolvedBugNum
        { get; set; }
        #endregion
    }
}
