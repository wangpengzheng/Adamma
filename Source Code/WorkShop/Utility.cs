using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WorkShop;
using System.Data;
using System.IO;
using System.Xml;
using System.Diagnostics;

namespace WorkShop
{
    public static class CommonUtilities
    {
        public static List<String> ConstructSqlTxtFromWiq(string _wiqPath)
        {
            if (!File.Exists(_wiqPath))
                return null;

            List<String> txtForConstruct = new List<String>();
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(_wiqPath);

            XmlElement xRoot = xmldoc.DocumentElement;
            XmlNode nodeServer = xRoot.SelectSingleNode("/WorkItemQuery/TeamFoundationServer");
            XmlNode nodeTeamProject = xRoot.SelectSingleNode("/WorkItemQuery/TeamProject");
            XmlNode nodeWiql = xRoot.SelectSingleNode("/WorkItemQuery/Wiql");

            txtForConstruct.Add(nodeServer.InnerText);
            txtForConstruct.Add(nodeTeamProject.InnerText);
            txtForConstruct.Add(nodeWiql.InnerText);

            return txtForConstruct;
        }

        public static String getSqlTxtFromWiq(String _wiqPath)
        {
            if (!File.Exists(_wiqPath))
                return null;

            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(_wiqPath);
            XmlElement xRoot = xmldoc.DocumentElement;
            XmlNode nodeWiql = xRoot.SelectSingleNode("/WorkItemQuery/Wiql");

            return nodeWiql.InnerText;
        }

        public static TFSFields updateTFSFieldsAndSortNum(TFSFields _tfsFields, List<String> _enabledFields)
        {
            int i = 1;
            foreach (String field in _enabledFields)
            {
                var match = from find in _tfsFields.Fields
                            where find.FieldName == field
                            select find;

                match.First().FieldNum = i++;
                match.First().Enabled = true;
            }

            foreach (TFSField field in _tfsFields.Fields)
            {
                if (!_enabledFields.Contains(field.FieldName))
                {
                    field.Enabled = false;
                    field.FieldNum = -1;
                }
            }
            return _tfsFields;
        }

        /// <summary>
        /// Get the display names list through tfs fields name list.
        /// </summary>
        /// <param name="tFSFields">All tfs fields.</param>
        /// <param name="_fieldsName">The fields list to get the correspondent names of their display name.</param>
        /// <param name="_isWiqName">True if the name is a wiq name</param>
        /// <returns></returns>
        public static List<String> GetDisplayNamesThoughFieldName(TFSFields tFSFields, List<String> _fieldsName, Boolean _isWiqName = true)
        {
            if (tFSFields == null || _fieldsName == null)
                return null;

            List<String> disFieldNames = new List<string>();
            foreach (String field in _fieldsName)
            {
                String realField = "";
                if (_isWiqName)
                    realField = field.Substring(field.LastIndexOf(".") + 1, field.Length - field.LastIndexOf(".") - 1);
                else
                    realField = field;

                var result = from value in tFSFields.Fields
                             where value.FieldName == realField
                             select value.FieldDisName;
                               
                if (result.Count() != 0)
                {
                    disFieldNames.Add(result.First());
                }
            }

            return disFieldNames;
        }

        /// <summary>
        /// Use to convert String to enum "BugStatus","BugType" and "QueryProduct".
        /// </summary>
        /// <typeparam name="T">"BugStatus","BugType","QueryProduct"</typeparam>
        /// <param name="_value">String value which used to convert to Enum</param>
        /// <returns></returns>
        public static T ParseNum<T>(String _value)
        {
            if (_value == "")
                return default(T);

            return (T)Enum.Parse(typeof(T), _value, true);
        }
    }

    public class Setting
    {
        private String settingFileLocation;
        public String SettingFileLocation
        {
            get { return settingFileLocation; }
            set { settingFileLocation = value; }
        }

        private String queryLocation;
        public String QueryLocation
        {
            get { return queryLocation; }
            set
            {
                queryLocation = value;
                adammaSetting.InfoLocation.QueryRootFolder = value;
                adammaSetting.saveSetupInfo(settingFileLocation);
            }
        }

        private String teamInfoFileLocation;
        public String TeamInfoFileLocation
        {
            get { return teamInfoFileLocation; }
            set
            {
                teamInfoFileLocation = value;
                adammaSetting.InfoLocation.TeamInfoLoc = value;
                adammaSetting.saveSetupInfo(settingFileLocation);
            }
        }

        private String tfsFieldInfoLoc;
        public String TFSFieldInfoLoc
        {
            get { return tfsFieldInfoLoc; }
        }

        private AdammaSetting adammaSetting;
        public AdammaSetting AdammaSetting
        {
            get { return adammaSetting; }
            set { adammaSetting = value; }
        }

        private TeamInfo teamInfo;
        public TeamInfo TeamInfo
        {
            get { return teamInfo; }
            set
            {
                teamInfo = value;

                // Update the related team names.
                List<Team> groups = teamInfo.GetAllGroups();
                teamNames.Clear();
                foreach (Team g in groups)
                {
                    teamNames.Add(g.Name);
                }
            }
        }

