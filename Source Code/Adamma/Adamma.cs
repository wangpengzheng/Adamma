using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WorkShop;
using TFSAdapter;
using System.IO;
using System.Threading;
using System.Diagnostics;

namespace Adamma
{
    public partial class Adamma : Form
    {
        BackgroundWorker workerForOneBug;
        BackgroundWorker workerForGroupBugs;
        BackgroundWorker workerForChartGroupBugs;

        /// <summary>
        /// The setup from configure file.
        /// </summary>
        private Setting SettingInfo;

        /// <summary>
        /// The opinion user choosen/ from Query Results
        /// </summary>
        private GlobalValue GlobalInfo;

        /// <summary>
        /// use to calculate the SLA in datagridview.
        /// </summary>
        SLAController slaController;

        /// <summary>
        /// Use to save the updated filter, Should be updated each time in method this.updateCurrentQueryExpression().
        /// The expressions need are,
        /// 1. Team for query.
        /// 2. Bug type for query.
        /// 3. Bug status for query.
        /// 4. BugDateType for query. (Optional)
        /// </summary>
        private ExpressionController GridViewFilterController;

        private ExpressionController ChartGridFilterController;

        public Adamma()
        {
            InitializeComponent();

            // Start Async worker.
            workerForGroupBugs = new BackgroundWorker();
            this.workerForGroupBugs.WorkerReportsProgress = false;
            this.workerForGroupBugs.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_DoWork);
            //this.workerForGroupBugs.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker_ProgressChanged);
            this.workerForGroupBugs.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker_RunWorkerCompleted);

