using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.Data;
using Excel = Microsoft.Office.Interop.Excel;
using System.Diagnostics;
using System.Windows.Forms;

namespace Adamma
{
    /// <summary>
    /// Export the Data in current DataGridview to formats "Excel","Xml","Txt"
    /// </summary>
    public class FileExportController
    {
        public static Boolean ExportToXmlFile(DataTable _dTtableForExport)
        {
            if (_dTtableForExport.Rows.Count > 0)
            {
                string Startuppath = Application.StartupPath + "/";
                string Destinationpath = Startuppath + "" + DateTime.Now.ToString("dd-MMM-yyy") + ".xml"; //File Extension as your requirement .dat or .txt 

                if (File.Exists(Destinationpath))
                    File.Delete(Destinationpath);

                using (XmlTextWriter myWriter = new XmlTextWriter(Destinationpath, Encoding.UTF8))
                {
                    myWriter.Formatting = Formatting.Indented;
                    myWriter.WriteStartElement("ExportedFromAdamma");
                    myWriter.Flush();
                }

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(Destinationpath);
                XmlNode root = xmlDoc.SelectSingleNode("ExportedFromAdamma");

                int columnsCount;
                foreach (DataRow dr in _dTtableForExport.Rows)
                {
                    columnsCount = 0;

                    XmlElement xe = xmlDoc.CreateElement("ExportedRow");
                    xe.SetAttribute("RowNumber", dr[columnsCount].ToString());

                    foreach (DataColumn dc in _dTtableForExport.Columns)
                    {
                        if (columnsCount++ == 0)
                            continue;
                        String columnNameWithOutSpace = dc.ColumnName.Replace(" ", String.Empty);
                        XmlElement xinside = xmlDoc.CreateElement(columnNameWithOutSpace);
                        xinside.InnerText = dr[dc.ColumnName].ToString();
                        xe.AppendChild(xinside);
                    }
                    root.AppendChild(xe);
                }
                xmlDoc.Save(Destinationpath);

                // Open the exported file.
                System.Diagnostics.Process.Start(Destinationpath);
                return true;
            }

            return false;
        }


        public static Boolean ExportToTextFile(DataTable _dTtableForExport)
        {
            if (_dTtableForExport.Rows.Count > 0)
            {
                String RowcCount = "";
                string Startuppath = Application.StartupPath + "/";
                string Destinationpath = Startuppath + "" + DateTime.Now.ToString("dd-MMM-yyy") + ".txt"; //File Extension as your requirement .dat or .txt 

                if (File.Exists(Destinationpath))
                    File.Delete(Destinationpath);

                using (StreamWriter Streamwrite = File.CreateText(Destinationpath))
                {
                    for (int i = 0; i < _dTtableForExport.Rows.Count; i++)
                    {
                        RowcCount = "";
                        for (int j = 0; j < _dTtableForExport.Columns.Count; j++)
                        {
                            if (RowcCount.Length > 0)
                            {
                                RowcCount = RowcCount + "|" + _dTtableForExport.Rows[i][j].ToString();
                            }
                            else
                            {
                                RowcCount = _dTtableForExport.Rows[i][j].ToString();
                            }
                        }
                        Streamwrite.WriteLine(RowcCount);
                    }
                    Streamwrite.WriteLine(Streamwrite.NewLine);
                    Streamwrite.Close();

                    // Open the exported file.
                    System.Diagnostics.Process.Start(Destinationpath);
                    return true;
                }
            }

            return false;
        }