        private TFSFields tFSFields;
        public TFSFields TFSFieldsValue
        {
            get { return tFSFields; }
            set
            {
                tFSFields = value;
            }
        }

        private DataTable teamInfoDT;
        public DataTable TeamInfoDT
        {
            get { return teamInfoDT; }
        }

        private List<String> teamNames;
        public List<String> TeamNames
        {
            get { return teamNames; }
        }

        /// <summary>
        /// Get the setting from Config file. If didn't exist. Create correspondent file to default places.
        /// 1. Query file location.
        /// 2. Team setup file location.
        /// 
        /// Provide the TeamInfo with DT and class Style.
        /// </summary>
        public Setting()
        {
            settingFileLocation = (String)new DefaultSettingValues(LocType.SettingFile).Value;
            adammaSetting = AdammaSetting.LoadSetupInfo(settingFileLocation);

            if (adammaSetting.InfoLocation.QueryRootFolder != "" && Directory.Exists(adammaSetting.InfoLocation.QueryRootFolder))
            {
                queryLocation = adammaSetting.InfoLocation.QueryRootFolder;
            }
            else
            {
                queryLocation = (String)new DefaultSettingValues(LocType.QueryRootFolder).Value;
                adammaSetting.InfoLocation.QueryRootFolder = queryLocation;
                adammaSetting.saveSetupInfo(settingFileLocation);
            }

            if (adammaSetting.InfoLocation.TeamInfoLoc != "" && File.Exists(adammaSetting.InfoLocation.TeamInfoLoc))
            {
                teamInfoFileLocation = adammaSetting.InfoLocation.TeamInfoLoc;
            }
            else
            {
                teamInfoFileLocation = (String)new DefaultSettingValues(LocType.TeamInfo).Value;
                adammaSetting.InfoLocation.TeamInfoLoc = teamInfoFileLocation;
                adammaSetting.saveSetupInfo(settingFileLocation);
            }

            if (adammaSetting.InfoLocation.FieldsLoc != "" && File.Exists(adammaSetting.InfoLocation.FieldsLoc))
            {
                tfsFieldInfoLoc = adammaSetting.InfoLocation.FieldsLoc;
            }
            else
            {
                tfsFieldInfoLoc = (String)new DefaultSettingValues(LocType.FieldsFile).Value;
                adammaSetting.InfoLocation.FieldsLoc = tfsFieldInfoLoc;
                adammaSetting.saveSetupInfo(settingFileLocation);
            }

            teamInfo = TeamInfo.LoadConfiguration(teamInfoFileLocation);
            teamInfoDT = changeTeamInfoToDataTable(teamInfo);
            TFSFieldsValue = TFSFields.LoadTFSFields(tfsFieldInfoLoc);
        }

        private DataTable changeTeamInfoToDataTable(TeamInfo _teamInfo)
        {
            List<Team> groups = teamInfo.GetAllGroups();
            teamNames = new List<String>();

            DataTable dtForDGV = new DataTable();
            dtForDGV.Columns.Add("MemberName", typeof(string));
            dtForDGV.Columns.Add("Alias", typeof(string));
            dtForDGV.Columns.Add("Role", typeof(string));
            dtForDGV.Columns.Add("Team", typeof(string));
            dtForDGV.Columns.Add("Enabled", typeof(int));

            if (groups == null)
                return dtForDGV;

            foreach (Team g in groups)
            {
                foreach (Member m in g.Members)
                {
                    DataRow dr = dtForDGV.NewRow();
                    dr["MemberName"] = m.MemberName;
                    dr["Alias"] = m.Alias;
                    dr["Role"] = m.Role;
                    dr["Team"] = g.Name;
                    dr["Enabled"] = m.Enable;
                    dtForDGV.Rows.Add(dr);
                }
                // Add Team Names to Combobox.
                teamNames.Add(g.Name);
            }

            return dtForDGV;
        }
    }

    public class DefaultSettingValues
    {
        private readonly LocType type;
        private object valueLocal;

        public object Value
        {
            get { return valueLocal; }
            set { valueLocal = value; }
        }
        public DefaultSettingValues(LocType _Type)
        {
            type = _Type;

            String programRootFolder = AppDomain.CurrentDomain.BaseDirectory + "Config";
            switch (type)
            {
                case LocType.SettingFile:
                    {
                        valueLocal = programRootFolder + @"\AdammaSetup.info";
                        if (!File.Exists(valueLocal.ToString()))
                        {
                            String queryLocation = (String)new DefaultSettingValues(LocType.QueryRootFolder).Value;
                            String teamInfoFileLocation = (String)new DefaultSettingValues(LocType.TeamInfo).Value;
                            Boolean startWithWindows = true;

                            AdammaSetting config = new AdammaSetting();
                            config.InfoLocation.QueryRootFolder = queryLocation;
                            config.InfoLocation.TeamInfoLoc = teamInfoFileLocation;
                            config.ProSetting.StartWithWindows = startWithWindows;

                            config.saveSetupInfo(valueLocal.ToString());
                        }
                        break;
                    }
                case LocType.QueryRootFolder:
                    {
                        valueLocal = programRootFolder + @"\UserQuery";
                        if (!Directory.Exists(valueLocal.ToString()))
                            Directory.CreateDirectory(valueLocal.ToString());
                        break;
                    }
                case LocType.TeamInfo:
                    {
                        valueLocal = programRootFolder + @"\TeamInfo.config";
                        if (!File.Exists(valueLocal.ToString()))
                        {
                            TeamInfo newTeamInfo = new TeamInfo();
                            newTeamInfo.SaveConfiguration(valueLocal.ToString());
                        }
                        break;
                    }
                case LocType.FieldsFile:
                    {
                        valueLocal = programRootFolder + @"\TFSFields.config";
                        if (!File.Exists(valueLocal.ToString()))
                        {
                            TFSFields tfsFields = new TFSFields();
                            tfsFields = importTFSFieldData.importFieldsData();
                            tfsFields.SaveTFSFields(valueLocal.ToString());
                        }
                        break;
                    }
            }
        }
    }

