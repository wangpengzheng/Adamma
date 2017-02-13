using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Data;
using ZedGraph;
using System.Diagnostics;

namespace Adamma
{
    /// <summary>
    /// Use to pick color with Random
    /// </summary>
    public class ColorPicker
    {
        private int colorCurPosition;
        private int knownColorTotalNum;
        private List<String> knownColorList;

        public ColorPicker()
        {
            colorCurPosition = 1;
            knownColorList = this.GetColors();
            knownColorTotalNum = knownColorList.Count();
        }

        public KnownColor PickNewColor()
        {
            colorCurPosition += 5;
            return (KnownColor)Enum.Parse(typeof(KnownColor), knownColorList[colorCurPosition % knownColorTotalNum]);
            //return (KnownColor)Enum.Parse(typeof(KnownColor),knownColorList[colorCurPosition ++ % knownColorTotalNum]);
        }

        private List<string> GetColors()
        {
            //create a generic list of strings
            List<string> colors = new List<string>();
            //get the color names from the Known color enum
            string[] colorNames = Enum.GetNames(typeof(KnownColor));
            //iterate thru each string in the colorNames array
            foreach (string colorName in colorNames)
            {
                //cast the colorName into a KnownColor
                KnownColor knownColor = (KnownColor)Enum.Parse(typeof(KnownColor), colorName);
                //check if the knownColor variable is a System color
                if (knownColor > KnownColor.Transparent)
                {
                    //add it to our list
                    colors.Add(colorName);
                }
            }
            //return the color list
            return colors;
        }
    }

    /// <summary>
    /// http://www.codeproject.com/Articles/5431/A-flexible-charting-library-for-NET
    /// </summary>
    public enum ChartType
    {
        BarChart,
        CurveChart,
        DevPieChart,
        TestPieChart,
        StackBarChart
    }

    public class pieSouceData
    {
        public string Name { get; set; }
        public double Count { get; set; }
    }

    public class barSourceData
    {
        public List<String> TeamName;

        public List<double> BreakSLABugsNumberCount;
        public List<double> LessThanTenBugsNumberCount;
        public List<double> LessThanTwentyBugsNumberCount;
        public List<double> LessThanSixtyBugsNumberCount;
        public List<double> NoSLABugsNumberCount;
        public List<double> ClosedOrResolvedBugsNumberCount;

        public barSourceData(DataTable _sourceDataTable)
        {
            TeamName = new List<string>();

            BreakSLABugsNumberCount = new List<double>();
            LessThanTenBugsNumberCount = new List<double>();
            LessThanTwentyBugsNumberCount = new List<double>();
            LessThanSixtyBugsNumberCount = new List<double>();
            NoSLABugsNumberCount = new List<double>();
            ClosedOrResolvedBugsNumberCount = new List<double>();

            foreach (DataRow dr in _sourceDataTable.Rows)
            {
                TeamName.Add(dr["TeamName"].ToString());

                BreakSLABugsNumberCount.Add(double.Parse(dr["BreakSLA"].ToString()));
                LessThanTenBugsNumberCount.Add(double.Parse(dr["LessThanTen"].ToString()));
                LessThanTwentyBugsNumberCount.Add(double.Parse(dr["LessThanTwenty"].ToString()));
                LessThanSixtyBugsNumberCount.Add(double.Parse(dr["LessThanSixty"].ToString()));
                NoSLABugsNumberCount.Add(double.Parse(dr["NoSLABugNum"].ToString()));
                ClosedOrResolvedBugsNumberCount.Add(double.Parse(dr["ClosedOrResolvedBugNum"].ToString()));
            }
        }
    }

    /// <summary>
    /// http://stackoverflow.com/questions/6370028/return-list-using-select-new-in-linq?rq=1
    /// </summary>
    public class ChartDataController
    {
        public List<pieSouceData> convertDataForPieChart(DataTable _sourceDT, Boolean _forDev = true)
        {
            if (_sourceDT == null || _sourceDT.Columns.Count == 0 || _sourceDT.Rows.Count == 0)
                return null;

            String keyColumnName;
            if (_forDev)
            {
                keyColumnName = "Dev Assigned";
            }
            else
            {
                keyColumnName = "Test Assigned";
            }

            if (!_sourceDT.Columns.Contains(keyColumnName))
            {
                Debug.Assert(false, "Columns count : " + _sourceDT.Columns.Count.ToString() + " Rows Count:" + _sourceDT.Rows.Count.ToString());
                return null;
            } 

            var groupedData = from b in _sourceDT.AsEnumerable()
                              group b by b.Field<string>(keyColumnName) into g
                              select new pieSouceData()
                              {
                                  Name = g.Key,
                                  Count = g.Count()
                              };

            return groupedData.ToList();
        }
    }