        public static void ExportToExcelFile(DataTable _dTtableForExport)
        {
            int colIndex = 1;
            int rowIndex = 1;
            Excel.Application xlApp;
            Excel.Workbook xlWorkBook;
            Excel.Worksheet xlWorkSheet;
            object misValue = System.Reflection.Missing.Value;
            xlApp = new Excel.Application();
            Excel.Range ExelRange;

            xlApp = new Excel.Application();
            xlWorkBook = xlApp.Workbooks.Add(misValue);
            xlWorkSheet = (Excel.Worksheet)xlWorkBook.Worksheets.get_Item(1);


            foreach (DataRow theRow in _dTtableForExport.Rows)
            {
                rowIndex = rowIndex + 1;
                colIndex = 0;
                foreach (DataColumn dc in _dTtableForExport.Columns)
                {
                    colIndex = colIndex + 1;
                    xlWorkSheet.Cells[rowIndex + 1, colIndex] = theRow[dc.ColumnName];
                    xlWorkSheet.Rows.AutoFit();
                    xlWorkSheet.Columns.AutoFit();
                }
            }

            xlWorkSheet.get_Range("b2", "e2").Merge(false);

            ExelRange = xlWorkSheet.get_Range("b2", "e2");
            ExelRange.FormulaR1C1 = "Exported lines from Adamma";

            ExelRange.HorizontalAlignment = 3;
            ExelRange.VerticalAlignment = 3;

            xlApp.Visible = true;
            FileExportController.ObjectRelease(xlWorkSheet);
            FileExportController.ObjectRelease(xlWorkBook);
            FileExportController.ObjectRelease(xlApp);

        }
        public static void ObjectRelease(object objRealease)
        {
            try
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(objRealease);
                objRealease = null;
            }
            catch (Exception ex)
            {
                objRealease = null;
                Debug.Assert(false, "Error exist while export to excel. " + ex.Message);
            }
            finally
            {
                GC.Collect();
            }
        }
    }

    /// <summary>
    /// Through the fields "Accept Date", "Priority", "VendDaysCredit" to calculate each bug's SLA.
    /// 
    /// The Calculated SLA bug status count will be following,
    /// 1. breakSLA
    /// 2. lessThanTen
    /// 3. lessThanTwenty
    /// 4. noSLABugNum
    /// 5. closedOrResolvedBugNum
    /// </summary>
    public class SLAController
    {
        #region The number of bugs of each status.
        public int BreakSLA
        { get; set; }

        public int LessThanTen
        { get; set; }

        public int LessThanTwenty
        { get; set; }

        public int LessThanSixty
        { get; set; }

        public int NoSLABugNum
        { get; set; }

        public int ClosedOrResolvedBugNum
        { get; set; }
        #endregion

        /// <summary>
        /// Calculate sla from Datatable which comes from one Team Or Query. Calculated SLA date will be added to the first column.
        /// </summary>
        /// <param name="_dtOrignal"></param>
        /// <param name="_forGridView">If true Columns "number" and "left Days" will be add to the finanlly grid view."</param>
        /// <returns></returns>
        public DataTable CalcSLAToDatatable(DataTable _dtOrignal, Boolean _forGridView = true)
        {
            if (_dtOrignal == null) return null;

            System.Diagnostics.Debug.Assert(
                _dtOrignal.Columns.Contains("Accept Date") &&
                _dtOrignal.Columns.Contains("Priority") &&
                _dtOrignal.Columns.Contains("Vendor Days Credit"));

            // Add the Count Column and SLA column to datatable.
            if (_forGridView)
            {
                _dtOrignal.Columns.Add("Left Days", typeof(Int64)).SetOrdinal(0);
                _dtOrignal.Columns.Add("Number", typeof(Int64)).SetOrdinal(0);
            }

            Int64 leftDays;
            Boolean wrongDataBug;
            int numberCount = 1;
            foreach (DataRow drEach in _dtOrignal.Rows)
            {
                wrongDataBug = false;

                // Get the Accept date,
                DateTime dt = (DateTime)(drEach["Accept Date"].ToString() == "" ? DateTime.MinValue : drEach["Accept Date"]);

                // Get the priority, is don't exist. Assign 5 represent error data.
                int priority;
                if (!int.TryParse(drEach["Priority"].ToString(), out priority))
                {
                    priority = 5;
                }

                // Get Credit date for a bug if exist.
                int creditDate = (drEach["Vendor Days Credit"].ToString() == "") ? 0 : int.Parse(drEach["Vendor Days Credit"].ToString());

                // Make as wrong data if there is no priority or AcceptDate
                if (priority == 5 || dt == DateTime.MinValue)
                    wrongDataBug = true;

                // Get the status of each bug.
                String bugState = drEach["State"].ToString();

                if (bugState == "Closed" || bugState == "Resolved")
                {
                    leftDays = this.getSLAForEachBug(dt, priority, creditDate, true);
                }
                else
                {
                    leftDays = this.getSLAForEachBug(dt, priority, creditDate, false);
                }

                // Add the left days to this row.
                if (!wrongDataBug && _forGridView)
                    drEach["Left Days"] = leftDays;

                if (_forGridView)
                    drEach["Number"] = numberCount++;
            }

            if (_forGridView)
            {
                // Get the DBnull value first.
                var resultOfNull = from myRow in _dtOrignal.AsEnumerable()
                                   where myRow["Left Days"] == DBNull.Value
                                   select myRow;

                // Sort the datatable with the order of column "Left Days"
                var resultAfterSort = from myRow in _dtOrignal.AsEnumerable()
                                      where myRow["Left Days"] != DBNull.Value
                                      orderby myRow.Field<Int64>("Left Days") ascending
                                      select myRow;

                DataTable newAfterSort = _dtOrignal.Clone();
                // Add the null value.
                foreach (DataRow dr in resultOfNull)
                {
                    newAfterSort.ImportRow(dr);
                }

                // Add the sorted value.
                foreach (DataRow dr in resultAfterSort)
                {
                    newAfterSort.ImportRow(dr);
                }
                _dtOrignal = newAfterSort;
            }

            return _dtOrignal;
        }

        public Int64 getSLAForEachBug(DateTime _dtForAcceptDate, int _priority, int _creditDate, Boolean _closedOrResolved = false)
        {
            int totalDate;
            Int64 leftDate;
            Int64 usedDays;

            // If this bug is resolved or closed, go ahead calculate it's SLA date.
            if (_closedOrResolved)
                ClosedOrResolvedBugNum += 1;

            // If this bug is without sla (e.g. No accept date, not priority, return directly with the min value.
            int[] legalPriority = { 1, 2, 3, 4 };
            if ((_dtForAcceptDate == DateTime.MinValue || _dtForAcceptDate == null || !legalPriority.Contains(_priority)) &&
                !_closedOrResolved)
            {
                NoSLABugNum += 1;
                return Int64.MinValue;
            }

            DateTime today = DateTime.Today;
            System.TimeSpan Ts = today - _dtForAcceptDate;
            usedDays = Ts.Days - _creditDate;

            switch (_priority.ToString())
            {
                case "1":
                    totalDate = 7;
                    break;
                case "2":
                    totalDate = 14;
                    break;
                case "3":
                    totalDate = 30;
                    break;
                case "4":
                    totalDate = 60;
                    break;
                default:
                    totalDate = int.MinValue;
                    break;
            }

            if (_closedOrResolved)
            {
                return totalDate - usedDays;
            }
            else if (totalDate == int.MinValue)
            {
                NoSLABugNum += 1;
                return Int64.MinValue;
            }
            else
            {
                leftDate = totalDate - usedDays;

                // Add the total value for team Chart.
                if (leftDate > 0 && leftDate <= 10)
                    LessThanTen += 1;
                else if (leftDate > 10 && leftDate <= 20)
                    LessThanTwenty += 1;
                else if (leftDate > 20 && leftDate <= 60)
                    LessThanSixty += 1;
                else
                    BreakSLA += 1;

                return leftDate;
            }
        }

        public DataTable getCurTeamBugsStatus()
        {
            DataTable dtForGroupChart = new DataTable();
            dtForGroupChart.Columns.Add("Break SLA");
            dtForGroupChart.Columns.Add("<=10d");
            dtForGroupChart.Columns.Add("<=20d");
            dtForGroupChart.Columns.Add("<=60d");
            dtForGroupChart.Columns.Add("NoSLA");
            dtForGroupChart.Columns.Add("ClosedOrResolved");

            DataRow dr = dtForGroupChart.NewRow();
            dr[0] = BreakSLA;
            dr[1] = LessThanTen;
            dr[2] = LessThanTwenty;
            dr[3] = LessThanSixty;
            dr[4] = NoSLABugNum;
            dr[5] = ClosedOrResolvedBugNum;

            dtForGroupChart.Rows.Add(dr);

            return dtForGroupChart;
        }
    }

    /// <summary>
    /// When new datatable assigned to lookupController. The avaliable field will update.
    /// When the field was choosen, the expression and lookup String need updated accordingly.
    /// 
    /// User can use the fields "AvaliableFields", "CurExpression", "TargetSearchString" to access the latest data of the filter.
    /// </summary>
    public class LookUpController
    {
        /// <summary>
        /// When CurfieldName updated, expression and lookup String need updated accordingly.
        /// </summary>
        private String curFieldName;
        public string CurFieldName
        {
            get { return curFieldName; }
            set
            {
                curFieldName = value;

                // Update the expression to this field.
                this.updateFieldExpression();
                this.refreshLookUpInfo();
            }
        }

        #region Define the field,Expression, and targetString Suggest for the filter.
        private List<String> curExpression;
        public List<String> CurExpression
        {
            get { return curExpression; }
        }

        private List<String> targetSearchString;
        public List<String> TargetSearchString
        {
            get { return targetSearchString; }
        }

        private List<String> avaliableFields;
        public List<String> AvaliableFields
        {
            get { return avaliableFields; }
            set { avaliableFields = value; }
        }
        #endregion

        private DataTable gridViewBugsDT;
        public DataTable GridViewBugsDT
        {
            get { return gridViewBugsDT; }
            set
            {
                gridViewBugsDT = value;

                // Refresh the avaliable fields
                this.updateAvaliableFields();
            }
        }

        public LookUpController()
        {
            gridViewBugsDT = new DataTable();
            curFieldName = "";

            avaliableFields = new List<string>();
            curExpression = new List<string>();
            targetSearchString = new List<string>();
        }

        private void updateAvaliableFields()
        {
            avaliableFields.Clear();
            if (gridViewBugsDT == null)
                return;

            foreach (DataColumn dc in gridViewBugsDT.Columns)
            {
                avaliableFields.Add(dc.ColumnName);
            }
        }

        private void updateFieldExpression()
        {
            if (gridViewBugsDT == null)
            {
                Debug.Assert(false, "Source for Lookup can not be null.");
                return;
            }

            curExpression.Clear();
            if (curFieldName == "")
                return;

            if (gridViewBugsDT.Columns[curFieldName].DataType == typeof(String))
            {
                curExpression.Add("Contains");
            }

            curExpression.Add("Equals");

            if (gridViewBugsDT.Columns[curFieldName].DataType == typeof(int) ||
                gridViewBugsDT.Columns[curFieldName].DataType == typeof(Int64) ||
                gridViewBugsDT.Columns[curFieldName].DataType == typeof(DateTime))
            {
                curExpression.Add(">=");
                curExpression.Add("<=");
            }
        }

        private void refreshLookUpInfo()
        {
            targetSearchString.Clear();
            // To reduce the performence loss.
            if (gridViewBugsDT.Rows.Count > 200)
                return;

            foreach (DataRow dr in gridViewBugsDT.Rows)
            {
                if (!targetSearchString.Contains(dr[curFieldName].ToString()))
                {
                    targetSearchString.Add(dr[curFieldName].ToString());
                }
            }
        }
    }

    /// <summary>
    /// Query the DataTable through the exist datatable column and provided expression & Value.
    /// </summary>
    public class QuickQueryController
    {
        // This part of query should be totally matched the code at: LookUpController.updateFieldExpression();
        public static DataTable ExecuteQuery(DataTable _dtForSource, String _fieldForQuery, String _queryExpression, String _value)
        {
            DataTable emptySource = _dtForSource.Clone();

            if (_dtForSource.Columns[_fieldForQuery].DataType == typeof(string))
            {
                // To string fields type, supports contains equals query.
                if (_queryExpression == "Contains")
                {
                    var results = from myRow in _dtForSource.AsEnumerable()
                                  where myRow[_fieldForQuery] != DBNull.Value
                                  && myRow.Field<String>(_fieldForQuery).Contains(_value)
                                  select myRow;

                    if (results.Count() != 0)
                    {
                        return results.CopyToDataTable();
                    }
                    else
                        return emptySource;
                }
                else
                {
                    var results = from myRow in _dtForSource.AsEnumerable()
                                  where myRow[_fieldForQuery] != DBNull.Value
                                  && myRow.Field<String>(_fieldForQuery) == _value
                                  select myRow;
                    
                    if (results.Count() != 0)
                    {
                        return results.CopyToDataTable();
                    }
                    else
                        return emptySource;
                }
            }
            else if (_dtForSource.Columns[_fieldForQuery].DataType == typeof(int))
            {
                int test;
                if (!int.TryParse(_value, out test))
                    return emptySource;

                switch (_queryExpression)
                {
                    case "Equals":
                        {
                            // To int or DataTime type, supports equals,belows to , upper to query.
                            var results = from myRow in _dtForSource.AsEnumerable()
                                          where myRow[_fieldForQuery] != DBNull.Value
                                          && myRow.Field<int>(_fieldForQuery) == int.Parse(_value)
                                          select myRow;
                            if (results.Count() != 0)
                                return results.CopyToDataTable();
                            else
                                return emptySource;
                        }
                    case ">=":
                        {
                            // To int or DataTime type, supports equals,belows to , upper to query.
                            var results = from myRow in _dtForSource.AsEnumerable()
                                          where myRow[_fieldForQuery] != DBNull.Value
                                          && myRow.Field<int>(_fieldForQuery) >= int.Parse(_value)
                                          select myRow;
                            if (results.Count() != 0)
                                return results.CopyToDataTable();
                            else
                                return emptySource;
                        }
                    case "<=":
                        {
                            // To int or DataTime type, supports equals,belows to , upper to query.
                            var results = from myRow in _dtForSource.AsEnumerable()
                                          where myRow[_fieldForQuery] != DBNull.Value
                                          && myRow.Field<int>(_fieldForQuery) <= int.Parse(_value)
                                          select myRow;
                            if (results.Count() != 0)
                                return results.CopyToDataTable();
                            else
                                return emptySource;
                        }
                    default: return emptySource;
                }
            }
            else if (_dtForSource.Columns[_fieldForQuery].DataType == typeof(Int64))
            {
                Int64 test;
                if (!Int64.TryParse(_value, out test))
                    return emptySource;

                switch (_queryExpression)
                {
                    case "Equals":
                        {
                            // To int or DataTime type, supports equals,belows to , upper to query.
                            var results = from myRow in _dtForSource.AsEnumerable()
                                          where myRow[_fieldForQuery] != DBNull.Value
                                          && myRow.Field<Int64>(_fieldForQuery) == Int64.Parse(_value)
                                          select myRow;
                            if (results.Count() != 0)
                                return results.CopyToDataTable();
                            else
                                return emptySource;
                        }
                    case ">=":
                        {
                            // To int or DataTime type, supports equals,belows to , upper to query.
                            var results = from myRow in _dtForSource.AsEnumerable()
                                          where myRow[_fieldForQuery] != DBNull.Value
                                          && myRow.Field<Int64>(_fieldForQuery) >= Int64.Parse(_value)
                                          select myRow;
                            if (results.Count() != 0)
                                return results.CopyToDataTable();
                            else
                                return emptySource;
                        }
                    case "<=":
                        {
                            // To int or DataTime type, supports equals,belows to , upper to query.
                            var results = from myRow in _dtForSource.AsEnumerable()
                                          where myRow[_fieldForQuery] != DBNull.Value
                                          && myRow.Field<Int64>(_fieldForQuery) <= Int64.Parse(_value)
                                          select myRow;
                            if (results.Count() != 0)
                                return results.CopyToDataTable();
                            else
                                return emptySource;
                        }
                    default: return emptySource;
                }
            }
            else if (_dtForSource.Columns[_fieldForQuery].DataType == typeof(DateTime))
            {
                DateTime test;
                if (!DateTime.TryParse(_value, out test))
                    return emptySource;

                switch (_queryExpression)
                {
                    case "Equals":
                        {
                            // To int or DataTime type, supports equals,belows to , upper to query.
                            var results = from myRow in _dtForSource.AsEnumerable()
                                          where myRow[_fieldForQuery] != DBNull.Value
                                          && myRow[_fieldForQuery].ToString() == _value
                                          select myRow;
                            if (results.Count() != 0)
                                return results.CopyToDataTable();
                            else
                                return emptySource;
                        }
                    case ">=":
                        {
                            // To int or DataTime type, supports equals,belows to , upper to query.
                            var results = from myRow in _dtForSource.AsEnumerable()
                                          where myRow[_fieldForQuery] != DBNull.Value
                                          && myRow.Field<DateTime>(_fieldForQuery) >= DateTime.Parse(_value)
                                          select myRow;
                            if (results.Count() != 0)
                                return results.CopyToDataTable();
                            else
                                return emptySource;
                        }
                    case "<=":
                        {
                            // To int or DataTime type, supports equals,belows to , upper to query.
                            var results = from myRow in _dtForSource.AsEnumerable()
                                          where myRow[_fieldForQuery] != DBNull.Value
                                          && myRow.Field<DateTime>(_fieldForQuery) <= DateTime.Parse(_value)
                                          select myRow;
                            if (results.Count() != 0)
                                return results.CopyToDataTable();
                            else
                                return emptySource;
                        }
                    default: return emptySource;
                }
            }
            else
            {
                Debug.Assert(false, "Type of " + _dtForSource.Columns[_fieldForQuery].DataType.ToString() + " Unimplementated.");
                // Do nothing if encountered unkown fileds.
                return emptySource;
            }
        }
    }

    /// <summary>
    ///  Use to calc Chart group team bugs.
    /// </summary>
    public class TeamChartGroupController
    {
        private DataTable dtForFinallyChart = null;
        public DataTable DtForFinallyChart
        {
            get { return dtForFinallyChart; }
        }

        private SLAController slaCalc = null;
        
        public TeamChartGroupController()
        {
            dtForFinallyChart = new DataTable();
            dtForFinallyChart.Columns.Add("TeamName");
            dtForFinallyChart.Columns.Add("BreakSLA");
            dtForFinallyChart.Columns.Add("LessThanTen");
            dtForFinallyChart.Columns.Add("LessThanTwenty");
            dtForFinallyChart.Columns.Add("LessThanSixty");            
            dtForFinallyChart.Columns.Add("NoSLABugNum");
            dtForFinallyChart.Columns.Add("ClosedOrResolvedBugNum");
        }

        /// <summary>
        /// And and calc each teams SLA to datatable with one row.
        /// </summary>
        /// <param name="_teamName"></param>
        /// <param name="_dtForTeamSLA"></param>
        public void AddTeamNamesWithCorrespondentData(String _teamName, DataTable _dtForTeamSLA)
        {
            slaCalc = new SLAController();

            slaCalc.CalcSLAToDatatable(_dtForTeamSLA, false);

            DataRow dr = dtForFinallyChart.NewRow();
            dr[0] = _teamName;
            dr[1] = slaCalc.BreakSLA;
            dr[2] = slaCalc.LessThanTen;
            dr[3] = slaCalc.LessThanTwenty;
            dr[4] = slaCalc.LessThanSixty;
            dr[5] = slaCalc.NoSLABugNum;
            dr[6] = slaCalc.ClosedOrResolvedBugNum;

            dtForFinallyChart.Rows.Add(dr);
        }
    }
}