    /// <summary>
    /// Control the Team infomation with the GridView.
    /// </summary>
    public class TeamSourceEdit
    {
        DataTable dtOriginal;
        String curEditTeam = "";

        public TeamSourceEdit(DataTable _dtForAllTeam)
        {
            dtOriginal = _dtForAllTeam;
        }

        public DataTable SortTableThroughTeamName(String _teamName = "")
        {
            curEditTeam = _teamName;

            if (_teamName.Trim() == "")
                return dtOriginal;
            else
            {
              if (dtOriginal.Select("Team = '" + curEditTeam + "'").Count() == 0)
                    return dtOriginal;
                else
                    return dtOriginal.Select("Team = '" + curEditTeam + "'").CopyToDataTable();
            }
        }

        /// <summary>
        /// If the edit is through one team. Replace all the records in that team and save.
        /// </summary>
        /// <param name="_dtFromView"></param>
        /// <param name="_updateToFile">If true, data will directly save to database.</param>
        /// <param name="_savePath"></param>
        /// <returns></returns>
        public TeamInfo SaveChange(DataTable _dtFromView = null, 
                                   Dictionary<String, Dictionary<int, Boolean>> _chartGroupTeam = null, 
                                   Boolean _updateToFile = false, 
                                   String _savePath = "")
        {
            // Accept uncommitted data.
            _dtFromView.AcceptChanges();

            if (_updateToFile && _savePath == "")
            {
                _savePath = new Setting().TeamInfoFileLocation; 
            }

            DataTable dtTeamForsave;
            if (curEditTeam == "" || _dtFromView == null)
                dtTeamForsave = dtOriginal;
            else
            {
                foreach (DataRow drNeedRemove in dtOriginal.Select("Team = '" + curEditTeam + "'"))
                {
                    dtOriginal.Rows.Remove(drNeedRemove);
                }

                if (_dtFromView.Rows.Count != 0)
                {
                    dtOriginal.Merge(_dtFromView);
                }
                dtTeamForsave = dtOriginal;
            }

            // Convert datatable to Object TeamInfo manually.
            TeamInfo teamInfo = this.dataTable2TeamInfo(dtTeamForsave, _chartGroupTeam);

            if (_updateToFile)
                teamInfo.SaveConfiguration(_savePath);

            return teamInfo;
        }

        /// <summary>
        /// http://stackoverflow.com/questions/8472005/efficient-datatable-group-by
        /// </summary>
        /// <param name="_dtForSave"></param>
        /// <returns></returns>
        private TeamInfo dataTable2TeamInfo(DataTable _dtForSave, 
                                            Dictionary<String, Dictionary<int, Boolean>> _chartGroupTeam = null)
        {
            if (_dtForSave == null || _dtForSave.Rows.Count == 0 || _chartGroupTeam == null)
                return null;

            TeamInfo teamInfo = new TeamInfo();
            List<String> teamNames = new List<string>();
            var teamNameResult = from row in _dtForSave.AsEnumerable()
                                 group row by row.Field<String>("Team") into grp
                                 select new
                                 {
                                     TeamName = grp.Key
                                 };

            Dictionary<String, Team> teamMap = new Dictionary<string, Team>();
            foreach (var name in teamNameResult)
            {
                if (name.TeamName == null || name.TeamName.Trim() == "")
                    continue;

                teamNames.Add(name.TeamName);

                // Add the teams info to each object.
                teamMap.Add(name.TeamName, new Team()
                { 
                    Name = name.TeamName,
                    GroupChartOrderNumber = _chartGroupTeam.ContainsKey(name.TeamName) ? _chartGroupTeam[name.TeamName].Keys.First<int>() : 0,
                    IsEnabledForGroupChart = _chartGroupTeam.ContainsKey(name.TeamName) ? _chartGroupTeam[name.TeamName].Values.First<Boolean>() : true
                });
            }

            foreach (DataRow dr in _dtForSave.Rows)
            {
                if (dr["Team"].ToString().Trim() == "" || !teamMap.ContainsKey(dr["Team"].ToString().Trim()))
                    continue;

                Team gpFind = teamMap[dr["Team"].ToString().Trim()];
                gpFind.Members.Add(new Member()
                {
                    MemberName = dr["MemberName"].ToString().Trim(),
                    Alias = dr["Alias"].ToString().Trim(),
                    Role = string2Role(dr["Role"].ToString()),
                    Enable = string2Bool(dr["Enabled"].ToString().Trim())
                });
            }

            foreach (Team value in teamMap.Values)
            {
                teamInfo.Teams.Add(value);
            }
            return teamInfo;
        }