    /// <summary>
    /// Definision of a class of ZedGraphController.
    /// </summary>
    public class ZedGraphController
    {
        DataTable groupSouceDT;

        public ZedGraphController(DataTable _groupSouceDT)
        {
            if (_groupSouceDT == null)
                groupSouceDT = _groupSouceDT;
            else
                groupSouceDT = _groupSouceDT.Copy();
        }

        #region functionality
        public static void FreshChartGroupBartype(ZedGraphControl _control, BarType _barType)
        {
            GraphPane myPane = _control.GraphPane;

            myPane.BarSettings.Type = _barType; 
        }
        #endregion


        public Boolean CreateGraph(ZedGraphControl _control, ChartType _chartType, BarType _barType = BarType.Cluster)
        {
            _control.GraphPane.CurveList.Clear();
            switch (_chartType)
            {
                case ChartType.BarChart: return this.CreateGraph_Bar(_control, _barType);
                case ChartType.CurveChart: return this.CreateGraph_Curve(_control);
                case ChartType.DevPieChart: return this.CreateGraph_Pie(_control, true);
                case ChartType.TestPieChart: return this.CreateGraph_Pie(_control, false);
                case ChartType.StackBarChart: return this.CreateGraph_StackBar(_control);
                default: return false;
            }
        }

        private Boolean CreateGraph_Bar(ZedGraphControl _control, BarType _barType)
        {
            barSourceData barData = new barSourceData(this.groupSouceDT);

            // get a reference to the GraphPane
            GraphPane myPane = _control.GraphPane;

            // Set the Titles
            myPane.Title.Text = "Teams Group";
            myPane.XAxis.Title.Text = "Choosen Teams Count : " + groupSouceDT.Rows.Count.ToString();
            myPane.YAxis.Title.Text = "Bug numbers";

            myPane.BarSettings.Type = _barType;

            BarItem myBar;

            myBar = myPane.AddBar("BreakSLA", null, barData.BreakSLABugsNumberCount.ToArray<double>(), Color.Black);
            myBar = myPane.AddBar("LessThanTen", null, barData.LessThanTenBugsNumberCount.ToArray<double>(), Color.Red);
            myBar = myPane.AddBar("LessThanSixty", null, barData.LessThanTwentyBugsNumberCount.ToArray<double>(), Color.Yellow);
            myBar = myPane.AddBar("LessThanTwenty", null, barData.LessThanSixtyBugsNumberCount.ToArray<double>(), Color.Green);
            myBar = myPane.AddBar("NoSLABugNum", null, barData.NoSLABugsNumberCount.ToArray<double>(), Color.DarkViolet);
            myBar = myPane.AddBar("ClosedOrResolvedBugNum", null, barData.ClosedOrResolvedBugsNumberCount.ToArray<double>(), Color.Gainsboro);

            //// Set the XAxis labels
            myPane.XAxis.Scale.TextLabels = barData.TeamName.ToArray<String>();
            //// Set the XAxis to Text type
            myPane.XAxis.Type = AxisType.Text;

            // Fill the Axis and Pane backgrounds
            myPane.Chart.Fill = new Fill(Color.White,
                  Color.FromArgb(255, 255, 166), 90F);
            myPane.Fill = new Fill(Color.FromArgb(250, 250, 255));

            // Tell ZedGraph to refigure the
            // axes since the data have changed
            _control.AxisChange();

            return true;
        }

        private Boolean CreateGraph_Curve(ZedGraphControl _control)
        {
            return true;
        }

        private Boolean CreateGraph_Pie(ZedGraphControl _control, Boolean _forDev)
        {
            ChartDataController pieController = new ChartDataController();

            // Convert the data from Datatable to a List of Pie source style needs.
            List<pieSouceData> targetDataForPie = pieController.convertDataForPieChart(groupSouceDT, _forDev);

            if (targetDataForPie == null)
                return false;

            #region Create graph
            GraphPane myPane = _control.GraphPane;
            ColorPicker cp = new ColorPicker();

            // Set the Titles
            if (_forDev)
            {
                myPane.Title.Text = "My Dev Bar Graph";
            }
            else
            {
                myPane.Title.Text = "My Test Bar Graph";
            }

            // Add data to pie slice
            foreach (pieSouceData source in targetDataForPie)
            {
                PieItem pieSliece = myPane.AddPieSlice(source.Count, Color.FromName(cp.PickNewColor().ToString()), 0F, source.Name);
                pieSliece.LabelType = PieLabelType.Name_Value_Percent;
            }

            // optional depending on whether you want labels within the graph legend
            myPane.Legend.IsVisible = true;
            myPane.Legend.Position = LegendPos.Top;

            _control.AxisChange();
            _control.Invalidate();
            #endregion

            return true;
        }

        private Boolean CreateGraph_StackBar(ZedGraphControl _control)
        {
            return true;
        }
    }
}
