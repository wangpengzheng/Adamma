using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WorkShop;
using System.Collections;
using System.IO;
using Microsoft.VisualBasic;
using Microsoft.Win32;
using ZedGraph;

namespace Adamma
{
    public partial class SetUp : Form
    {
        Setting SettingInfo;
        TeamSourceEdit TeamEditor;

        /// <summary>
        /// Manage the change to source setup data. Choose to save which had made a change.
        /// </summary>
        static class EditAble
        {
            public static Boolean TeamEdit = false;
            public static Boolean FieldEdit = false;
            public static Boolean SettingEdit = false;
        }

        public SetUp()
        {
            InitializeComponent();

            toolStripTeamMember.Visible = true;
            toolStripUserQuery.Visible = true;
            toolStripSelectTeamGroups.Visible = true;

            // Initialize variability
            SettingInfo = new Setting();
            
            #region Add column to Team information.
            DataGridViewTextBoxColumn dgvColumnMemberName = new DataGridViewTextBoxColumn();
            {
                dgvColumnMemberName.DataPropertyName = "MemberName";
                dgvColumnMemberName.HeaderText = "Member Name";
                dgvColumnMemberName.Width = 150;
            }

            DataGridViewTextBoxColumn dgvColumnAlias = new DataGridViewTextBoxColumn();
            {
                dgvColumnAlias.DataPropertyName = "Alias";
                dgvColumnAlias.HeaderText = "Alias";
                dgvColumnAlias.Width = 100;
            }

            DataGridViewComboBoxColumn dgvColumnRole = new DataGridViewComboBoxColumn();
            {
                dgvColumnRole.DataPropertyName = "Role";
                dgvColumnRole.HeaderText = "Role";
                dgvColumnRole.Width = 100;
                dgvColumnRole.FlatStyle = FlatStyle.System;
                dgvColumnRole.Items.AddRange("Dev", "Test", "PM");
            }

            DataGridViewTextBoxColumn dgvColumnTeam = new DataGridViewTextBoxColumn();
            {
                dgvColumnTeam.DataPropertyName = "Team";
                dgvColumnTeam.HeaderText = "Team";
                dgvColumnTeam.Width = 100;                
            }

            DataGridViewCheckBoxColumn dgvColumEnabled = new DataGridViewCheckBoxColumn();
            {
                dgvColumEnabled.DataPropertyName = "Enabled";
                dgvColumEnabled.HeaderText = "Enabled";
                dgvColumEnabled.Width = 100;
            }

            dataGridViewTeamMembers.Columns.Add(dgvColumnMemberName);
            dataGridViewTeamMembers.Columns.Add(dgvColumnAlias);
            dataGridViewTeamMembers.Columns.Add(dgvColumnRole);
            dataGridViewTeamMembers.Columns.Add(dgvColumnTeam);
            dataGridViewTeamMembers.Columns.Add(dgvColumEnabled);
            #endregion
        }

        public delegate void loadingProcess();
        private void SetUp_Load(object sender, EventArgs e)
        {
            try
            {
                loadingProcess LoadData = this.loadTeamMemberToGridView;
                LoadData += loadQueryRootfolderAndTreeView;
                LoadData += loadTeamNamesToSelection;
                LoadData += loadTFSFieldsForSelection;

                LoadData += loadUserSetting;
                LoadData += LoadTrackBarAndComments;
                LoadData += LoadChartGroupBarType;
                LoadData.Invoke();

                this.reportNews("Loading setting complete.", reportType.Info);
            }
            catch (Exception ex)
            {
                this.reportNews("Loading failed! " + ex.Message, reportType.Error);
            }
        }

        #region Event of User query part.
        private void buttonNewLocation_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog fbD = new FolderBrowserDialog();
            DialogResult result = fbD.ShowDialog();
            if (result == DialogResult.OK)
            {
                refreshQueryRootfolderAndTreeView(fbD.SelectedPath);
            }
        }
                