        public Role string2Role(string _str)
        {
            switch (_str.ToUpper())
            {
                case "DEV": return Role.Dev;
                case "TEST":
                case "TESTER": return Role.Test;
                case "PM": return Role.PM;
                default: return Role.Other;
            }
        }

        public Boolean string2Bool(string _str)
        {
            switch (_str.ToUpper())
            {
                case "1": return true;
                case "0":
                case "": return false;
                default: return true;
            }
        }
    }



    public class importTFSFieldData
    {
        public static TFSFields importFieldsData()
        {
            List<String> fieldWiqName = new List<String>();
            List<String> fieldDisName = new List<String>();
            String fieldName;
            TFSFields tfsFields = new TFSFields();

            List<String> suggestField = new List<string>() 
                                { 
                                    "Id",
                                    "Issue",
                                    "State",
                                    "AcceptDate",
                                    "Title",
                                    "DevAssigned",
                                    "TestAssigned",
                                    "AssignedTo"
                                };

            #region TFS fields
            fieldWiqName.Add("System.Id");
            fieldWiqName.Add("Microsoft.Dynamics.BugID");
            fieldWiqName.Add("System.WorkItemType");
            fieldWiqName.Add("System.Title");
            fieldWiqName.Add("System.AssignedTo");
            fieldWiqName.Add("System.State");
            fieldWiqName.Add("Microsoft.Dynamics.PMAssigned");
            fieldWiqName.Add("Microsoft.VSTS.Common.Priority");
            fieldWiqName.Add("Microsoft.Dynamics.TestAssigned");
            fieldWiqName.Add("Microsoft.Dynamics.TestReviewer");
            fieldWiqName.Add("Microsoft.VSTS.Common.Version");
            fieldWiqName.Add("Microsoft.Dynamics.Partner");
            fieldWiqName.Add("System.History");
            fieldWiqName.Add("Microsoft.Dynamics.DevFTEs");
            fieldWiqName.Add("Microsoft.Dynamics.DevAssigned");
            fieldWiqName.Add("Microsoft.Dynamics.CodeReviewer");
            fieldWiqName.Add("Microsoft.Dynamics.AXSEBug");
            fieldWiqName.Add("Microsoft.Dynamics.Branch");
            fieldWiqName.Add("Microsoft.Dynamics.AcceptDate");
            fieldWiqName.Add("Microsoft.VSTS.Common.ActivatedDate");
            fieldWiqName.Add("System.AreaPath");
            fieldWiqName.Add("Microsoft.Dynamics.CP.BugNumber");
            fieldWiqName.Add("Microsoft.Dynamics.CCList");
            fieldWiqName.Add("Microsoft.VSTS.Common.ActivatedBy");
            fieldWiqName.Add("Microsoft.Dynamics.SE.ApplicationTestBuild");
            fieldWiqName.Add("System.AreaId");
            fieldWiqName.Add("Microsoft.Dynamics.AssignedDevTeam");
            fieldWiqName.Add("Microsoft.Dynamics.AssignedTestTeam");
            fieldWiqName.Add("System.AttachedFileCount");
            fieldWiqName.Add("System.AuthorizedAs");
            fieldWiqName.Add("Microsoft.Dynamics.AutomatedBy");
            fieldWiqName.Add("Microsoft.IWCS.Bug.Blocking");
            fieldWiqName.Add("Microsoft.Dynamics.Browser");
            fieldWiqName.Add("Microsoft.Dynamics.BusinessImpact");
            fieldWiqName.Add("Microsoft.Dynamics.BusinessProblem");
            fieldWiqName.Add("Microsoft.Dynamics.BusinessProblemDescription");
            fieldWiqName.Add("System.ChangedBy");
            fieldWiqName.Add("System.ChangedDate");
            fieldWiqName.Add("Microsoft.VSTS.Common.ChangeListInfo");
            fieldWiqName.Add("Microsoft.Dynamics.CheckedInBy");
            fieldWiqName.Add("Microsoft.Dynamics.ClientOS");
            fieldWiqName.Add("Microsoft.VSTS.Common.ClosedBy");
            fieldWiqName.Add("Microsoft.VSTS.Common.ClosedDate");
            fieldWiqName.Add("Microsoft.Dynamics.CodeReview");
            fieldWiqName.Add("Microsoft.Dynamics.SE.CorePresent");
            fieldWiqName.Add("System.CreatedBy");
            fieldWiqName.Add("System.CreatedDate");
            fieldWiqName.Add("Microsoft.Dynamics.Customer");
            fieldWiqName.Add("Microsoft.Dynamics.Database");
            fieldWiqName.Add("System.Description");
            fieldWiqName.Add("Microsoft.Dynamics.DevReviewCredit");
            fieldWiqName.Add("Microsoft.Dynamics.RiskCategory");
            fieldWiqName.Add("Microsoft.VSTS.Common.Discipline");
            fieldWiqName.Add("Microsoft.VSTS.Common.DuplicateIDParent");
            fieldWiqName.Add("Microsoft.Dynamics.EarliestKnownBrokenBuild");
            fieldWiqName.Add("Microsoft.Dynamics.SE.EEPresent");
            fieldWiqName.Add("Microsoft.Dynamics.Escalation");
            fieldWiqName.Add("Microsoft.Dynamics.EscalationEngineerAssigned");
            fieldWiqName.Add("Microsoft.Dynamics.ETADate");
            fieldWiqName.Add("Microsoft.Dynamics.ExplicitRequestedBy");
            fieldWiqName.Add("System.ExternalLinkCount");
            fieldWiqName.Add("Microsoft.Dynamics.FixBy");
            fieldWiqName.Add("Microsoft.Dynamics.FixImpact");
            fieldWiqName.Add("Microsoft.Dynamics.FixImpactDescription");
            fieldWiqName.Add("Microsoft.Dynamics.GoLiveBlocker");
            fieldWiqName.Add("Microsoft.Dynamics.GoLiveBug");
            fieldWiqName.Add("Microsoft.Dynamics.GoLiveDate");
            fieldWiqName.Add("Microsoft.Dynamics.HowFound");
            fieldWiqName.Add("System.HyperLinkCount");
            fieldWiqName.Add("Microsoft.Dynamics.InformationRequest");
            fieldWiqName.Add("Microsoft.Dynamics.InformationResponse");
            fieldWiqName.Add("Microsoft.VSTS.Build.IntegrationBuild");
            fieldWiqName.Add("Microsoft.VSTS.Common.Issue");
            fieldWiqName.Add("System.IterationPath");
            fieldWiqName.Add("System.IterationId");
            fieldWiqName.Add("Microsoft.Dynamics.KBNumber");
            fieldWiqName.Add("Microsoft.Dynamics.SE.KernelTestBuild");
            fieldWiqName.Add("Microsoft.Dynamics.KernelType");
            fieldWiqName.Add("Microsoft.VSTS.Common.Keywords");
            fieldWiqName.Add("Microsoft.Dynamics.LastKnownWorkingBuild");
            fieldWiqName.Add("Microsoft.Dynamics.SE.MPTSResult");
            fieldWiqName.Add("System.NodeName");
            fieldWiqName.Add("Microsoft.Dynamics.OpenedApplicationBuild");
            fieldWiqName.Add("Microsoft.Dynamics.OpenedKernelBuild");
            fieldWiqName.Add("Microsoft.Dynamics.SE.OTLatestFailed");
            fieldWiqName.Add("Microsoft.Dynamics.SE.OTLatestPassed");
            fieldWiqName.Add("Microsoft.Dynamics.SE.OTLatestPending");
            fieldWiqName.Add("Microsoft.Dynamics.SE.OTLatestTotal");
            fieldWiqName.Add("Microsoft.Dynamics.SE.OTRTMSP1Failed");
            fieldWiqName.Add("Microsoft.Dynamics.SE.OTRTMSP1Passed");
            fieldWiqName.Add("Microsoft.Dynamics.SE.OTRTMSP1Pending");
            fieldWiqName.Add("Microsoft.Dynamics.SE.OTRTMSP1Total");
            fieldWiqName.Add("Microsoft.Dynamics.SE.PTPassedFirst");
            fieldWiqName.Add("Microsoft.Dynamics.SE.PTPassedRerun");
            fieldWiqName.Add("Microsoft.Dynamics.SE.PlannedUpdate");
            fieldWiqName.Add("Microsoft.Dynamics.ProductStudio");
            fieldWiqName.Add("Microsoft.Dynamics.PSRegressionBug");
            fieldWiqName.Add("Microsoft.Dynamics.SE.PTFailed");
            fieldWiqName.Add("Microsoft.Dynamics.SE.PTKnownFailure");
            fieldWiqName.Add("Microsoft.Dynamics.SE.PTPassedManual");
            fieldWiqName.Add("Microsoft.Dynamics.SE.PTPending");
            fieldWiqName.Add("Microsoft.Dynamics.SE.PTTotal");
            fieldWiqName.Add("Microsoft.Dynamics.SE.RCA.Detection");
            fieldWiqName.Add("Microsoft.Dynamics.SE.RCA.Diagnosis");
            fieldWiqName.Add("Microsoft.Dynamics.SE.RCA.Prevention");
            fieldWiqName.Add("Microsoft.Dynamics.SE.RCA.Symptom");
            fieldWiqName.Add("System.Reason");
            fieldWiqName.Add("Microsoft.Dynamics.RegionCountry");
            fieldWiqName.Add("Microsoft.VSTS.Common.Regression");
            fieldWiqName.Add("System.RelatedLinkCount");
            fieldWiqName.Add("Microsoft.Dynamics.ReproApplicationBuild");
            fieldWiqName.Add("Microsoft.Dynamics.ReproKernelBuild");
            fieldWiqName.Add("Microsoft.VSTS.Common.ResolvedBy");
            fieldWiqName.Add("Microsoft.VSTS.Common.ResolvedDate");
            fieldWiqName.Add("Microsoft.VSTS.Common.ResolvedReason");
            fieldWiqName.Add("System.Rev");
            fieldWiqName.Add("System.RevisedDate");
            fieldWiqName.Add("Microsoft.Dynamics.RiskDescription");
            fieldWiqName.Add("Microsoft.VSTS.Common.SecurityBug");
            fieldWiqName.Add("Microsoft.Dynamics.ServerOS");
            fieldWiqName.Add("Microsoft.VSTS.Common.Severity");
            fieldWiqName.Add("Microsoft.Dynamics.Solution");
            fieldWiqName.Add("Microsoft.Dynamics.SE.Solution1");
            fieldWiqName.Add("Microsoft.Dynamics.SE.Solution2");
            fieldWiqName.Add("Microsoft.Dynamics.SE.Solution3");
            fieldWiqName.Add("Microsoft.Dynamics.SE.SROpenedDate");
            fieldWiqName.Add("Microsoft.Dynamics.SE.SRReqNumber");
            fieldWiqName.Add("Microsoft.VSTS.Common.StateChangeDate");
            fieldWiqName.Add("Microsoft.Dynamics.StateTransitionDate");
            fieldWiqName.Add("Microsoft.VSTS.CMMI.StepsToReproduce");
            fieldWiqName.Add("Microsoft.Dynamics.TaskStarted");
            fieldWiqName.Add("Microsoft.Dynamics.Team");
            fieldWiqName.Add("System.TeamProject");
            fieldWiqName.Add("Microsoft.Dynamics.TechnicalDescription");
            fieldWiqName.Add("Microsoft.Dynamics.TestCaseDataSource");
            fieldWiqName.Add("Microsoft.Dynamics.TestCaseId");
            fieldWiqName.Add("Microsoft.Dynamics.TestCaseStatus");
            fieldWiqName.Add("Microsoft.Dynamics.SE.TestRecommendation");
            fieldWiqName.Add("Microsoft.Dynamics.SE.TestResultsNotes");
            fieldWiqName.Add("Microsoft.Dynamics.TestReviewCredit");
            fieldWiqName.Add("Microsoft.Dynamics.TestScenarios");
            fieldWiqName.Add("Microsoft.Dynamics.SE.TestSpecNotes");
            fieldWiqName.Add("Microsoft.VSTS.Common.Triage");
            fieldWiqName.Add("Microsoft.Dynamics.DevCost");
            fieldWiqName.Add("Microsoft.Dynamics.VendorDaysCredit");
            fieldWiqName.Add("Microsoft.Dynamics.WTTPaths");
            #endregion TFS fields

            #region TFS display fields
            fieldDisName.Add("ID");
            fieldDisName.Add("Bug ID");
            fieldDisName.Add("Work Item Type");
            fieldDisName.Add("Title");
            fieldDisName.Add("Assigned To");
            fieldDisName.Add("State");
            fieldDisName.Add("PM Assigned");
            fieldDisName.Add("Priority");
            fieldDisName.Add("Test Assigned");
            fieldDisName.Add("Test Reviewer");
            fieldDisName.Add("Version");
            fieldDisName.Add("Partner");
            fieldDisName.Add("History");
            fieldDisName.Add("Dev FTEs");
            fieldDisName.Add("Dev Assigned");
            fieldDisName.Add("Code Reviewer");
            fieldDisName.Add("AXSE Bug");
            fieldDisName.Add("Branch");
            fieldDisName.Add("Accept Date");
            fieldDisName.Add("Activated Date");
            fieldDisName.Add("Area Path");
            fieldDisName.Add("Bug");
            fieldDisName.Add("CC: List");
            fieldDisName.Add("Activated By");
            fieldDisName.Add("Application Test Build");
            fieldDisName.Add("AreaID");
            fieldDisName.Add("Assigned Dev Team");
            fieldDisName.Add("Assigned Test Team");
            fieldDisName.Add("AttachedFileCount");
            fieldDisName.Add("Authorized As");
            fieldDisName.Add("Automated By");
            fieldDisName.Add("Blocking");
            fieldDisName.Add("Browser");
            fieldDisName.Add("Business Impact");
            fieldDisName.Add("Business Problem");
            fieldDisName.Add("Business Problem Description");
            fieldDisName.Add("Changed By");
            fieldDisName.Add("Changed Date");
            fieldDisName.Add("ChangeListInfo");
            fieldDisName.Add("Checked In By");
            fieldDisName.Add("Client OS");
            fieldDisName.Add("Closed By");
            fieldDisName.Add("Closed Date");
            fieldDisName.Add("Code Review");
            fieldDisName.Add("Core Present");
            fieldDisName.Add("Created By");
            fieldDisName.Add("Created Date");
            fieldDisName.Add("Customer");
            fieldDisName.Add("Database");
            fieldDisName.Add("Description");
            fieldDisName.Add("Dev Review Credit");
            fieldDisName.Add("Development Risk");
            fieldDisName.Add("Discipline");
            fieldDisName.Add("Duplicate ID (Parent)");
            fieldDisName.Add("Earliest Known Broken Build");
            fieldDisName.Add("EE Present");
            fieldDisName.Add("Escalation");
            fieldDisName.Add("Escalation Engineer Assigned");
            fieldDisName.Add("ETA");
            fieldDisName.Add("Explicit Requested By");
            fieldDisName.Add("ExternalLinkCount");
            fieldDisName.Add("Fix By");
            fieldDisName.Add("Fix Impact");
            fieldDisName.Add("Fix Impact Description");
            fieldDisName.Add("GoLive Blocker");
            fieldDisName.Add("GoLive Bug");
            fieldDisName.Add("GoLive Date");
            fieldDisName.Add("How Found");
            fieldDisName.Add("HyperLinkCount");
            fieldDisName.Add("Information Request");
            fieldDisName.Add("Information Response");
            fieldDisName.Add("Integration Build");
            fieldDisName.Add("Issue");
            fieldDisName.Add("Iteration Path");
            fieldDisName.Add("IterationID");
            fieldDisName.Add("KBNumber");
            fieldDisName.Add("Kernel Test Build");
            fieldDisName.Add("Kernel Type");
            fieldDisName.Add("Keywords");
            fieldDisName.Add("Last Known Working Build");
            fieldDisName.Add("MPTS Result");
            fieldDisName.Add("Node Name");
            fieldDisName.Add("Opened Application Build");
            fieldDisName.Add("Opened Kernel Build");
            fieldDisName.Add("OTLatest Failed");
            fieldDisName.Add("OTLatest Passed");
            fieldDisName.Add("OTLatest Pending");
            fieldDisName.Add("OTLatest Total");
            fieldDisName.Add("OTRTMSP1 Failed");
            fieldDisName.Add("OTRTMSP1 Passed");
            fieldDisName.Add("OTRTMSP1 Pending");
            fieldDisName.Add("OTRTMSP1 Total");
            fieldDisName.Add("Passed on First Run");
            fieldDisName.Add("Passed on Rerun");
            fieldDisName.Add("Planned Update");
            fieldDisName.Add("Product Studio");
            fieldDisName.Add("PS Regression Bug");
            fieldDisName.Add("PT Failed (To Be Investigated)");
            fieldDisName.Add("PT Invalid (Known Failure)");
            fieldDisName.Add("PT Passed on Manual Run");
            fieldDisName.Add("PT Pending");
            fieldDisName.Add("PT Total");
            fieldDisName.Add("RCA Detection");
            fieldDisName.Add("RCA Diagnosis");
            fieldDisName.Add("RCA Prevention");
            fieldDisName.Add("RCA Symptom");
            fieldDisName.Add("Reason");
            fieldDisName.Add("Region/Country");
            fieldDisName.Add("Regression");
            fieldDisName.Add("RelatedLinkCount");
            fieldDisName.Add("Repro Application Build");
            fieldDisName.Add("Repro Kernel Build");
            fieldDisName.Add("Resolved By");
            fieldDisName.Add("Resolved Date");
            fieldDisName.Add("Resolved Reason");
            fieldDisName.Add("Rev");
            fieldDisName.Add("Revised Date");
            fieldDisName.Add("Risk Description");
            fieldDisName.Add("Security Bug");
            fieldDisName.Add("Server OS");
            fieldDisName.Add("Severity");
            fieldDisName.Add("Solution");
            fieldDisName.Add("Solution 1");
            fieldDisName.Add("Solution 2");
            fieldDisName.Add("Solution 3");
            fieldDisName.Add("SR Opened Date");
            fieldDisName.Add("SR Req Number");
            fieldDisName.Add("State Change Date");
            fieldDisName.Add("State Transition Date");
            fieldDisName.Add("Steps To Reproduce");
            fieldDisName.Add("Task Started");
            fieldDisName.Add("Team");
            fieldDisName.Add("Team Project");
            fieldDisName.Add("Technical Description");
            fieldDisName.Add("Test Case Data Source");
            fieldDisName.Add("Test Case ID");
            fieldDisName.Add("Test Case Status");
            fieldDisName.Add("Test Recommendations");
            fieldDisName.Add("Test Results Notes");
            fieldDisName.Add("Test Review Credit");
            fieldDisName.Add("Test Scenarios");
            fieldDisName.Add("Test Spec Notes");
            fieldDisName.Add("Triage");
            fieldDisName.Add("T-Shirt Size");
            fieldDisName.Add("Vendor Days Credit");
            fieldDisName.Add("WTT Paths");

            #endregion

            int index = 0;
            int suggestFieldsID = 0;
            foreach (String wiqName in fieldWiqName)
            {
                fieldName = wiqName.Substring(wiqName.LastIndexOf('.') + 1);
                tfsFields.Fields.Add(new TFSField()
                {
                    FieldName = fieldName,
                    FieldWiqName = wiqName,
                    FieldDisName = fieldDisName[index++],
                    FieldWidth = 100,
                    FieldNum = suggestField.Contains(fieldName) ? suggestFieldsID ++ : -1,
                    Enabled = suggestField.Contains(fieldName) ? true : false 
                });
            }
            
            return tfsFields;
        }
    }

