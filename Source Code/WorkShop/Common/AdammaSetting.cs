using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Drawing;
using TFSAdapter;

namespace WorkShop
{
    [XmlRoot(ElementName = "AdammaSetting")]
    public class AdammaSetting
    {
        #region private data
        private HistorySetting historySetting = null;
        private ProgramSetting proSetting = null;
        private InfoLoc infoLocation = null;
        #endregion 

        public AdammaSetting()
        {
            proSetting = new ProgramSetting();
            infoLocation = new InfoLoc();
            HistorySetting = new HistorySetting();
        }

        [XmlElement(ElementName = "ProgramSetting")]
        public ProgramSetting ProSetting
        {
            get { return proSetting; }
            set { proSetting = value; }
        }

        [XmlElement(ElementName = "HistorySetting")]
        public HistorySetting HistorySetting
        {
            get { return historySetting; }
            set { historySetting = value; }
        }

        [XmlElement(ElementName = "InfoLocation")]
        public InfoLoc InfoLocation
        {
            get { return infoLocation; }
            set { infoLocation = value; }
        }

        public void saveSetupInfo(String _path)
        {
            FileStream fs = null;
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(AdammaSetting));
                fs = new FileStream(_path, FileMode.Create, FileAccess.Write);
                xs.Serialize(fs, this);
                fs.Close();
            }
            catch
            {
                if (fs != null)
                    fs.Close();
            }
        }

        public static AdammaSetting LoadSetupInfo(String _path)
        {
            FileStream fs = null;
            try
            {
                XmlSerializer xs = new XmlSerializer(typeof(AdammaSetting));
                fs = new FileStream(_path, FileMode.Open, FileAccess.Read);
                AdammaSetting setting = (AdammaSetting)xs.Deserialize(fs);
                fs.Close();
                return setting;
            }
            catch
            {
                if (fs != null)
                    fs.Close();

                return null;
            }
        }
    }

    public class HistorySetting
    {
        #region private data
        private String historyLoadingTeam;
        private String historyLoadingBugType;
        private BugStatus historyLoadingBugStatus;
        #endregion

        public HistorySetting()
        {
            historyLoadingTeam = "";
            historyLoadingBugType = "";
            historyLoadingBugStatus = BugStatus.ActiveBugs;
        }

        [XmlElement(ElementName = "HistoryLoadingTeam", Type = typeof(String))]
        public String HistoryLoadingTeam
        {
            get { return historyLoadingTeam; }
            set { historyLoadingTeam = value; }
        }

        /// <summary>
        /// This string is constuct by 0 & 1. 1 means enabled, 0 means disabled bugtype.
        /// </summary>
        [XmlElement(ElementName = "HistoryLoadingBugType", Type = typeof(String))]
        public String HistoryLoadingBugType
        {
            get { return historyLoadingBugType; }
            set { historyLoadingBugType = value; }
        }

        [XmlElement(ElementName = "HistoryLoadingBugStatus", Type = typeof(BugStatus))]
        public BugStatus HistoryLoadingBugStatus
        {
            get { return historyLoadingBugStatus; }
            set { historyLoadingBugStatus = value; }
        }
    }

    public class ProgramSetting
    {
        #region private data
        private Boolean startWithWindows;
        private int refreshCycle;
        private Boolean customizeGridViewLoading;
        private Boolean useCustomizedFieldWhenExcuteQuery;

        private Boolean preLoadingAllSingalBugsData;
        private Boolean loadingFromQuery;
        private Boolean loadingFromDefaultTeam;

        private String defaultTeamNameForLoad;
        private String defaultQueryForLoad;

        private String barType;
        #endregion

        public ProgramSetting()
        {
            this.startWithWindows = true;
            this.refreshCycle = 0;

            this.customizeGridViewLoading = false;
            this.useCustomizedFieldWhenExcuteQuery = true;
            this.preLoadingAllSingalBugsData = true;

            this.loadingFromQuery = false;
            this.loadingFromDefaultTeam = false;

            this.defaultTeamNameForLoad = "";
            this.defaultQueryForLoad = "";

            this.barType = "Cluster";
        }

        [XmlElement(ElementName = "StartWithWindows", Type = typeof(Boolean))]
        public Boolean StartWithWindows
        {
            get { return startWithWindows; }
            set { startWithWindows = value; }
        }

        [XmlElement(ElementName = "RefreshCycle", Type = typeof(int))]
        public int RefreshCycle
        {
            get { return refreshCycle; }
            set { refreshCycle = value; }
        }

        [XmlElement(ElementName = "CustomizeGridViewLoading", Type = typeof(Boolean))]
        public Boolean CustomizeGridViewLoading
        {
            get { return customizeGridViewLoading; }
            set { customizeGridViewLoading = value; }
        }

        [XmlElement(ElementName = "UseCustomizedFieldWhenExcuteQuery", Type = typeof(Boolean))]
        public Boolean UseCustomizedFieldWhenExcuteQuery
        {
            get { return useCustomizedFieldWhenExcuteQuery; }
            set { useCustomizedFieldWhenExcuteQuery = value; }
        }

        [XmlElement(ElementName = "DefaultQueryForLoad", Type = typeof(String))]
        public String DefaultQueryForLoad
        {
            get { return defaultQueryForLoad; }
            set { defaultQueryForLoad = value; }
        }

        [XmlElement(ElementName = "PreLoadingAllSingalBugsData", Type = typeof(Boolean))]
        public Boolean PreLoadingAllSingalBugsData
        {
            get { return preLoadingAllSingalBugsData; }
            set { preLoadingAllSingalBugsData = value; }
        }

        [XmlElement(ElementName = "LoadingFromQuery", Type = typeof(Boolean))]
        public Boolean LoadingFromQuery
        {
            get { return loadingFromQuery; }
            set
            {
                loadingFromQuery = value;
                if (loadingFromQuery)
                    loadingFromDefaultTeam = false;
                else
                    loadingFromDefaultTeam = true;
            }
        }

        [XmlElement(ElementName = "DefaultTeamNameForLoad", Type = typeof(String))]
        public String DefaultTeamNameForLoad
        {
            get { return defaultTeamNameForLoad; }
            set { defaultTeamNameForLoad = value; }
        }

        [XmlElement(ElementName = "LoadingFromDefaultTeam", Type = typeof(Boolean))]
        public Boolean LoadingFromDefaultTeam
        {
            get { return loadingFromDefaultTeam; }
            set 
            {
                loadingFromDefaultTeam = value;
                if (loadingFromDefaultTeam)
                    loadingFromQuery = false;
                else
                    loadingFromQuery = true;
            }
        }

        [XmlElement(ElementName = "BarType")]
        public String BarType
        {
            get { return barType; }
            set { barType = value; }
        }
    }

    public class InfoLoc
    {
        private String queryRootFolder;
        private String teamInfoLoc;
        private String fieldsLoc;

        public InfoLoc()
        {
            this.queryRootFolder = "";
            this.teamInfoLoc = "";
            this.fieldsLoc = "";
        }

        [XmlElement(ElementName = "QueryRootFolder")]
        public String QueryRootFolder
        {
            get { return queryRootFolder; }
            set { queryRootFolder = value; }
        }

        [XmlElement(ElementName = "TeamInfoLoc")]
        public String TeamInfoLoc
        {
            get { return teamInfoLoc; }
            set { teamInfoLoc = value; }
        }

        [XmlElement(ElementName = "FieldsLoc")]
        public String FieldsLoc
        {
            get { return fieldsLoc; }
            // set the fields location when the absolutly address changed in other computers.
            set { fieldsLoc = value; }
        }
    }

    public enum LocType
    {
        SettingFile,
        QueryRootFolder,
        TeamInfo,
        FieldsFile
    }
}