            // Start Async worker.
            workerForChartGroupBugs = new BackgroundWorker();
            this.workerForChartGroupBugs.WorkerReportsProgress = false;
            this.workerForChartGroupBugs.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerForChartGroup_DoWork);
            this.workerForChartGroupBugs.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerForChartGroup_RunWorkerCompleted);


            workerForOneBug = new BackgroundWorker();
            this.workerForOneBug.WorkerReportsProgress = false;
            this.workerForOneBug.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorkerLoadingSpecificBug_DoWork);
            //this.backgroundWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorkerLoadingSpecificBug_ProgressChanged);
            this.workerForOneBug.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorkerLoadingSpecificBug_RunWorkerCompleted);

            zedGraphControlWholeTeam.Visible = false;
            pictureBoxWhite2.Visible = true;
            pictureBoxLoading.Visible = true;
            pictureBoxWhite1.Visible = true;
            pictureBoxLoadingForGridView.Visible = true;

            // Diable the dynamic loading display
            timerForSignalBug.Enabled = false;

            // Setup the query button as wait for query.
            toolStripButtonExecuteQuery.Image = imageListQuery.Images[0];
            toolStripButtonExecuteQuery.Text = "Execute Query";
        }

        /// <summary>
        /// Load form control 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Adamma_Load(object sender, EventArgs e)
        {
            try
            {
                // Load setting info from config file.
                SettingInfo = new Setting();

                // Add the required things(Class, Struct, enum, data content..ect...) to gloabal value.
                GlobalInfo = new GlobalValue(SettingInfo);

                // Load values to init form.
                this.loadFromControlValues();

                // Load queries from query folder.
                this.loadQueryFilesToTreeViews();

                // Load data Asynchronous for grid view/Dev Test Pie Chart/Chart group
                this.loadAndDisplayTeamBugsDataAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Adamma init failed!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #region Load form control values from the setting file.
        /// <summary>
        /// 1. Load Bug type from BugType enum.
        /// 2. Load Bug Status from BugStates enum and two Customized enum value "ActiveBugs" & "ResolvedColsedBug"
        /// 3. Load last user choosen bugStates and bugType.
        /// 4. Load DateQueryFields from enum. Set the from date Time picker to the first day of current month. Set the To date Time picker to the last date of current month.
        /// </summary>
        private void loadFromControlValues()
        {
            this.refreshDataWithAdammaSetting();
            // Load Bug Status from BugStates enum and two Customized enum value "ActiveBugs" & "ResolvedColsedBug"
            comboBoxStatus.Items.Clear();
            comboBoxStatus.AutoCompleteMode = AutoCompleteMode.Suggest;
            comboBoxStatus.AutoCompleteSource = AutoCompleteSource.ListItems;
            foreach (string status in typeof(BugStatus).GetEnumNames())
            {
                if (status != "EmptyBugStatus")
                {
                    comboBoxStatus.Items.Add(status);
                }
            }

            // Set the combobox event to null to ensure unwishes even happened.
            this.comboBoxTeam.SelectedIndexChanged -= new System.EventHandler(this.buttonRefresh_Click);
            this.comboBoxStatus.SelectedIndexChanged -= new System.EventHandler(this.buttonRefresh_Click);

            // Load last user choosen bugStates and bugType.
            comboBoxTeam.Text = GlobalInfo.CurrentTeam;
            comboBoxStatus.Text = (GlobalInfo.CurBugStatus == BugStatus.EmptyBugStatus) ? "" : GlobalInfo.CurBugStatus.ToString();

            this.comboBoxTeam.SelectedIndexChanged += new System.EventHandler(this.buttonRefresh_Click);
            this.comboBoxStatus.SelectedIndexChanged += new System.EventHandler(this.buttonRefresh_Click);

            // Load Bug type from BugType. Formate saved as "010001"
            String bugTypeStr = SettingInfo.AdammaSetting.HistorySetting.HistoryLoadingBugType;
            int curBugIndex = 0;

            checkedListBoxTypeRange.Items.Clear();
            foreach (string type in typeof(BugType).GetEnumNames())
            {
                if (type != "EmptyBugType")
                {
                    if (curBugIndex < bugTypeStr.Count() && bugTypeStr.Substring(curBugIndex, 1) == "1")
                    {
                        checkedListBoxTypeRange.Items.Add(type, true);
                    }
                    else
                    {
                        checkedListBoxTypeRange.Items.Add(type, false);
                    }
                }

                curBugIndex++;
            }

            GlobalInfo.CurBugType = this.updateCurrentBugType();

            // Load DateQueryFields from enum.
            foreach (string queryDateField in typeof(DateQueryFields).GetEnumNames())
            {
                if (queryDateField != "EmptyField")
                {
                    comboBoxDateQueryFields.Items.Add(queryDateField);
                }
            }

            // Set the from date Time picker to the first day of current month. 
            dateTimePickerFromDate.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            // Set the To date Time picker to the last date of current month.
            dateTimePickerToDate.Value = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1).AddDays(-1);

            checkBoxFromDate.Enabled = false;
            checkBoxToDate.Enabled = false;
            checkBoxFromDate.Checked = false;
            checkBoxToDate.Checked = false;

            dateTimePickerFromDate.Enabled = false;
            dateTimePickerToDate.Enabled = false;

            GlobalInfo.CurBugDateType = new BugDateTypeCLS(CommonUtilities.ParseNum<DateQueryFields>(comboBoxDateQueryFields.Text),
                                            dateTimePickerFromDate.Value,
                                            dateTimePickerToDate.Value);

            if (GlobalInfo.CurAsyncLoadingType == AsyncLoadingType.TeamBugs)
            {
                GridViewFilterController = new ExpressionController(QueryType.TeamQuery);

                GridViewFilterController.setExpression(
                        SettingInfo.TeamInfo.GetAllEnabledTeamMembersToDictionaryThroughTeamName(GlobalInfo.CurrentTeam),
                        GlobalInfo.CurBugType,
                        GlobalInfo.CurBugStatus,
                        GlobalInfo.CurBugDateType);
            }
            else if (GlobalInfo.CurAsyncLoadingType == AsyncLoadingType.QueryBugs)
            {
                string sqlfromQuery = CommonUtilities.getSqlTxtFromWiq(GlobalInfo.CurrentQueryFileLocation);

                if (sqlfromQuery != null)
                {
                    GridViewFilterController = new ExpressionController(QueryType.WiqSqlQuery);
                    GridViewFilterController.setExpressionForWiqSqlQuery(
                        sqlfromQuery,
                        SettingInfo.AdammaSetting.ProSetting.UseCustomizedFieldWhenExcuteQuery);
                }
            }
        }
        #endregion



        #region Asychoronous loading process.
        #region BackGroundWorker girdview to do Async.
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            this.loadAllDataForForm();
        }

        private delegate void displayProcess();
        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                pictureBoxWhite1.Visible = false;
                pictureBoxLoadingForGridView.Visible = false;
                dataGridViewBugs.Visible = true;
                timerForGridview.Enabled = false;
                toolStripStatusLabelLoadingStatus.Text = "Loading data complete! Perform displaying process in Adamma.";

                displayProcess displayProcess = this.displayColorAndDataForGridview;
                displayProcess += displayChartData;
                displayProcess += displayChartPerDevTestData;
                displayProcess += displayInformationData;
                displayProcess.Invoke();

                toolStripStatusLabelLoadingStatus.Text = "Loading completed!";
            }
            catch
            {
                MessageBox.Show("Error exist when display data.");
            }
        }

        private void backgroundWorkerForChartGroup_DoWork(object sender, DoWorkEventArgs e)
        {
            this.loadChartGroupData();            
        }

        private void backgroundWorkerForChartGroup_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            this.displayChartGroupData();
        }
        #endregion 

        #region Backgroud work for loading one bug.
        private void backgroundWorkerLoadingSpecificBug_DoWork(object sender, DoWorkEventArgs e)
        {
            this.loadAllDataForSingalBug();
        }

        private delegate void displayProcessForOneBug();
        private void backgroundWorkerLoadingSpecificBug_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            displayProcessForOneBug displayProcess = this.displaySingalBugInformation;
            displayProcess.Invoke();
        }

        private void displaySingalBugInformation()
        {
            if (dataGridViewBugs.Rows.Count == 0 ||
                (GlobalValue.curSelectedPSID == "" && GlobalValue.curSelectedTFSID == "") ||
                (!SettingInfo.AdammaSetting.ProSetting.PreLoadingAllSingalBugsData && (GlobalInfo.CharDTForSignalBug == null || GlobalInfo.CharDTForSignalBug.Rows.Count == 0)) ||
                (SettingInfo.AdammaSetting.ProSetting.PreLoadingAllSingalBugsData && (GlobalInfo.GridViewBugsDT == null || GlobalInfo.GridViewBugsDT.Rows.Count == 0 )))
                return;

            DataTable dtForDisplaysingalBug;
            if (SettingInfo.AdammaSetting.ProSetting.PreLoadingAllSingalBugsData)
            {
                dtForDisplaysingalBug = GlobalInfo.GridViewBugsDT.Copy();
            }
            else
            {
                toolStripProgressBarLoadingStatus.Visible = false;
                timerForSignalBug.Enabled = false;

                toolStripStatusLabelLoadingStatus.Text = "Loading completed for DAXSE_" + GlobalValue.curSelectedTFSID + "!";
                dtForDisplaysingalBug = GlobalInfo.CharDTForSignalBug.Copy();
            }

            DataRow drForSingalBug = dtForDisplaysingalBug.NewRow();
            
            if (dtForDisplaysingalBug.Select("ID = '" + GlobalValue.curSelectedTFSID + "'").Count() > 0)
            {
                drForSingalBug = dtForDisplaysingalBug.Select("ID = '" + GlobalValue.curSelectedTFSID + "'")[0];
            }
            else
            {
                return;
            }

            #region Calculate SLA.
            slaController = new SLAController();
            Int64 leftDays;
            Boolean wrongDataBug;
            wrongDataBug = false;
            
            DateTime dt = (DateTime)(drForSingalBug["Accept Date"].ToString() == "" ? DateTime.MinValue : drForSingalBug["Accept Date"]);
            int priority;
            if (!int.TryParse(drForSingalBug["Priority"].ToString(), out priority))
            {
                priority = 5;
            }
            int creditDate = (drForSingalBug["Vendor Days Credit"].ToString() == "") ? 0 : int.Parse(drForSingalBug["Vendor Days Credit"].ToString());

            if (priority == 5 || dt == DateTime.MinValue)
                wrongDataBug = true;

            String bugState = drForSingalBug["State"].ToString();

            if (bugState == "Closed" || bugState == "Resolved")
            {
                leftDays = slaController.getSLAForEachBug(dt, priority, creditDate, true);
            }
            else
            {
                leftDays = slaController.getSLAForEachBug(dt, priority, creditDate, false);
            }

            // Add color to rich box.
            if (!wrongDataBug && bugState != "Closed" && bugState != "Resolved")
            {
                if (leftDays <= 60 && leftDays > 20)
                {
                    richTextBoxSLA.ForeColor = Color.Green;
                    richTextBoxSLA.Text = leftDays.ToString() + " days before SLA";
                }
                else if (leftDays > 10 && leftDays <= 20)
                {
                    richTextBoxSLA.ForeColor = Color.DarkOrchid;
                    richTextBoxSLA.Text = leftDays.ToString() + " days before SLA";
                }
                else if (leftDays <= 10 && leftDays > 0)
                {
                    richTextBoxSLA.ForeColor = Color.Red;
                    richTextBoxSLA.Text = leftDays.ToString() + (leftDays == 1 ? " day before SLA" : " days before SLA");
                }
                else if (leftDays == Int64.MinValue)
                {
                    richTextBoxSLA.ForeColor = Color.Black;
                    richTextBoxSLA.Text = "No SLA";
                }
                else
                {
                    richTextBoxSLA.ForeColor = Color.Black;
                    richTextBoxSLA.Text = "Had broken SLA for " + (-leftDays).ToString() + (leftDays == 1 ? " day" : " days");
                }
            }
            else
            {
                richTextBoxSLA.ForeColor = Color.Black;
                richTextBoxSLA.Text = "No SLA";
            }
            #endregion

            try
            {
                textBoxTitle.Text = drForSingalBug["Title"].ToString();
                textBoxPath.Text = drForSingalBug["Area Path"].ToString();
                linkLabelTFSBugID.Text = drForSingalBug["ID"].ToString();
                linkLabelBugIDLink.Text = drForSingalBug["Bug ID"].ToString();

                webBrowserHistory.DocumentText = drForSingalBug["History"].ToString();

                // Common Part.
                textBoxIssueType.Text = drForSingalBug["Issue"].ToString();
                textBoxStatus.Text = drForSingalBug["State"].ToString();
                textBoxPriority.Text = drForSingalBug["Priority"].ToString();
                textBoxAcceptDate.Text = drForSingalBug["Accept Date"].ToString();
                textBoxVersion.Text = drForSingalBug["Version"].ToString();

                // Contact Part.
                textBoxAssignedTo.Text = drForSingalBug["Assigned To"].ToString();
                textBoxAssignedPM.Text = drForSingalBug["PM Assigned"].ToString();
                textBoxAssignedDev.Text = drForSingalBug["Dev Assigned"].ToString();
                textBoxAssignedTest.Text = drForSingalBug["Test Assigned"].ToString();
                textBoxEE.Text = drForSingalBug["Escalation Engineer Assigned"].ToString();
                textBoxCodeReviewer.Text = drForSingalBug["Code Reviewer"].ToString();
                textBoxTestReviewer.Text = drForSingalBug["Test Reviewer"].ToString();

                // Todo: EE part. Issue exist there. Complex html can not be analysed.
                webBrowserBFPD.DocumentText = drForSingalBug["Business Problem Description"].ToString();
                String test = drForSingalBug["Business Impact"].ToString();
                webBrowserBPAI.DocumentText = test;
                webBrowserTDE.DocumentText = drForSingalBug["Technical Description"].ToString();

                // Dev part.
                webBrowserBP.DocumentText = drForSingalBug["Business Problem"].ToString();
                webBrowserSolution.DocumentText = drForSingalBug["Solution"].ToString();

                textBoxRisk.Text = "Development Risk category: " + drForSingalBug["Development Risk"].ToString() + "\r\n\r\n"
                    + drForSingalBug["Risk Description"].ToString();
                textBoxImpact.Text = "Fix impact category: " + drForSingalBug["Fix Impact"].ToString() + "\r\n\r\n"
                    + drForSingalBug["Fix Impact Description"].ToString();

                webBrowserTest.DocumentText = drForSingalBug["Test Recommendations"].ToString();
            }
            catch
            {
                // When the setup SettingInfo.AdammaSetting.ProSetting.PreLoadingAllSingalBugsData first change from false to ture. Current
                // Datatable will not contains the reqiured fields. Need wait for the new in coming data loading async.
                this.loadAndDisplayTeamBugsDataAsync();
                return;
            }
        }
        #endregion 

        /// <summary>
        /// Asynchronous display the team data or the group data.
        /// </summary>
        private void loadAndDisplayTeamBugsDataAsync()
        {
            if (!workerForGroupBugs.IsBusy)
            {
                toolStripProgressBarLoadingStatus.Visible = true;
                toolStripStatusLabelLoadingStatus.Visible = true;

                toolStripStatusLabelEachStatus.Visible = false;
                toolStripStatusLabelTotalBugsInCurTeam.Visible = false;

                // For loading picture.
                pictureBoxWhite1.Visible = true;
                pictureBoxLoadingForGridView.Visible = true;
                dataGridViewBugs.Visible = false;
                
                // For the loading text.
                timerForGridview.Enabled = true;

                // Cancel the query in the second query
                this.cancelQuery();

                workerForGroupBugs.RunWorkerAsync();
            }

            if (!workerForChartGroupBugs.IsBusy)
            {
                zedGraphControlWholeTeam.Visible = false;
                pictureBoxWhite2.Visible = true;
                pictureBoxLoading.Visible = true;

                workerForChartGroupBugs.RunWorkerAsync();
            }            
        }

        private void loadAndDisplaySingalBugDataAsync()
        {
            if (!workerForOneBug.IsBusy) // && dataGridViewBugs.Rows.Count > 0)
            {
                GlobalInfo.CurAsyncLoadingType = AsyncLoadingType.SignleBug;
                workerForOneBug.RunWorkerAsync();
            }
        }

        /// <summary>Load all data for form.
        /// 1. Load default setting from xml and query database for data.
        /// 2. Load current team's chart data.
        /// </summary>
        private void loadAllDataForForm()
        {
            this.queryGroupBugsToDataTable();
        }

        private void loadAllDataForSingalBug()
        {
            this.queryOneBugWithID();
        }

        private void queryGroupBugsToDataTable()
        {
            if (SettingInfo == null)
            {
                Debug.Assert(false, "SettingInfo can not be null");
                return;
            } 

            ExecuteTFSQuery queryEntry = new ExecuteTFSQuery("http://vstfmbs:8080/tfs/mbs");
            Dictionary<String, int> tfsFields = SettingInfo.TFSFieldsValue.GetEnabledTFSFieldsAndWidth();
            List<String> tfsFieldsForSingleBug = GlobalInfo.WiqFieldsForFormControls;

            if (tfsFields.Count() == 0)
                return;

            try
            {
                if (GlobalInfo.CurrentTeam.Trim() == "" && GlobalInfo.CurAsyncLoadingType == AsyncLoadingType.TeamBugs)
                    return;

                QuerySQLGenerator sqlGenerator;

                // Define which Aysnchronous mode need to choose and which fields should be load.
                if (SettingInfo.AdammaSetting.ProSetting.PreLoadingAllSingalBugsData)
                {
                    sqlGenerator = new QuerySQLGenerator(tfsFields, tfsFieldsForSingleBug, true);
                }
                else
                {
                    sqlGenerator = new QuerySQLGenerator(tfsFields);
                }                

                string querySTR ;

                if (GlobalInfo.CurAsyncLoadingType == AsyncLoadingType.QueryBugs)
                {
                    string sqlfromQuery = CommonUtilities.getSqlTxtFromWiq(GlobalInfo.CurrentQueryFileLocation);
                    
                    if (sqlfromQuery != null)
                    {
                        GridViewFilterController = new ExpressionController(QueryType.WiqSqlQuery);
                        GridViewFilterController.setExpressionForWiqSqlQuery(
                            sqlfromQuery,
                            SettingInfo.AdammaSetting.ProSetting.UseCustomizedFieldWhenExcuteQuery);

                        querySTR = sqlGenerator.ConstructSQLFromTypeAndExpression(QueryType.WiqSqlQuery, GridViewFilterController);
                    }
                    else
                    {
                        querySTR = sqlGenerator.ConstructSQLFromTypeAndExpression(QueryType.TeamQuery, GridViewFilterController);
                    }
                }
                else
                {
                    querySTR = sqlGenerator.ConstructSQLFromTypeAndExpression(QueryType.TeamQuery, GridViewFilterController);
                }
                GlobalInfo.TfsFieldsToBeSetInvisiableInGridView = CommonUtilities.GetDisplayNamesThoughFieldName(SettingInfo.TFSFieldsValue, sqlGenerator.tfsFieldsToBeSetInvisiableInGridView, true);
                GlobalInfo.MandantoryFields = sqlGenerator.MandantoryFields;
                GlobalInfo.GridViewBugsDT = queryEntry.QueryTeamFoundationServerViaWiq(querySTR);
            }
            catch 
            {
                MessageBox.Show("Error exist during the query!");
            }
        }

        /// <summary>
        /// TODO: This method will cause a performance loss.
        /// </summary>
        private void loadChartGroupData()
        {
            // Get all Enabled TeamNames For ChartGroup In Order
            String[] AllEnabledTeamNamesForChartGroupInOrder = SettingInfo.TeamInfo.getALLEnabledTeamsForChartGroupInOrder();

            if (AllEnabledTeamNamesForChartGroupInOrder == null || AllEnabledTeamNamesForChartGroupInOrder.Count() == 0)
                return;

            GlobalInfo.teamChartGroupControl = new TeamChartGroupController();
            foreach (String teamName in AllEnabledTeamNamesForChartGroupInOrder)
            {
                ChartGridFilterController = new ExpressionController(QueryType.ChartGroupQuery);

                //Create the pure tfsFields for SLA for current team.
                ChartGridFilterController.setExpression(
                        SettingInfo.TeamInfo.GetAllEnabledTeamMembersToDictionaryThroughTeamName(teamName),
                        GlobalInfo.CurBugType,
                        GlobalInfo.CurBugStatus,
                        GlobalInfo.CurBugDateType);
                
                ExecuteTFSQuery queryEntry = new ExecuteTFSQuery("http://vstfmbs:8080/tfs/mbs");
                                
                QuerySQLGenerator sqlGenerator = new QuerySQLGenerator();

                String querySTR = sqlGenerator.ConstructSQLFromTypeAndExpression(
                    QueryType.ChartGroupQuery,
                    ChartGridFilterController);

                // Add the calculated SLA datatable to TeamChartController for a whole calculation.
                GlobalInfo.teamChartGroupControl.AddTeamNamesWithCorrespondentData(
                    teamName,
                    queryEntry.QueryTeamFoundationServerViaWiq(querySTR));
            }

            // Setup the Gloabl value for the Asynchronous display process.
            GlobalInfo.CharGroupDT = GlobalInfo.teamChartGroupControl.DtForFinallyChart;
        }


        private void queryOneBugWithID()
        {
            if (GlobalValue.curSelectedPSID == "" && GlobalValue.curSelectedTFSID == "")
                return;

            GridViewFilterController = new ExpressionController(QueryType.SignalBugQuery);

            if (GlobalValue.curSelectedPSID != "")
            {
                GridViewFilterController.setExpressionForSingalBug(int.Parse(GlobalValue.curSelectedPSID), false);
            }
            else
            {
                GridViewFilterController.setExpressionForSingalBug(int.Parse(GlobalValue.curSelectedTFSID), true);
            }
            
            ExecuteTFSQuery query = new ExecuteTFSQuery("http://vstfmbs:8080/tfs/mbs");
            List<String> tfsFieldsForSingleBug = GlobalInfo.WiqFieldsForFormControls;
            
            //try
            //{
            QuerySQLGenerator sql = new QuerySQLGenerator(tfsFieldsForSingleBug);
            if (GlobalInfo.CurrentTeam.Trim() == "")
                return;

            string signalBugQuerySTR = sql.ConstructSQLFromTypeAndExpression(QueryType.SignalBugQuery, GridViewFilterController);

            GlobalInfo.CharDTForSignalBug = query.QueryTeamFoundationServerViaWiq(signalBugQuerySTR);
            //}
            //catch (Exception ex)
            //{
            //    MessageBox.Show(ex.Message);
            //}
        }

        private void displayColorAndDataForGridview()
        {
            toolStripStatusLabelLoadingStatus.Text = "Loading data complete! Display";
            if (GlobalInfo.GridViewBugsDT == null)
                return;

            this.LoadLineAndColorInGridView();
        }

        private void displayInformationData()
        {
            toolStripStatusLabelEachStatus.Visible = true;
            toolStripStatusLabelTotalBugsInCurTeam.Visible = true;

            String eachStatusTxt = "";

            int totalBugsNum = GlobalInfo.breakSLA
                + GlobalInfo.lessThanTen
                + GlobalInfo.lessThanSixty
                + GlobalInfo.lessThanTwenty
                + GlobalInfo.noSLABugNum
                + GlobalInfo.ClosedOrResolvedBugNum;

            if (GlobalInfo.CurAsyncLoadingType == AsyncLoadingType.QueryBugs)
            {
                toolStripStatusLabelTotalBugsInCurTeam.Text = "There are total " + totalBugsNum.ToString() + " bugs in " + Path.GetFileNameWithoutExtension(GlobalInfo.CurrentQueryFileLocation) + " Query.     ";
            }
            else
            {
                toolStripStatusLabelTotalBugsInCurTeam.Text = "There are total " + totalBugsNum.ToString() + " bugs in " + GlobalInfo.CurrentTeam + " team.     ";
            }
            eachStatusTxt += GlobalInfo.lessThanTen.ToString() + " bugs in danger, ";
            eachStatusTxt += GlobalInfo.lessThanTwenty.ToString() + " bugs less than twenty days, ";
            eachStatusTxt += GlobalInfo.lessThanSixty.ToString() + " bugs less than sixty days";

            if (GlobalInfo.breakSLA != 0)
            {
                eachStatusTxt += ", " + GlobalInfo.breakSLA.ToString() + " bugs had break SLA";
            }

            if (GlobalInfo.noSLABugNum != 0)
            {
                eachStatusTxt += ", " + GlobalInfo.noSLABugNum.ToString() + " bugs without Priority or AcceptDate";
            }

            if (GlobalInfo.ClosedOrResolvedBugNum != 0)
            {
                eachStatusTxt += ", " + GlobalInfo.ClosedOrResolvedBugNum.ToString() + " bugs had closed or resolved.";
            }

            toolStripStatusLabelEachStatus.Text = eachStatusTxt;

            GlobalInfo.breakSLA = 0;
            GlobalInfo.lessThanTen = 0;
            GlobalInfo.lessThanSixty = 0;
            GlobalInfo.lessThanTwenty = 0;
            GlobalInfo.noSLABugNum = 0;
            GlobalInfo.ClosedOrResolvedBugNum = 0;

            toolStripProgressBarLoadingStatus.Value = 10;

            toolStripProgressBarLoadingStatus.Visible = false;
            toolStripStatusLabelLoadingStatus.Visible = false;
            toolStripProgressBarLoadingStatus.Value = 0;
            toolStripStatusLabelLoadingStatus.Text = "";
            toolStripStatusLabelTotalBugsInCurTeam.Visible = true;
        }
        
        /// <summary>
        /// Display after multithread loaded data for chart.
        /// </summary>
        private void displayChartData()
        {
            chartSLA.DataSource = GlobalInfo.ChartDT;
            chartSLA.Series[0].YValueMembers = "Break SLA";
            chartSLA.Series[1].YValueMembers = "<=10d";
            chartSLA.Series[2].YValueMembers = "<=20d";
            chartSLA.Series[3].YValueMembers = "<=60d";
            chartSLA.Series[4].YValueMembers = "NoSLA";
            chartSLA.Series[5].YValueMembers = "ClosedOrResolved";

            chartSLA.DataBind();
        }

        private void LoadLineAndColorInGridView()
        {
            if (GlobalInfo.TfsFieldsToBeSetInvisiableInGridView == null)
            {
                Debug.Assert(GlobalInfo.TfsFieldsToBeSetInvisiableInGridView != null, "TFS fields for invisiable get wrong.");
                return;
            }

            if (GlobalInfo.GridViewBugsDT.Columns.Contains("Number") &&
                GlobalInfo.GridViewBugsDT.Columns.Contains("Left Days"))
            {
                dataGridViewBugs.DataSource = GlobalInfo.GridViewBugsDT;
            }
            else
            {
                // Add the number,SLA column to datatable.
                slaController = new SLAController();

                // This is very important to clear the fields order memery.
                dataGridViewBugs.DataSource = null;
                dataGridViewBugs.DataSource = slaController.CalcSLAToDatatable(GlobalInfo.GridViewBugsDT, true);

                // Update the filter "avaliable fields" for the grid view.
                toolStripComboBoxField.Items.Clear();
                toolStripComboBoxField.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                toolStripComboBoxField.AutoCompleteSource = AutoCompleteSource.ListItems;

                // Add the customized field "Left Days"
                toolStripComboBoxField.Items.Add("Left Days");
                foreach (String str in GlobalInfo.lookUpForGridBug.AvaliableFields)
                {
                    toolStripComboBoxField.Items.Add(str);
                }

                // Prepare the team SLA data for chart here.
                GlobalInfo.ChartDT = slaController.getCurTeamBugsStatus();
                GlobalInfo.lessThanTen = slaController.LessThanTen;
                GlobalInfo.lessThanSixty = slaController.LessThanSixty;
                GlobalInfo.lessThanTwenty = slaController.LessThanTwenty;
                GlobalInfo.breakSLA = slaController.BreakSLA;
                GlobalInfo.noSLABugNum = slaController.NoSLABugNum;
                GlobalInfo.ClosedOrResolvedBugNum = slaController.ClosedOrResolvedBugNum;
            }

            //Refresh the color due to SLA left date.
            this.RefreshLineColorInGridView();

            for (int columnNum = 0; columnNum < dataGridViewBugs.Columns.Count; columnNum++)
            {
                if (columnNum == 0 || columnNum == 1)
                {
                    // First column width 50, second (SLA) column width is 70
                    dataGridViewBugs.Columns[columnNum].Width = 50 + columnNum * 20;
                }
                else if (GlobalInfo.TfsFieldsToBeSetInvisiableInGridView.Contains<String>(dataGridViewBugs.Columns[columnNum].Name))
                {
                    dataGridViewBugs.Columns[columnNum].Width = 100;
                }
                else
                {
                    // Get the field width from Setting dynamic.
                    dataGridViewBugs.Columns[columnNum].Width = SettingInfo.TFSFieldsValue.FindTFSFieldsWidthWithFieldDisplayName(dataGridViewBugs.Columns[columnNum].Name.ToString());
                }
            }

            // Set the mandantory key to unvisiable if it not user selected.
            foreach (DataGridViewTextBoxColumn column in dataGridViewBugs.Columns)
            {
                if (GlobalInfo.TfsFieldsToBeSetInvisiableInGridView.Contains(column.Name))
                {
                    dataGridViewBugs.Columns[column.Name].Visible = false;
                }
                else
                {
                    dataGridViewBugs.Columns[column.Name].Visible = true;
                }
            }

            dataGridViewBugs.Refresh();
        }

        /// <summary>
        /// Refresh and recalculate each bug's SLA date. This action is needed while sort the gridview.
        /// </summary>
        private void RefreshLineColorInGridView()
        {
            for (int i = 0; i < dataGridViewBugs.Rows.Count; i++)
            {
                String bugState = dataGridViewBugs.Rows[i].Cells["State"].Value.ToString();
                Int64 leftDays = dataGridViewBugs.Rows[i].Cells["Left Days"].Value.ToString() == "" ? Int64.MinValue :
                    Int64.Parse(dataGridViewBugs.Rows[i].Cells["Left Days"].Value.ToString());

                #region add color to lines. 
                if ((leftDays != Int64.MinValue) && bugState != "Closed" && bugState != "Resolved")
                {
                    if (leftDays <= 60 && leftDays > 20)
                    {
                        dataGridViewBugs.Rows[i].DefaultCellStyle.BackColor = Color.Green;
                    }
                    else if (leftDays > 10 && leftDays <= 20)
                    {
                        dataGridViewBugs.Rows[i].DefaultCellStyle.BackColor = Color.Yellow;
                    }
                    else if (leftDays <= 10 && leftDays > 0)
                    {
                        dataGridViewBugs.Rows[i].DefaultCellStyle.BackColor = Color.Red;
                    }
                    else if (leftDays == Int64.MinValue)
                    {
                        dataGridViewBugs.Rows[i].DefaultCellStyle.BackColor = Color.White;
                    }
                    else
                    {
                        dataGridViewBugs.Rows[i].DefaultCellStyle.BackColor = Color.Beige;
                    }
                }
                #endregion
            }
        }
        
        /// <summary>
        /// Update the color in grid view. When sort mode changed in gridview.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridViewBugs_ColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            this.RefreshLineColorInGridView();
        }


        /// <summary>
        /// Display after multithread got data for the team group chart.
        /// </summary>
        private void displayChartPerDevTestData()
        {
            ZedGraphController pieChart = new ZedGraphController(GlobalInfo.GridViewBugsDT);
            pieChart.CreateGraph(zedGraphControlDevOnHandBugs, ChartType.DevPieChart);
            pieChart.CreateGraph(zedGraphControlTestOnHandBugs, ChartType.TestPieChart);
        }

        //
        private void displayChartGroupData()
        {
            ZedGraphController barChart = new ZedGraphController(GlobalInfo.CharGroupDT);
            barChart.CreateGraph(zedGraphControlWholeTeam, ChartType.BarChart, GlobalInfo.ChartGroupBartype);

            zedGraphControlWholeTeam.Visible = true;
            pictureBoxWhite2.Visible = false;
            pictureBoxLoading.Visible = false;
        }
        #endregion Asychoronous loading process.



        #region Common Forms event.
        /// <summary>
        /// Save the user last value when form closing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Adamma_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.updateCurrentQueryExpression();
        }

        private void buttonSetup_Click(object sender, EventArgs e)
        {
            this.openSetup();
        }

        /// <summary>
        /// This even will be called when : 
        /// 1. change the combobox of team or status.
        /// 2. Click the refresh button.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                if (GlobalInfo.CurAsyncLoadingType == AsyncLoadingType.SignleBug)
                    GlobalInfo.CurAsyncLoadingType = AsyncLoadingType.TeamBugs;

                // If the sender is from combobox of team or status, change the loading type from any to Team bugs.
                if (sender.GetType() == typeof(ComboBox))
                {
                    ComboBox senderObject = (ComboBox)sender;

                    if ((senderObject.Name == "comboBoxTeam" || senderObject.Name == "comboBoxStatus") &&
                        comboBoxTeam.Text != "" && comboBoxStatus.Text != "")
                        GlobalInfo.CurAsyncLoadingType = AsyncLoadingType.TeamBugs;
                }

                this.updateCurrentQueryExpression();

                if (sender.GetType() == typeof(ComboBox) && ((ComboBox)sender).Name == "comboBoxStatus")
                    return;

                this.loadAndDisplayTeamBugsDataAsync();
            }
            catch
            {
                MessageBox.Show("Data refresh failure!");
            }
        }

        ErrorProvider errMain = new ErrorProvider();
        private void comboBox_Leave(object sender, EventArgs e)
        {
            if (SettingInfo == null)
                return;

            if (!SettingInfo.TeamNames.Contains(comboBoxTeam.Text) && comboBoxTeam.Text.Trim() != "")
            { comboBoxTeam.Text = ""; errMain.SetError(sender as ComboBox, "Invalid Team Name"); }

            if (comboBoxStatus.Text.Trim() != "")
            {
                try { CommonUtilities.ParseNum<BugStatus>(comboBoxStatus.Text); }
                catch { comboBoxStatus.Text = ""; errMain.SetError(sender as ComboBox, "Invalid Bug Status"); }
            }
        }

        private void dataGridViewBugs_DataError(object sender, DataGridViewDataErrorEventArgs e)
        {
            // Do nothing in case of data error.
            return;
        }

        private void dataGridViewBugs_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (dataGridViewBugs.SelectedRows.Count == 0)
                    return;

                if (dataGridViewBugs.Columns.Contains("ID"))
                    GlobalValue.curSelectedTFSID = dataGridViewBugs.SelectedRows[0].Cells["ID"].Value.ToString();

                if (dataGridViewBugs.Columns.Contains("BugID"))
                    GlobalValue.curSelectedTFSID = dataGridViewBugs.SelectedRows[0].Cells["BugID"].Value.ToString();

                if (SettingInfo.AdammaSetting.ProSetting.PreLoadingAllSingalBugsData)
                {
                    this.displaySingalBugInformation();
                }
                else
                {
                    // Set Team labels unvisiable.
                    toolStripStatusLabelEachStatus.Visible = false;
                    toolStripStatusLabelTotalBugsInCurTeam.Visible = false;

                    toolStripStatusLabelLoadingStatus.Text = "";
                    toolStripProgressBarLoadingStatus.Visible = true;
                    toolStripStatusLabelLoadingStatus.Visible = true;

                    // Use Timer to control the text in "toolStripStatusLabelLoadingStatus"
                    timerForSignalBug.Enabled = true;

                    this.loadAndDisplaySingalBugDataAsync();
                }
            }
            catch
            {
                MessageBox.Show("Singal bug load failure!");
            }
        }

        private void timerForSignalBug_Tick(object sender, EventArgs e)
        {
            if (toolStripStatusLabelLoadingStatus.Text.Trim() == "" || toolStripStatusLabelLoadingStatus.Text.Contains("Loading completed"))
            {
                toolStripStatusLabelLoadingStatus.Text = "Loading details for DAXSE_" + GlobalValue.curSelectedTFSID + " from TFS.";
            }
            else
            {
                String source = toolStripStatusLabelLoadingStatus.Text.ToString();
                if (toolStripProgressBarLoadingStatus.Value <= 8)
                {
                    toolStripStatusLabelLoadingStatus.Text = source + ".";
                    toolStripProgressBarLoadingStatus.Value += 2;
                }
                else
                {
                    toolStripStatusLabelLoadingStatus.Text = "Loading data for DAXSE_" + GlobalValue.curSelectedTFSID + " from TFS.";
                    toolStripProgressBarLoadingStatus.Value = 0;
                }
            }
        }

        private void timerForGridview_Tick(object sender, EventArgs e)
        {
            if (toolStripStatusLabelLoadingStatus.Text.Trim() == "" || toolStripStatusLabelLoadingStatus.Text.Contains("Loading completed"))
            {
                toolStripStatusLabelLoadingStatus.Text = "Loading data from TFS.";
            }
            else
            {
                String source = toolStripStatusLabelLoadingStatus.Text.ToString();
                if (toolStripProgressBarLoadingStatus.Value <= 8)
                {
                    toolStripStatusLabelLoadingStatus.Text = source + ".";
                    toolStripProgressBarLoadingStatus.Value += 2;
                }
                else
                {
                    toolStripStatusLabelLoadingStatus.Text = "Loading data from TFS.";
                    toolStripProgressBarLoadingStatus.Value = 0;
                }
            }
        }

        private void linkLabelTFSBugID_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (linkLabelTFSBugID.Text == "" || linkLabelTFSBugID.Text == "TFSLink")
                return;

            string id = linkLabelTFSBugID.Text;
            BugsNavigator.NavigateToTFSBug(id);
        }

        private void linkLabelBugIDLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (linkLabelBugIDLink.Text == "" || linkLabelBugIDLink.Text == "PSLink")
                return;

            string id = linkLabelBugIDLink.Text;
            BugsNavigator.NavigateToPSBug(id, false);
        }

        /// <summary>
        ///  Grid Movement control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButtonMoveClick(object sender, EventArgs e)
        {
            if (dataGridViewBugs.Rows.Count == 0)
                return;

            if (dataGridViewBugs.SelectedRows.Count > 0)
            {
                int currentIndex = dataGridViewBugs.SelectedRows[0].Index;

                dataGridViewBugs.ClearSelection();
                switch (((ToolStripButton)sender).Name)
                {
                    case "toolStripButtonFirst":
                        {
                            if (dataGridViewBugs.Rows.Count > 0)
                            {
                                dataGridViewBugs.Rows[0].Selected = true;
                                dataGridViewBugs.FirstDisplayedScrollingRowIndex = 0;
                            }
                            break;
                        }
                    case "toolStripButtonBack":
                        {
                            if (currentIndex > 0)
                            {
                                dataGridViewBugs.Rows[currentIndex - 1].Selected = true;
                                dataGridViewBugs.FirstDisplayedScrollingRowIndex = currentIndex - 1;
                            }
                            break;
                        }
                    case "toolStripButtonMoveForward":
                        {
                            if (currentIndex < dataGridViewBugs.Rows.Count - 1)
                            {
                                dataGridViewBugs.Rows[currentIndex + 1].Selected = true;
                                dataGridViewBugs.FirstDisplayedScrollingRowIndex = currentIndex + 1;
                            }
                            break;
                        }
                    case "toolStripButtonMoveLast":
                        {
                            if (dataGridViewBugs.Rows.Count > 0)
                            {
                                dataGridViewBugs.Rows[dataGridViewBugs.Rows.Count - 1].Selected = true;
                                dataGridViewBugs.FirstDisplayedScrollingRowIndex = dataGridViewBugs.Rows.Count - 1;
                            }
                            break;
                        }
                }
            }
            else
            {
                dataGridViewBugs.Rows[0].Selected = true;
            }
        }

        private void treeViewQuery_AfterSelect(object sender, TreeViewEventArgs e)
        {
            String realLoc = this.relativeAddress2AbsolutelyAddress(((TreeView)sender).SelectedNode.FullPath);
            if (!File.Exists(realLoc) || workerForGroupBugs.IsBusy)
            {
                e.Node.SelectedImageIndex = 3;
                return;
            }
            else
                e.Node.SelectedImageIndex = 2;

            GlobalInfo.CurrentQueryFileLocation = realLoc;
            GlobalInfo.CurAsyncLoadingType = AsyncLoadingType.QueryBugs;

            this.loadAndDisplayTeamBugsDataAsync();
        }

        private void toolStripButtonGoToBug_Click(object sender, EventArgs e)
        {
            GoToBug frmGoTo = new GoToBug();
            frmGoTo.ShowDialog();
        }

        private void toolStripComboBoxField_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update expression.
            GlobalInfo.lookUpForGridBug.CurFieldName = toolStripComboBoxField.Text;
            toolStripComboBoxCondition.Items.Clear();
            foreach (String str in GlobalInfo.lookUpForGridBug.CurExpression)
            {
                toolStripComboBoxCondition.Items.Add(str);
            }
            toolStripComboBoxCondition.Text = "Equals";

            // Update target string.
            toolStripTextBoxValue.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            toolStripTextBoxValue.AutoCompleteSource = AutoCompleteSource.CustomSource;
            AutoCompleteStringCollection targetString = new AutoCompleteStringCollection();
            foreach (String str in GlobalInfo.lookUpForGridBug.TargetSearchString)
            {
                targetString.Add(str);
            }
            toolStripTextBoxValue.AutoCompleteCustomSource = targetString;
        }

        private void comboBoxDateQueryFields_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (CommonUtilities.ParseNum<DateQueryFields>(comboBoxDateQueryFields.Text.Trim()) != DateQueryFields.EmptyField)
            {
                checkBoxFromDate.Enabled = true;
                checkBoxToDate.Enabled = true;
                checkBoxFromDate.Checked = true;
                checkBoxToDate.Checked = true;

                dateTimePickerFromDate.Enabled = true;
                dateTimePickerToDate.Enabled = true;
            }
            else
            {
                checkBoxFromDate.Enabled = false;
                checkBoxToDate.Enabled = false;
                checkBoxFromDate.Checked = false;
                checkBoxToDate.Checked = false;

                dateTimePickerFromDate.Enabled = false;
                dateTimePickerToDate.Enabled = false;
            }
        }

        private void comboBoxDateQueryFields_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete)
            {
                checkBoxFromDate.Enabled = false;
                checkBoxToDate.Enabled = false;
                checkBoxFromDate.Checked = false;
                checkBoxToDate.Checked = false;

                dateTimePickerFromDate.Enabled = false;
                dateTimePickerToDate.Enabled = false;
            }
        }

        private void toolStripTextBoxValue_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.Back || e.KeyCode == Keys.Delete) 
                && GlobalInfo.TmpGridviewBugDTBeforeSort != null)
            {
                this.cancelQuery();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                this.startQuery();
            }
        }

        private void toolStripButtonExecuteQuery_Click(object sender, EventArgs e)
        {
            if (toolStripButtonExecuteQuery.Text == "Execute Query")
            {
                this.startQuery();                
            }
            else
            {
                this.cancelQuery();                
            }
        }

        private void toolStripComboBoxCondition_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.startQuery();
        }

        private void exportSelectLinesToTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataTable tempForSelectRows = this.getSelectedLinesFromDataGridview();
            if (tempForSelectRows == null)
                MessageBox.Show("Export failure!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                FileExportController.ExportToTextFile(tempForSelectRows);
        }

        private void exportSelectLinesToExcelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataTable tempForSelectRows = this.getSelectedLinesFromDataGridview();
            if (tempForSelectRows == null)
                MessageBox.Show("Export failure!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                FileExportController.ExportToExcelFile(tempForSelectRows);
        }

        private void sendSelectLinesThroughEmailToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataTable tempForSelectRows = this.getSelectedLinesFromDataGridview();
            if (tempForSelectRows == null)
                MessageBox.Show("Export failure!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            else
                FileExportController.ExportToXmlFile(tempForSelectRows);
        }

        private void refreshQueryFilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.loadQueryFilesToTreeViews();
        }

        private void toolStripButtonHelp_Click(object sender, EventArgs e)
        {
            this.openHelpFile();
        }
        #endregion


        
        #region Common form functionalities
        #region Expression update
        /// <summary>
        /// Need update expression before start query.
        /// </summary>
        private void updateCurrentQueryExpression()
        {
            try
            {
                SettingInfo.AdammaSetting.HistorySetting.HistoryLoadingTeam = GlobalInfo.CurrentTeam;
                SettingInfo.AdammaSetting.HistorySetting.HistoryLoadingBugStatus = GlobalInfo.CurBugStatus;

                GridViewFilterController = new ExpressionController(QueryType.TeamQuery);


                GlobalInfo.CurrentTeam = comboBoxTeam.Text.ToString();
                GlobalInfo.CurBugStatus = CommonUtilities.ParseNum<BugStatus>(comboBoxStatus.Text);


                GlobalInfo.CurBugDateType = new BugDateTypeCLS(CommonUtilities.ParseNum<DateQueryFields>(comboBoxDateQueryFields.Text),
                                                            checkBoxFromDate.Checked ? dateTimePickerFromDate.Value : DateTime.MinValue,
                                                            checkBoxToDate.Checked ? dateTimePickerToDate.Value : DateTime.MinValue);

                GlobalInfo.CurBugType = updateCurrentBugType();
                SettingInfo.AdammaSetting.HistorySetting.HistoryLoadingBugType = BugTypeCLS.ConvertBugTypesToStrings(GlobalInfo.CurBugType);

                GridViewFilterController.setExpression(
                    SettingInfo.TeamInfo.GetAllEnabledTeamMembersToDictionaryThroughTeamName(GlobalInfo.CurrentTeam),
                    GlobalInfo.CurBugType,
                    GlobalInfo.CurBugStatus,
                    GlobalInfo.CurBugDateType);

                SettingInfo.AdammaSetting.saveSetupInfo(SettingInfo.SettingFileLocation);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private BugType updateCurrentBugType(Boolean _updateFromControlToValue = true)
        {
            if (_updateFromControlToValue)
            {
                BugType curBugType = new BugType();

                if (checkedListBoxTypeRange.CheckedItems.Count != 0)
                {
                    foreach (string str in checkedListBoxTypeRange.CheckedItems)
                    {
                        switch (CommonUtilities.ParseNum<BugType>(str))
                        {
                            case BugType.RequestforHotfix: curBugType = curBugType | BugType.RequestforHotfix; break;
                            case BugType.CodeDefect: curBugType = curBugType | BugType.CodeDefect; break;
                            case BugType.CollaborationRequest: curBugType = curBugType | BugType.CollaborationRequest; break;
                            case BugType.DesignChangeRequest: curBugType = curBugType | BugType.DesignChangeRequest; break;
                            case BugType.OverLayeringIssue: curBugType = curBugType | BugType.OverLayeringIssue; break;
                            case BugType.Test: curBugType = curBugType | BugType.Test; break;
                        }
                    }
                }
                return curBugType;
            }
            else
            {
                return BugType.RequestforHotfix;
            }
        }
        #endregion 

        /// <summary>
        /// 1. Load/Refresh Teams, and time interval from Configuration.
        /// </summary>
        private void refreshDataWithAdammaSetting()
        {
            // Load interval of from user's setting.
            if (SettingInfo.AdammaSetting.ProSetting.RefreshCycle != 0)
            {
                timerRefresh.Enabled = true;
                timerRefresh.Interval = SettingInfo.AdammaSetting.ProSetting.RefreshCycle * 1000;
            }
            else
                timerRefresh.Enabled = false;

            // Load Teams from Configuration.
            comboBoxTeam.Items.Clear();
            comboBoxTeam.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
            comboBoxTeam.AutoCompleteSource = AutoCompleteSource.ListItems;
            foreach (string teamName in SettingInfo.TeamNames)
            {
                comboBoxTeam.Items.Add(teamName);
            }

            GlobalInfo.ChartGroupBartype = CommonUtilities.ParseNum<ZedGraph.BarType>(SettingInfo.AdammaSetting.ProSetting.BarType);
            ZedGraphController.FreshChartGroupBartype(zedGraphControlWholeTeam, GlobalInfo.ChartGroupBartype);
        }

        private void openSetup()
        {
            SetUp frmSetup = new SetUp();
            frmSetup.ShowDialog();
            SettingInfo = new Setting();
            this.refreshDataWithAdammaSetting();
            this.loadQueryFilesToTreeViews();
        }

        /// <summary>
        /// refresh the query files belows to the folder
        /// </summary>
        private void loadQueryFilesToTreeViews()
        {
            try
            {
                // refresh the query files belows to the folder
                treeViewQuery.Nodes.Clear();
                treeViewQuery.ImageList = imageListTreeNode;

                var stack = new Stack<TreeNode>();
                var rootDirectory = new DirectoryInfo(SettingInfo.QueryLocation);
                var node = new TreeNode(rootDirectory.Name) { Tag = rootDirectory };
                node.ImageIndex = 0;
                stack.Push(node);

                while (stack.Count > 0)
                {
                    var currentNode = stack.Pop();
                    var directoryInfo = (DirectoryInfo)currentNode.Tag;

                    try
                    {
                        foreach (var directory in directoryInfo.GetDirectories())
                        {
                            var childDirectoryNode = new TreeNode(directory.Name) { Tag = directory };
                            currentNode.Nodes.Add(childDirectoryNode);
                            childDirectoryNode.ImageIndex = 0;
                            stack.Push(childDirectoryNode);
                        }
                        foreach (var file in directoryInfo.GetFiles())
                        {
                            if (Path.GetExtension(file.ToString()) == ".wiq")
                            {
                                TreeNode tnSql = new TreeNode(file.Name);
                                currentNode.Nodes.Add(tnSql);
                                tnSql.ImageIndex = 1;
                            }
                        }
                    }
                    catch
                    {
                        // Do nothing if the access to file denied.
                    }
                }

                treeViewQuery.Nodes.Add(node);
                treeViewQuery.ExpandAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Query Load Failure! Please choose another one.", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Global keys.
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="keyData"></param>
        /// <returns></returns>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.G)
                || keyData == (Keys.F2)
                || keyData == (Keys.Alt | Keys.K))
            {
                GoToBug frmGoTo = new GoToBug();
                frmGoTo.ShowDialog();

                return true;
            }
            else if (keyData == Keys.F5 && !workerForGroupBugs.IsBusy)
            {
                GlobalInfo.CurAsyncLoadingType = AsyncLoadingType.TeamBugs;
                this.updateCurrentQueryExpression();
                this.loadAndDisplayTeamBugsDataAsync();
            }
            else if (keyData == Keys.F3)
            {
                // Process setup 
                this.openSetup();
            }
            else if (keyData == Keys.F1)
            {
                // Help docuemnt
                this.openHelpFile();
            }
            else if (keyData == Keys.Escape)
            {
                this.Close();
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        /// <summary>
        /// Change the address from tree view to the real file path.
        /// </summary>
        /// <param name="_relativeAddress"></param>
        /// <returns></returns>
        private String relativeAddress2AbsolutelyAddress(String _relativeAddress)
        {
            String absoluteAddress = SettingInfo.QueryLocation;
            _relativeAddress = _relativeAddress.Remove(0, 9);
            return absoluteAddress + _relativeAddress;
        }

        private void cancelQuery()
        {
            if (GlobalInfo.TmpGridviewBugDTBeforeSort == null || GlobalInfo.GridViewBugsDT == null
                || GlobalInfo.TmpGridviewBugDTBeforeSort.Rows.Count == 0)
                return;

            // Restore the datatable.
            GlobalInfo.GridViewBugsDT = GlobalInfo.TmpGridviewBugDTBeforeSort.Copy();

            // Update the Color in the gridview.
            this.LoadLineAndColorInGridView();

            // Update the Dev & Test chart due to the latest sort data in Current Grid View.
            this.displayChartPerDevTestData();

            // Display the button as Execute query.
            toolStripButtonExecuteQuery.Image = imageListQuery.Images[0];
            toolStripButtonExecuteQuery.Text = "Execute Query";
        }

        private void startQuery()
        {
            String fieldForQuery = toolStripComboBoxField.Text.Trim();
            String queryExpression = toolStripComboBoxCondition.Text;
            String value = toolStripTextBoxValue.Text.Trim();

            // If current state is query, then cancel the current query first to accept new expression for query.
            if (toolStripButtonExecuteQuery.Text == "Cancel Query")
            {
                this.cancelQuery();
            }

            // Return if condition fails.
            if (GlobalInfo.GridViewBugsDT == null ||
                value == "" ||
                fieldForQuery == "" ||
                toolStripComboBoxCondition.Text.Trim() == "" ||
                GlobalInfo.GridViewBugsDT.Rows.Count == 0 ||
                !GlobalInfo.GridViewBugsDT.Columns.Contains(fieldForQuery) ||
                (queryExpression != "Equals" && queryExpression != ">=" && queryExpression != "<=" && queryExpression != "Contains"))
                return;

            // Backup the source datatale first.
            GlobalInfo.TmpGridviewBugDTBeforeSort = GlobalInfo.GridViewBugsDT.Copy();

            // Execute Query.
            DataTable results = QuickQueryController.ExecuteQuery(GlobalInfo.GridViewBugsDT, fieldForQuery, queryExpression, value);
            if (results != null)
            {
                GlobalInfo.GridViewBugsDT = results;
                // Update the Color in the gridview.
                this.LoadLineAndColorInGridView();
            }

            // Update the Dev & Test chart due to the latest sort data in Current Grid View.
            this.displayChartPerDevTestData();

            // Display the button as cancel query.
            toolStripButtonExecuteQuery.Image = imageListQuery.Images[1];
            toolStripButtonExecuteQuery.Text = "Cancel Query";
        }

        private DataTable getSelectedLinesFromDataGridview()
        {
            if (dataGridViewBugs.Rows.Count == 0 || GlobalInfo.TfsFieldsToBeSetInvisiableInGridView == null)
                return null;

            DataTable tempForSelectRows = GlobalInfo.GridViewBugsDT.Copy();

            // Remove the column which invisiable 
            foreach (String columnToRemove in GlobalInfo.TfsFieldsToBeSetInvisiableInGridView)
            {
                tempForSelectRows.Columns.Remove(columnToRemove);
            }

            if (dataGridViewBugs.SelectedRows.Count != 0)
            {
                tempForSelectRows.Rows.Clear();

                foreach (DataRow dr in dataGridViewBugs.SelectedRows)
                {
                    tempForSelectRows.Rows.Add(dr);
                }
            }

            return tempForSelectRows;
        }

        private void openHelpFile()
        {
            string helpFilePath = Application.StartupPath + "/" + "Adamma 2 Help Document.docx";
            if (File.Exists(helpFilePath))
            {
                try
                {
                    Process.Start(helpFilePath);
                }
                catch
                {
                    MessageBox.Show("Can not open help document!");
                }
            }
            else
            {
                MessageBox.Show("Help file doesn't exist!");
            }
        }
        #endregion 
    }
}