    #region Fields genrator program backup.
//    using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Windows.Forms;
//using Microsoft.TeamFoundation;
//using Microsoft.TeamFoundation.Client;
//using Microsoft.TeamFoundation.WorkItemTracking.Client;

//namespace QueryAPI
//{
//    public partial class Form1 : Form
//    {
//        //http://msdn.microsoft.com/en-us/library/bb130306.aspx
//        //Query 
//        public Form1()
//        {
//            InitializeComponent();
//        }

//        //string strQuery = "SELECT [System.Id], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State], [Microsoft.Dynamics.AcceptDate], [Microsoft.VSTS.Common.Priority], [Microsoft.Dynamics.ETADate] FROM WorkItems WHERE [Microsoft.Dynamics.AcceptDate] >= '2011-08-01T00:00:00.0000000' AND [Microsoft.Dynamics.AcceptDate] <= '2011-08-31T00:00:00.0000000' AND [System.WorkItemType] = 'Product Bug' ORDER BY [System.Id] ";
//        //[Microsoft.Dynamics.AXSEBug]
//        string strQueryTestData = "SELECT [System.Id], [Microsoft.Dynamics.BugID], [Microsoft.VSTS.Common.Priority], [System.WorkItemType], [System.Title], [System.AssignedTo], [System.State], [Microsoft.Dynamics.AcceptDate], [Microsoft.Dynamics.EscalationEngineerAssigned], [Microsoft.Dynamics.CodeReviewer], [Microsoft.Dynamics.TestReviewer] FROM WorkItems WHERE [Microsoft.Dynamics.BugID] = 31623 or [Microsoft.Dynamics.BugID] = 35470 ORDER BY [System.Id]";


//        public List<String> DisplayNames = new List<string>();
//        public List<String> WiqNames = new List<string>();

//        public DataTable dtResult = new DataTable();

//        private void Form1_Load(object sender, EventArgs e)
//        {
//            //textBox1.Text = strQuery;
//            textBox1.Text = strQueryTestData;
//        }

//        private void button1_Click(object sender, EventArgs e)
//        {
//            TfsTeamProjectCollection tpc = new TfsTeamProjectCollection(
//                new Uri("http://vstfmbs:8080/tfs"));

//            WorkItemStore workItemStore = (WorkItemStore)tpc.GetService(typeof(WorkItemStore));
//            //WorkItemCollection queryResults = workItemStore.Query("select * from bugcache");

//            this.getWiqNames(textBox1.Text);
//            try
//            {
//                //WorkItemCollection queryResults = workItemStore.Query(strQuery);
//                WorkItemCollection queryResults = workItemStore.Query(textBox1.Text);
                
//                dtResult = renderWorkItemAsDataSet(queryResults);
//                dtResult.TableName = "testData";
//                dataGridView1.DataSource = dtResult;

//                this.constuctUsefulWord();
//                //http://support.softartisans.com/kbview_1301.aspx
//                dtResult.WriteXml(@"D:\Tool\Test Project\QueryAPI\queryData.xml", XmlWriteMode.WriteSchema);
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show(ex.Message);
//            }
//        }
        
//        /// <summary>
//        /// refer the link: http://forums.asp.net/t/1178002.aspx
//        /// </summary>
//        /// <param name="wiCollection"></param>
//        /// <returns></returns>
//        private DataTable renderWorkItemAsDataSet (WorkItemCollection wiCollection)
//        {
//            DataTable sourceDt = new DataTable();

//            if (wiCollection != null)
//            {
//                foreach (FieldDefinition fieldDef in wiCollection.DisplayFields)
//                {
//                    sourceDt.Columns.Add(fieldDef.Name,fieldDef.SystemType);
//                    DisplayNames.Add(fieldDef.Name);
//                }

//                foreach (WorkItem workItem in wiCollection)
//                {
//                    DataRow dr = sourceDt.NewRow();
//                    foreach (FieldDefinition fieldDef in wiCollection.DisplayFields)
//                    {
//                        try
//                        {
//                            dr[fieldDef.Name] = workItem[fieldDef.Name];
//                        }
//                        catch
//                        {
//                            dr[fieldDef.Name] = DBNull.Value;
//                        }
//                    }

//                    sourceDt.Rows.Add(dr);
//                }
//            }

//            return sourceDt;
//        }

//        private void getWiqNames(String _wiqStr)
//        {
//            String totalWiqFields = _wiqStr.Substring(_wiqStr.IndexOf("SELECT") + 6, _wiqStr.IndexOf("FROM") - _wiqStr.IndexOf("SELECT") - 7);

//            String[] eachWiqFields = totalWiqFields.Split(',');

//            foreach (string str in eachWiqFields)
//            {
//                string fir = str.Remove(0, 2);

//                WiqNames.Add(fir.Remove(fir.Count() - 1, 1));
//            }
//        }


//        private void constuctUsefulWord()
//        {
//            if (WiqNames.Count == DisplayNames.Count)
//            {
//                String wiqNameList = "fieldWiqName";
//                String displayNameList = "fieldDisName";

//                String total = "";

//                foreach (String str in WiqNames)
//                {
//                    total += wiqNameList + ".Add(\"" + str + "\");\r\n";
//                }

//                foreach (String str in DisplayNames)
//                {
//                    total += displayNameList + ".Add(\"" + str + "\");\r\n";
//                }

//                textBox1.Text = total;
//            }            
//        }
//    }
//}

#endregion
}