        private void toolStripButtonAddQuery_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Filter = "wiq query files (*.wiq)|*.wiq";
                ofd.InitialDirectory = SettingInfo.QueryLocation;
                DialogResult result = ofd.ShowDialog();
                if (result == DialogResult.OK)
                {
                    if (treeViewQuery.SelectedNode != null)
                    {
                        String curDirecotory = this.relativeAddress2AbsolutelyAddress(treeViewQuery.SelectedNode.FullPath, true);
                        if (curDirecotory.EndsWith(".wiq"))
                            curDirecotory = Path.GetDirectoryName(curDirecotory);

                        // Move the new wiq query file under the selected direcotory.
                        File.Copy(ofd.FileName, curDirecotory + "\\" + ofd.SafeFileName);
                    }
                    else
                    {
                        File.Copy(ofd.FileName, SettingInfo.QueryLocation + "\\" + ofd.SafeFileName);
                    }
                    this.reportNews("Query file import successfully.", reportType.Info);
                }
            }
            catch (Exception ex)
            {
                this.reportNews(ex.Message, reportType.Error);
                return;
            }
            this.refreshQueryRootfolderAndTreeView();
        }

        private void toolStripButtonAddFloder_Click(object sender, EventArgs e)
        {
            String folderName = Microsoft.VisualBasic.Interaction.InputBox("Input a new folder name", "New folder").Trim();

            try
            {
                if (treeViewQuery.SelectedNode != null)
                {
                    String curDirecotory = this.relativeAddress2AbsolutelyAddress(treeViewQuery.SelectedNode.FullPath, true);
                    if (curDirecotory.EndsWith(".wiq"))
                        curDirecotory = Path.GetDirectoryName(curDirecotory);

                    // Move the new wiq query file under the selected direcotory.
                    Directory.CreateDirectory(curDirecotory + "\\" + folderName);
                    this.reportNews("Folder created successfully.", reportType.Info);
                }
                else
                {
                    Directory.CreateDirectory(SettingInfo.QueryLocation + "\\" + folderName);
                    this.reportNews("Folder created successfully.", reportType.Info);
                }
            }
            catch
            {
                this.reportNews("Folder create failed! Not a valid folder name.", reportType.Error);
                return;
            }
            this.refreshQueryRootfolderAndTreeView();
        }

        private void toolStripButtonRemove_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure to delete this query?", "Delete Confirm.", MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == System.Windows.Forms.DialogResult.OK)
            {
                String realLoc = this.relativeAddress2AbsolutelyAddress(treeViewQuery.SelectedNode.FullPath, true);
                if (!File.Exists(realLoc))
                    return;
                else
                {
                    treeViewQuery.SelectedNode.Remove();
                    comboBoxQueryForLoad.Items.Remove(treeViewQuery.SelectedNode.Text);
                    treeViewQuery.Refresh();
                    File.Delete(realLoc);

                    this.reportNews("Selected files had been successfuly deleted.", reportType.Info);
                }
            }
        }

        private void toolStripButtonRefresh_Click(object sender, EventArgs e)
        {
            this.refreshQueryRootfolderAndTreeView();
        }

        private void toolStripLabelOpenCur_Click(object sender, EventArgs e)
        {
            if (SettingInfo.QueryLocation.Trim() != "" && Directory.Exists(SettingInfo.QueryLocation))
                System.Diagnostics.Process.Start(SettingInfo.QueryLocation);
        }

        private void treeViewQuery_AfterSelect(object sender, TreeViewEventArgs e)
        {
            String realLoc = this.relativeAddress2AbsolutelyAddress(((TreeView)sender).SelectedNode.FullPath, true);
            if (!File.Exists(realLoc))
            {
                e.Node.SelectedImageIndex = 3;
                return;
            }
            else
                e.Node.SelectedImageIndex = 2;

            richTextBoxSqlViewer.Text = "";
            List<String> txtForConstruct = CommonUtilities.ConstructSqlTxtFromWiq(realLoc);

            if (txtForConstruct != null)
            {
                richTextBoxSqlViewer.BorderStyle = BorderStyle.None;
                richTextBoxSqlViewer.SelectionColor = Color.Green;
                richTextBoxSqlViewer.SelectionFont = new Font("Consolas", 12f, FontStyle.Bold);
                richTextBoxSqlViewer.AppendText("TeamFoundationServer: \r\n");
                richTextBoxSqlViewer.SelectionColor = Color.Black;
                richTextBoxSqlViewer.SelectionFont = new Font("Georigia", 10f, FontStyle.Regular);
                richTextBoxSqlViewer.AppendText(txtForConstruct[0] + "\r\n\r\n");

                richTextBoxSqlViewer.SelectionColor = Color.Green;
                richTextBoxSqlViewer.AppendText("TeamProject: \r\n");
                richTextBoxSqlViewer.SelectionColor = Color.Black;
                richTextBoxSqlViewer.SelectionFont = new Font("Georigia", 14f, FontStyle.Regular);
                richTextBoxSqlViewer.AppendText(txtForConstruct[1] + "\r\n\r\n");

                richTextBoxSqlViewer.SelectionColor = Color.Green;
                richTextBoxSqlViewer.AppendText("SQL: \r\n");
                richTextBoxSqlViewer.SelectionColor = Color.Black;
                richTextBoxSqlViewer.SelectionFont = new Font("Georigia", 8f, FontStyle.Regular);
                richTextBoxSqlViewer.AppendText(txtForConstruct[2]);
            }
        }
        #endregion Event of User query part.

        #region Team member edit part.
        #region Team members Tab control.
        private void loadTeamMemberToGridView()
        {
            dataGridViewTeamMembers.DataSource = SettingInfo.TeamInfoDT;

            // Create the instant of edit to the Gridview infomation.
            TeamEditor = new TeamSourceEdit(SettingInfo.TeamInfoDT);
        }

        /// <summary>
        /// When changes made to team names. The new team name should be add to the Team popup.
        /// </summary>
        private void loadTeamNamesToSelection()
        {
            String[] allTeamNames = SettingInfo.TeamInfo.GetAllTeamNamesInOrder();

            if (allTeamNames == null)
                return;

            toolStripComboBoxAllTeams.Items.Clear();
            toolStripComboBoxAllTeams.Items.AddRange(allTeamNames);

            comboBoxTeamForLoad.Items.Clear();
            comboBoxTeamForLoad.Items.AddRange(allTeamNames);

            checkedListBoxSelectTeams.Items.Clear();
            foreach (String team in allTeamNames)
            {
                checkedListBoxSelectTeams.Items.Add(team, SettingInfo.TeamInfo.CheckIsEnabledForChartGroup(team));
            }
        }
        #endregion

        // Create, Delete, Save the changes to team members.
        private void toolStripButtonNewMember_Click(object sender, EventArgs e)
        {
            if (dataGridViewTeamMembers.Columns.Count == 0)
                return;

            if (dataGridViewTeamMembers.Rows.Count == 0)
                dataGridViewTeamMembers.CurrentCell = dataGridViewTeamMembers.Rows[0].Cells[0];
            else
            {
                dataGridViewTeamMembers.CurrentCell = dataGridViewTeamMembers.Rows[dataGridViewTeamMembers.Rows.Count - 1].Cells[0];
                dataGridViewTeamMembers.FirstDisplayedScrollingRowIndex = dataGridViewTeamMembers.Rows.Count - 1;
            }
        }

        private void toolStripButtonDelete_Click(object sender, EventArgs e)
        {
            int allRows = dataGridViewTeamMembers.SelectedRows.Count;

            if (allRows == 0)
            {
                return;
            }

            if (MessageBox.Show("Do you really want to delete seleted  :"
                + Convert.ToString(allRows) + "items?",
                "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
            {
                this.reportNews("Delete cancelled.", reportType.Info);
                return;
            }
            else
            {
                if (allRows != dataGridViewTeamMembers.Rows.Count)
                {
                    for (int i = allRows; i >= 1; i--)
                    {
                        try
                        {
                            dataGridViewTeamMembers.Rows.RemoveAt(dataGridViewTeamMembers.SelectedRows[i - 1].Index);
                        }
                        catch 
                        {
                            this.reportNews("Uncommitted rows detected.", reportType.Warn);
                            //Do nothig if user delete uncommit rows.
                        }
                    }
                }
                else
                    dataGridViewTeamMembers.Rows.Clear();
            }

            queueList saveList = null;
            EditAble.TeamEdit = true;
            saveList = this.toolStripButtonSaveChange_Click;
            saveList.Invoke(null, null);
        }

        private void toolStripButtonSaveChange_Click(object sender, EventArgs e)
        {
            try
            {
                if (EditAble.TeamEdit)
                {
                    //Save the changes that didn't been submit.
                    dataGridViewTeamMembers.EndEdit();

                    // Save the latest change in TeamChart
                    Dictionary<String, Dictionary<int, Boolean>> chartTeamSource = new Dictionary<string, Dictionary<int, bool>>();
                    int curIndex = 1;

                    foreach (object item in checkedListBoxSelectTeams.Items)
                    {
                        Dictionary<int, Boolean> eachTeam = new Dictionary<int, bool>();
                        eachTeam.Add(curIndex, checkedListBoxSelectTeams.GetItemChecked(curIndex - 1));
                        chartTeamSource.Add(item.ToString(), eachTeam);
                        curIndex++;
                    }

                    Boolean memberExistError = false;

                    List<DataGridViewRow> rowsForRemove = new List<DataGridViewRow>();
                    foreach (DataGridViewRow dr in dataGridViewTeamMembers.Rows)
                    {
                        if (dr.Cells[0].Value == null ||
                            dr.Cells[0].Value == null ||
                            dr.Cells[0].Value == null)
                        {
                            continue;
                        }

                        if (dr.Cells[0].Value.ToString().Trim() == "" ||
                            dr.Cells[2].Value.ToString().Trim() == "" ||
                            dr.Cells[3].Value.ToString().Trim() == "")
                        {
                            rowsForRemove.Add(dr);
                            memberExistError = true;
                        }
                    }

                    if (memberExistError)
                    {
                        if (MessageBox.Show("Current team members exsit error. If choose continue. Error lines will be removed automatically!", "Save confirm",
                            MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning) == System.Windows.Forms.DialogResult.Yes)
                        {
                            foreach (DataGridViewRow drEach in rowsForRemove)
                            {
                                if (!drEach.IsNewRow)
                                    dataGridViewTeamMembers.Rows.Remove(drEach);
                            }
                        }
                        else
                        {
                            this.reportNews("Save cancelled.", reportType.Warn);
                            return;
                        }
                    }

                    //Save team members add.
                    SettingInfo.TeamInfo = TeamEditor.SaveChange(
                        (DataTable)dataGridViewTeamMembers.DataSource,
                        chartTeamSource,
                        true,
                        SettingInfo.TeamInfoFileLocation);

                    // Refresh the team names in Combobox.
                    this.loadTeamNamesToSelection();

                    this.reportNews("Save record success.", reportType.Info);

                    EditAble.TeamEdit = false;
                }
            }
            catch
            {
                this.reportNews("Error encouterred while save files!", reportType.Error);
            }
        }

        private void toolStripComboBoxAllTeams_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataGridViewTeamMembers.DataSource = TeamEditor.SortTableThroughTeamName(toolStripComboBoxAllTeams.Text.Trim());
        }

        private void toolStripComboBoxAllTeams_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Back)
            {
                dataGridViewTeamMembers.DataSource = TeamEditor.SortTableThroughTeamName(toolStripComboBoxAllTeams.Text.Trim());
            }
        }

        /// <summary>
        ///  Add the default value of the new lines.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridViewTeamMembers_DefaultValuesNeeded(object sender, DataGridViewRowEventArgs e)
        {
            try
            {
                e.Row.Cells[4].Value = true;
                e.Row.Cells[2].Value = "Dev";

                if (toolStripComboBoxAllTeams.Text.Trim() != "")
                {
                    e.Row.Cells[3].Value = toolStripComboBoxAllTeams.Text.Trim();
                }
                else
                {
                    e.Row.Cells[3].Value = "New";
                }
            }
            catch { }
        }

        /// <summary>
        ///  Grid Movement control.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolStripButtonMoveClick(object sender, EventArgs e)
        {
            if (dataGridViewTeamMembers.Rows.Count == 0)
                return;

            if (dataGridViewTeamMembers.SelectedRows.Count > 0)
            {
                int currentIndex = dataGridViewTeamMembers.SelectedRows[0].Index;

                dataGridViewTeamMembers.ClearSelection();
                switch (((ToolStripButton)sender).Name)
                {
                    case "toolStripButtonFirst":
                        {
                            if (dataGridViewTeamMembers.Rows.Count > 0)
                            {
                                dataGridViewTeamMembers.Rows[0].Selected = true;
                                dataGridViewTeamMembers.FirstDisplayedScrollingRowIndex = 0;
                            }
                            break;
                        }
                    case "toolStripButtonBack":
                        {
                            if (currentIndex > 0)
                            {
                                dataGridViewTeamMembers.Rows[currentIndex - 1].Selected = true;
                                dataGridViewTeamMembers.FirstDisplayedScrollingRowIndex = currentIndex - 1;
                            }
                            break;
                        }
                    case "toolStripButtonMoveForward":
                        {
                            if (currentIndex < dataGridViewTeamMembers.Rows.Count - 1)
                            {
                                dataGridViewTeamMembers.Rows[currentIndex + 1].Selected = true;
                                dataGridViewTeamMembers.FirstDisplayedScrollingRowIndex = currentIndex + 1;
                            }
                            break;
                        }
                    case "toolStripButtonMoveLast":
                        {
                            if (dataGridViewTeamMembers.Rows.Count > 0)
                            {
                                dataGridViewTeamMembers.Rows[dataGridViewTeamMembers.Rows.Count - 1].Selected = true;
                                dataGridViewTeamMembers.FirstDisplayedScrollingRowIndex = dataGridViewTeamMembers.Rows.Count - 1;
                            }
                            break;
                        }
                }
            }
            else
            {
                dataGridViewTeamMembers.Rows[0].Selected = true;
            }
        }

        /// <summary>
        /// Determines if the change had been made to team members.
        /// If new changes make before last sync, system will save the changes automatically.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridViewTeamMembers_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            EditAble.TeamEdit = true;
        }
        #endregion Team member edit part.

        #region TFS fields customzie part.
        private void loadTFSFieldsForSelection()
        {
            var queryChosenColumns = from field in SettingInfo.TFSFieldsValue.Fields
                                     where field.Enabled == true
                                     orderby field.FieldNum ascending
                                     select field;

            foreach (TFSField result in queryChosenColumns)
                listBoxChosenColumns.Items.Add(result.FieldName);

            var queryAvaliableColumns = from field in SettingInfo.TFSFieldsValue.Fields
                                        where field.Enabled == false
                                        select field;

            foreach (TFSField result in queryAvaliableColumns)
                listBoxAvaliableColumns.Items.Add(result.FieldName);
        }

        /// <summary>
        /// Change Tfs fields to enabled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonAdd_Click(object sender, EventArgs e)
        {
            if (listBoxAvaliableColumns.SelectedItems.Count == 0)
                return;

            EditAble.FieldEdit = true;
            List<String> selectedColumns = new List<string>();
            foreach (object o in listBoxAvaliableColumns.SelectedItems)
            {
                selectedColumns.Add(o.ToString());
                listBoxChosenColumns.Items.Add(o);
            }
            foreach (object remove in selectedColumns)
            {
                listBoxAvaliableColumns.Items.Remove(remove);
            }
        }

        /// <summary>
        /// Change Tfs fields to disabled.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void buttonRemove_Click(object sender, EventArgs e)
        {
            if (listBoxChosenColumns.SelectedItems.Count == 0)
                return;

            EditAble.FieldEdit = true;
            List<String> selectedColumns = new List<string>();
            foreach (object o in listBoxChosenColumns.SelectedItems)
            {
                selectedColumns.Add(o.ToString());
                listBoxAvaliableColumns.Items.Add(o);
            }
            foreach (object remove in selectedColumns)
            {
                listBoxChosenColumns.Items.Remove(remove);
            }
        }

        /// <summary>
        /// Add default fields to chosen columns.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxUseDefaultFields_CheckedChanged(object sender, EventArgs e)
        {
            EditAble.FieldEdit = true;

            listBoxChosenColumns.Items.Clear();
            listBoxAvaliableColumns.Items.Clear();
            if (checkBoxUseDefaultFields.Checked)
            {
                List<String> suggestField = new List<string>()
                                { 
                                    "Id",
                                    "Issue",
                                    "State",
                                    "AssignedTo",
                                    "DevAssigned",
                                    "TestAssigned",
                                    "Title",
                                    "AcceptDate"
                                };
                
                foreach (TFSField field in SettingInfo.TFSFieldsValue.Fields)
                {
                    if (!suggestField.Contains(field.FieldName))
                    {
                        listBoxAvaliableColumns.Items.Add(field.FieldName);
                    }
                }

                foreach (String field in suggestField)
                {
                    listBoxChosenColumns.Items.Add(field);
                }
            }
            else
            {
                this.loadTFSFieldsForSelection();
            }
        }
        #endregion 

        #region Load and closed form.
        private void loadUserSetting()
        {
            checkBoxStartUpWithWindows.Checked = SettingInfo.AdammaSetting.ProSetting.StartWithWindows;
            checkBoxTFSFieldFrom.Checked = SettingInfo.AdammaSetting.ProSetting.UseCustomizedFieldWhenExcuteQuery;
            checkBoxPreLoadingSingalBugFields.Checked = SettingInfo.AdammaSetting.ProSetting.PreLoadingAllSingalBugsData;

            // if user not choose customize the info for grid view. Last team user selected will be remmbered as default loading team.
            checkBoxCustomizeGridView.Checked = SettingInfo.AdammaSetting.ProSetting.CustomizeGridViewLoading;
            radioButtonUseTeamForLoad.Checked = SettingInfo.AdammaSetting.ProSetting.LoadingFromDefaultTeam;
            radioButtonUseQueryForLoad.Checked = SettingInfo.AdammaSetting.ProSetting.LoadingFromQuery;
            comboBoxTeamForLoad.Text = SettingInfo.AdammaSetting.ProSetting.DefaultTeamNameForLoad;
            comboBoxQueryForLoad.Text = Path.GetFileName(SettingInfo.AdammaSetting.ProSetting.DefaultQueryForLoad);

            this.refreshDefaultLoadingForGridViewControls();
        }

        public delegate void queueList(object s, EventArgs e);
        /// <summary>
        /// Perform save procee to the items below,
        /// 1. Save Team members.
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SetUp_FormClosing(object sender, FormClosingEventArgs e)
        {
            // This active change will auto call the leave event for saving the changed items.
            tabControlUserQuery.SelectedIndex = 1;

            // Save the change made to team members
            queueList saveList = null;
            saveList = this.toolStripButtonSaveChange_Click;            
            saveList.Invoke(null,null);

            if (listBoxChosenColumns.Items.Count == 0)
            {
                MessageBox.Show("At least one fields need specific! Saving process cancelled.","Fatal error", MessageBoxButtons.OK,MessageBoxIcon.Error);
                return;
            }

            // Save the change made to TFS fields
            if (EditAble.FieldEdit)
            {
                List<String> selectedColumns = new List<string>();
                foreach (object o in listBoxChosenColumns.Items)
                {
                    selectedColumns.Add(o.ToString());
                }
                CommonUtilities.updateTFSFieldsAndSortNum(SettingInfo.TFSFieldsValue, selectedColumns).SaveTFSFields(SettingInfo.TFSFieldInfoLoc);
            }

            // Save the change made to team members
            if (EditAble.SettingEdit)
                SettingInfo.AdammaSetting.saveSetupInfo(SettingInfo.SettingFileLocation);
        }
        #endregion

        #region Common Forms event.
        #region Move the tfs fields as user required.
        private void buttonTop_Click(object sender, EventArgs e)
        {
            this.MoveItem(-listBoxChosenColumns.SelectedIndex);
        }

        private void buttonUp_Click(object sender, EventArgs e)
        {
            this.MoveItem(-1);
        }

        private void buttonDown_Click(object sender, EventArgs e)
        {
            this.MoveItem(1);
        }

        private void buttonBotton_Click(object sender, EventArgs e)
        {
            this.MoveItem(listBoxChosenColumns.Items.Count - listBoxChosenColumns.SelectedIndex - 1);
        }

        private void MoveItem(int _direction)
        {
            if (listBoxChosenColumns.SelectedItem == null || listBoxChosenColumns.SelectedIndex < 0)
                return;

            int newIndex = listBoxChosenColumns.SelectedIndex + _direction;

            if (newIndex < 0 || newIndex >= listBoxChosenColumns.Items.Count)
                return;

            EditAble.FieldEdit = true;
            object selected = listBoxChosenColumns.SelectedItem;

            listBoxChosenColumns.Items.Remove(selected);
            listBoxChosenColumns.Items.Insert(newIndex, selected);
            listBoxChosenColumns.SetSelected(newIndex, true);
        }
        #endregion

        #region Set/Get fields width.
        /// <summary>
        /// Get the width of field from Setting.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBoxColumns_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxChosenColumns.SelectedItem == null)
                return;

            String fieldName = listBoxChosenColumns.SelectedItem.ToString();

            var choosenField = from field in SettingInfo.TFSFieldsValue.Fields
                               where field.FieldName == fieldName
                               select field;
            if (choosenField != null)
                textBoxChoosenFieldWidth.Text = choosenField.First<TFSField>().FieldWidth.ToString();
        }

        /// <summary>
        /// Set the field width when user change it manually.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void textBoxChoosenFieldWidth_Leave(object sender, EventArgs e)
        {
            int newWidth;
            if (textBoxChoosenFieldWidth.Text.Trim() == ""
                || listBoxChosenColumns.SelectedItems.Count == 0
                || !Int32.TryParse(textBoxChoosenFieldWidth.Text, out newWidth))
                return;

            EditAble.FieldEdit = true;
            String fieldName = listBoxChosenColumns.SelectedItem.ToString();

            var choosenField = from field in SettingInfo.TFSFieldsValue.Fields
                               where field.FieldName == fieldName
                               select field;

            if (choosenField.First<TFSField>().FieldWidth != newWidth)
            {
                choosenField.First<TFSField>().FieldWidth = newWidth;
                EditAble.FieldEdit = true;
            }
        }
        #endregion Set/Get fields width.

        /// <summary>
        /// http://stackoverflow.com/questions/7427354/program-start-with-windows-c-sharp
        /// Add startup to all user. If no access, add startup to current user.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkBoxStartUpWithWindows_CheckedChanged(object sender, EventArgs e)
        {
            EditAble.SettingEdit = true;
            var path = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
            if (checkBoxStartUpWithWindows.Checked)
            {
                try
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(path, true);
                    key.SetValue("Addamma2", Application.ExecutablePath.ToString());
                    SettingInfo.AdammaSetting.ProSetting.StartWithWindows = true;
                }
                catch
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(path, true);
                    key.SetValue("Addamma2", Application.ExecutablePath.ToString());
                    SettingInfo.AdammaSetting.ProSetting.StartWithWindows = true;
                }
            }
            else
            {
                try
                {
                    RegistryKey key = Registry.LocalMachine.OpenSubKey(path, true);
                    key.DeleteValue("Addamma2", false);
                    SettingInfo.AdammaSetting.ProSetting.StartWithWindows = false;
                }
                catch
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(path, true);
                    key.DeleteValue("Addamma2", false);
                    SettingInfo.AdammaSetting.ProSetting.StartWithWindows = false;
                }
            }
        }

        private void checkBoxPreLoadingSingalBugFields_CheckedChanged(object sender, EventArgs e)
        {
            EditAble.SettingEdit = true;
            if (checkBoxPreLoadingSingalBugFields.Checked == true)
                SettingInfo.AdammaSetting.ProSetting.PreLoadingAllSingalBugsData = true;
            else
                SettingInfo.AdammaSetting.ProSetting.PreLoadingAllSingalBugsData = false;
        }

        private void radioButtonUseTeamForLoad_CheckedChanged(object sender, EventArgs e)
        {
            EditAble.SettingEdit = true;
            if (radioButtonUseTeamForLoad.Checked == true)
                SettingInfo.AdammaSetting.ProSetting.LoadingFromDefaultTeam = true;
            else
                SettingInfo.AdammaSetting.ProSetting.LoadingFromDefaultTeam = false;

            this.refreshDefaultLoadingForGridViewControls();
        }

        private void checkBoxCustomizeGridView_CheckedChanged(object sender, EventArgs e)
        {
            EditAble.SettingEdit = true;
            if (checkBoxCustomizeGridView.Checked == true)
                SettingInfo.AdammaSetting.ProSetting.CustomizeGridViewLoading = true;
            else
                SettingInfo.AdammaSetting.ProSetting.CustomizeGridViewLoading = false;

            this.refreshDefaultLoadingForGridViewControls();
        }

        private void checkBoxTFSFieldFrom_CheckedChanged(object sender, EventArgs e)
        {
            EditAble.SettingEdit = true;
            if (checkBoxTFSFieldFrom.Checked == true)
                SettingInfo.AdammaSetting.ProSetting.UseCustomizedFieldWhenExcuteQuery = true;
            else
                SettingInfo.AdammaSetting.ProSetting.UseCustomizedFieldWhenExcuteQuery = false;
        }

        private void radioButtonUseQueryForLoad_CheckedChanged(object sender, EventArgs e)
        {
            EditAble.SettingEdit = true;
            if (radioButtonUseQueryForLoad.Checked == true)
                SettingInfo.AdammaSetting.ProSetting.LoadingFromQuery = true;
            else
                SettingInfo.AdammaSetting.ProSetting.LoadingFromQuery = false;

            this.refreshDefaultLoadingForGridViewControls();
        }

        private void comboBoxTeamForLoad_SelectedIndexChanged(object sender, EventArgs e)
        {
            EditAble.SettingEdit = true;
            SettingInfo.AdammaSetting.ProSetting.DefaultTeamNameForLoad = comboBoxTeamForLoad.Text;
        }

        private void comboBoxQueryForLoad_SelectedIndexChanged(object sender, EventArgs e)
        {
            EditAble.SettingEdit = true;
            SettingInfo.AdammaSetting.ProSetting.DefaultQueryForLoad = this.relativeAddress2AbsolutelyAddress(comboBoxQueryForLoad.Text, false);
        }

        private void LoadTrackBarAndComments()
        {
            switch (SettingInfo.AdammaSetting.ProSetting.RefreshCycle /60)
            {
                case 5:
                    {
                        trackBarTimeRefresh.Value = 0;
                        labelRefreshComments.Text = "Check for changes to bugs every : 5 minutes";
                        break;
                    }
                case 10:
                    {
                        trackBarTimeRefresh.Value = 1;
                        labelRefreshComments.Text = "Check for changes to bugs every : 10 minutes";
                        break;
                    }
                case 20:
                    {
                        trackBarTimeRefresh.Value = 2;
                        labelRefreshComments.Text = "Check for changes to bugs every : 20 minutes";
                        break;
                    }
                case 30:
                    {
                        trackBarTimeRefresh.Value = 3;
                        labelRefreshComments.Text = "Check for changes to bugs every : 30 minutes";
                        break;
                    }
                case 60:
                    {
                        trackBarTimeRefresh.Value = 4;
                        labelRefreshComments.Text = "Check for changes to bugs every : 60 minutes";
                        break;
                    }
                case 120:
                    {
                        trackBarTimeRefresh.Value = 5;
                        labelRefreshComments.Text = "Check for changes to bugs every : 2 hours";
                        break;
                    }
                case 200:
                    {
                        trackBarTimeRefresh.Value = 6;
                        labelRefreshComments.Text = "Manual refresh only";
                        break;
                    }
                default:
                    {
                        trackBarTimeRefresh.Value = 6;
                        labelRefreshComments.Text = "Manual refresh only";
                        break;
                    }
            }
        }

        private void trackBarTimeRefresh_Scroll(object sender, EventArgs e)
        {
            EditAble.SettingEdit = true;
            switch (trackBarTimeRefresh.Value)
            {
                case 0:
                    {
                        SettingInfo.AdammaSetting.ProSetting.RefreshCycle = 5 * 60;
                        labelRefreshComments.Text = "Check for changes to bugs every : 5 minutes";
                        break;
                    }
                case 1:
                    {
                        SettingInfo.AdammaSetting.ProSetting.RefreshCycle = 10 * 60;
                        labelRefreshComments.Text = "Check for changes to bugs every : 10 minutes";
                        break;
                    }
                case 2:
                    {
                        SettingInfo.AdammaSetting.ProSetting.RefreshCycle = 20 * 60;
                        labelRefreshComments.Text = "Check for changes to bugs every : 20 minutes";
                        break;
                    }
                case 3:
                    {
                        SettingInfo.AdammaSetting.ProSetting.RefreshCycle = 30 * 60;
                        labelRefreshComments.Text = "Check for changes to bugs every : 30 minutes";
                        break;
                    }
                case 4:
                    {
                        SettingInfo.AdammaSetting.ProSetting.RefreshCycle = 60 * 60;
                        labelRefreshComments.Text = "Check for changes to bugs every : 60 minutes";
                        break;
                    }
                case 5:
                    {
                        SettingInfo.AdammaSetting.ProSetting.RefreshCycle = 120 * 60;
                        labelRefreshComments.Text = "Check for changes to bugs every : 2 hours";
                        break;
                    }
                case 6:
                    {
                        SettingInfo.AdammaSetting.ProSetting.RefreshCycle = 0;
                        labelRefreshComments.Text = "Manual refresh only";
                        break;
                    }
            }
        }

        private void LoadChartGroupBarType()
        {
            switch (SettingInfo.AdammaSetting.ProSetting.BarType)
            {
                case "Cluster": radioButtonCluster.Checked = true; break;
                case "ClusterHiLow": radioButtonClusterHiLow.Checked = true; break;
                case "Overlay": radioButtonOverlay.Checked = true; break;
                case "PercentStack": radioButtonPercentStack.Checked = true; break;
                case "SortedOverlay": radioButtonSortedOverlay.Checked = true; break;
                case "Stack": radioButtonStack.Checked = true; break;
                default: radioButtonCluster.Checked = true; break;
            }
        }

        private void radioButtonBarType_CheckedChanged(object sender, EventArgs e)
        {
            if (sender.GetType() != typeof(RadioButton))
                return;

            EditAble.SettingEdit = true;

            RadioButton senderObject = (RadioButton)sender;

            switch (senderObject.Name)
            {
                case "radioButtonCluster":
                    {
                        SettingInfo.AdammaSetting.ProSetting.BarType = BarType.Cluster.ToString();
                        textBoxCommentsForBarType.Text = "This is the normal format in which various bar series are grouped together in clusters at each base value.";
                        break;
                    }
                case "radioButtonClusterHiLow":
                    {
                        SettingInfo.AdammaSetting.ProSetting.BarType = BarType.ClusterHiLow.ToString();
                        textBoxCommentsForBarType.Text = "This format draws a hi-low (bars have a top and bottom that are user defined) in a cluster format, so multiple high-low bars can be grouped together at each base value.";
                        break;
                    }
                case "radioButtonOverlay":
                    {
                        SettingInfo.AdammaSetting.ProSetting.BarType = BarType.Overlay.ToString();
                        textBoxCommentsForBarType.Text = "In this format, the bars are drawn on top of each other, with the first BarItem drawn at the back, and the last BarItem drawn at the front.";
                        break;
                    }
                case "radioButtonSortedOverlay":
                    {
                        SettingInfo.AdammaSetting.ProSetting.BarType = BarType.SortedOverlay.ToString();
                        textBoxCommentsForBarType.Text = "This is similar to Overlay, but the bars are sorted on value, and the highest value is drawn at the back, and the lowest value is drawn at the front.";
                        break;
                    }
                case "radioButtonStack":
                    {
                        SettingInfo.AdammaSetting.ProSetting.BarType = BarType.Stack.ToString();
                        textBoxCommentsForBarType.Text = "The bars are stacked on top of each other, accumulating in value.";
                        break;
                    }
                case "radioButtonPercentStack":
                    {
                        SettingInfo.AdammaSetting.ProSetting.BarType = BarType.PercentStack.ToString();
                        textBoxCommentsForBarType.Text = "The bars are stacked on top of each other, and plotted as a percentile, with the total height always being 100%.";
                        break;
                    }
                default:
                    {
                        SettingInfo.AdammaSetting.ProSetting.BarType = BarType.Cluster.ToString();
                        textBoxCommentsForBarType.Text = "The bars are stacked on top of each other, accumulating in value.";
                        break;
                    }
            }
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (checkedListBoxSelectTeams.Items.Count == 0 || sender.GetType() != typeof(ToolStripMenuItem))
                return;

            Boolean selectAll = true;

            if (((ToolStripMenuItem)sender).Name == "unselectAllToolStripMenuItem")
                selectAll = false;

            for (int index = 0; index < checkedListBoxSelectTeams.Items.Count; index++)
            {
                checkedListBoxSelectTeams.SetItemChecked(index, selectAll);
            }
        }

        private void toolStripButtonMoveChartGroupTeam_Click(object sender, EventArgs e)
        {
            if (checkedListBoxSelectTeams.Items.Count == 0 || 
                checkedListBoxSelectTeams.Items.Count == 1 ||
                checkedListBoxSelectTeams.SelectedItems.Count == 0 ||
                sender.GetType() != typeof(ToolStripButton))
                return;

            Boolean moveDown = true;
            if (((ToolStripButton)sender).Name == "toolStripButtonMoveChartGroupTeamUp")
            {
                moveDown = false;
            }
                        
            if ((moveDown && checkedListBoxSelectTeams.SelectedIndex == checkedListBoxSelectTeams.Items.Count) ||
                (!moveDown && checkedListBoxSelectTeams.SelectedIndex == 0))
                return;

            EditAble.TeamEdit = true;

            String tmpTeamname;
            Boolean tempEnable;
            int curSelectIndex = checkedListBoxSelectTeams.SelectedIndex;

            tmpTeamname = checkedListBoxSelectTeams.SelectedItem.ToString();
            tempEnable = checkedListBoxSelectTeams.GetItemChecked(curSelectIndex);

            // Remove current selection
            checkedListBoxSelectTeams.Items.RemoveAt(curSelectIndex);

            // Add the current selection to right place.
            int newIndex = curSelectIndex + (moveDown ? 1 : -1);
            checkedListBoxSelectTeams.Items.Insert(newIndex, tmpTeamname);
            checkedListBoxSelectTeams.SetItemChecked(newIndex, tempEnable);

            checkedListBoxSelectTeams.SetSelected(newIndex, true);
        }

        private void checkedListBoxSelectTeams_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (checkedListBoxSelectTeams.Items.Count == 0)
                return;

            // Save the changes if user change the ability of teams for chart.
            EditAble.TeamEdit = true;
        }
        #endregion

        #region Functionalities
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Close();
            }
            if (keyData == (Keys.Control | Keys.S))
            {
                queueList saveList = null;
                saveList = this.toolStripButtonSaveChange_Click;
                saveList.Invoke(null, null);
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }
        
        #region Setup report news part.
        enum reportType
        {
            Info,
            Warn,
            Error
        }
        private void reportNews(String _reportString, reportType infoType = reportType.Info)
        {
            toolStripStatusLabelSetupStatus.Text = _reportString;

            switch (infoType)
            {
                case reportType.Info: toolStripStatusLabelSetupStatus.ForeColor = Color.Black; break;
                case reportType.Warn: toolStripStatusLabelSetupStatus.ForeColor = Color.Yellow; break;
                case reportType.Error: toolStripStatusLabelSetupStatus.ForeColor = Color.Red; break;
            }
        }
        #endregion                 

        /// <summary>
        /// Control the access to each setup button.
        /// </summary>
        private void refreshDefaultLoadingForGridViewControls()
        {
            if (SettingInfo.AdammaSetting.ProSetting.CustomizeGridViewLoading)
            {
                radioButtonUseTeamForLoad.Enabled = true;
                comboBoxTeamForLoad.Enabled = true;
                radioButtonUseQueryForLoad.Enabled = true;
                comboBoxQueryForLoad.Enabled = true;

                if (SettingInfo.AdammaSetting.ProSetting.LoadingFromDefaultTeam)
                {
                    radioButtonUseTeamForLoad.Checked = true;
                    comboBoxTeamForLoad.Enabled = true;
                    comboBoxQueryForLoad.Enabled = false;
                }
                else
                {
                    radioButtonUseQueryForLoad.Checked = true;
                    comboBoxQueryForLoad.Enabled = true;
                    comboBoxTeamForLoad.Enabled = false;
                }
            }
            else
            {
                radioButtonUseTeamForLoad.Enabled = false;
                comboBoxTeamForLoad.Enabled = false;
                radioButtonUseQueryForLoad.Enabled = false;
                comboBoxQueryForLoad.Enabled = false;
            }
        }


        /// <summary>
        /// Change the address from tree view to the real file path.
        /// </summary>
        /// <param name="_relativeAddress"></param>
        /// <param name="_fromTreeView">True if the path is from Tree view. Need remove the former string</param>
        /// <returns>The real path of the query file</returns>
        private String relativeAddress2AbsolutelyAddress(String _relativeAddress, Boolean _fromTreeView)
        {
            String absoluteAddress = SettingInfo.QueryLocation;

            if (_fromTreeView)
                _relativeAddress = _relativeAddress.Remove(0, _relativeAddress.LastIndexOf('\\') + 1);

            if (GlobalValue.absoluteToRelatedQueryFileAddressMap.ContainsKey(_relativeAddress))
                return GlobalValue.absoluteToRelatedQueryFileAddressMap[_relativeAddress];
            else
            {
                GlobalValue.absoluteToRelatedQueryFileAddressMap.Add(_relativeAddress, absoluteAddress + "\\" + _relativeAddress);
                return absoluteAddress + "\\" + _relativeAddress;
            }
        }

        /// <summary>
        /// refresh query root folder and treeview.
        /// using when,
        /// 1. Loading setup form.
        /// 2. refresh the real location.
        /// </summary>
        /// <param name="_queryRootCustomizedFolder"></param>
        private void refreshQueryRootfolderAndTreeView(String _queryRootCustomizedFolder = "")
        {
            try
            {
                if (_queryRootCustomizedFolder != "")
                {
                    SettingInfo.QueryLocation = _queryRootCustomizedFolder;
                }

                textBoxQueryLoc.Text = SettingInfo.QueryLocation;

                // refresh the query files belows to the folder
                treeViewQuery.Nodes.Clear();
                treeViewQuery.ImageList = imageListTreeNode;

                comboBoxQueryForLoad.Items.Clear();
                GlobalValue.absoluteToRelatedQueryFileAddressMap.Clear();

                var stack = new Stack<TreeNode>();
                var rootDirectory = new DirectoryInfo(SettingInfo.QueryLocation);
                var node = new TreeNode(rootDirectory.Name) { Tag = rootDirectory };
                node.ImageIndex = 0;
                stack.Push(node);

                while (stack.Count > 0)
                {
                    var currentNode = stack.Pop();
                    var directoryInfo = (DirectoryInfo)currentNode.Tag;

                    // Try if the access to folder denied 
                    try
                    {
                        foreach (var directory in directoryInfo.GetDirectories())
                        {
                            var childDirectoryNode = new TreeNode(directory.Name) { Tag = directory };
                            currentNode.Nodes.Add(childDirectoryNode);
                            childDirectoryNode.ImageIndex = 0;
                            stack.Push(childDirectoryNode);
                        }
                    }
                    catch (Exception ex)
                    {
                        this.reportNews(ex.Message, reportType.Warn);
                    }

                    // Try if the access to file denied 
                    try
                    {
                        foreach (var file in directoryInfo.GetFiles())
                        {
                            if (Path.GetExtension(file.ToString()) == ".wiq")
                            {
                                TreeNode tnSql = new TreeNode(file.Name);
                                currentNode.Nodes.Add(tnSql);
                                tnSql.ImageIndex = 1;

                                // Same query file name will be ignored.
                                if (!GlobalValue.absoluteToRelatedQueryFileAddressMap.ContainsKey(file.Name))
                                {
                                    GlobalValue.absoluteToRelatedQueryFileAddressMap.Add(file.Name, file.FullName);
                                    comboBoxQueryForLoad.Items.Add(file.Name);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        this.reportNews(ex.Message, reportType.Warn);
                    }
                }

                treeViewQuery.Nodes.Add(node);
                treeViewQuery.ExpandAll();
            }
            catch (Exception ex)
            {
                this.reportNews(ex.Message, reportType.Error);
            }
        }

        /// <summary>
        /// Used for instant for form loading.
        /// </summary>
        private void loadQueryRootfolderAndTreeView()
        {
            this.refreshQueryRootfolderAndTreeView();
        }
        #endregion Functionalities

        private void dataGridViewTeamMembers_CurrentCellChanged(object sender, EventArgs e)
        {
            if (dataGridViewTeamMembers.IsCurrentCellDirty)
            {
                dataGridViewTeamMembers.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }
    }
}
