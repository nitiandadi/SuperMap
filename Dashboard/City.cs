using SuperMap.Data;
using SuperMap.Mapping;
using SuperMap.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using TrackBar = System.Windows.Forms.TrackBar;

namespace Dashboard
{
     public abstract class City
    {
        internal Datasource datasource;
        internal MapControl mapControl;
        internal TrackBar trackBar;
        internal System.Windows.Forms.ComboBox comboBox;
        internal DataGridView dataGridView;
        internal DatasetVector datasetVector;
        internal QueryParameter queryParameter;
        internal Queryer queryer;
        internal PointSelector pointSelector;
        internal BarrySelector barrySelector;
        public RouteAnalyster routeAnalyster;
        public DatasetVector m_datasetLine;
        public DatasetVector m_datasetPoint;
        public Layer m_layerLine;
        public Layer m_layerPoint;
        public string name;
        public Point2D Center;
        internal abstract void SetDatasource();
        internal abstract void SetQueryer();
        internal abstract void SetRouteAnalyster();
        internal abstract void SetBarrySelector();
        public void ShowGeometry(int row_index)
        {
            DataGridViewRow row = this.dataGridView.Rows[row_index];
            Geometry geometry = (Geometry)row.Tag;//从row记录取出geometry
            if (geometry==null)
            {
                return;
            }
            this.mapControl.Map.EnsureVisible(geometry);//居中显示要素

            //geometry.Style = new GeoStyle() { FillForeColor = Color.Yellow };
            this.mapControl.Map.TrackingLayer.Add(geometry, "选中记录");//将geometry符号化后，加入临时绘画层显示
            this.mapControl.Map.Refresh();//刷新地图
        }
        internal void SelectPointMode()
        {
            //SetQueryer();
            this.pointSelector.isEnabled = true;
            this.barrySelector.isEnabled = false;
            this.queryer.selectPointer = this.pointSelector;
            mapControl.Action = SuperMap.UI.Action.Select;
        }
        internal void SelectBarryMode()
        {
            this.pointSelector.isEnabled = false;
            this.barrySelector.isEnabled = true;
            barrySelector.m_layerLine.IsSelectable = true;
            barrySelector.m_layerPoint.IsSelectable = true;
            mapControl.Action = SuperMap.UI.Action.Select2;
            if (pointSelector.points == null )
            {
                MessageBox.Show("请完成选点操作");
                return;
            }
            //this.SetBarrySelector();
            mapControl.Action = SuperMap.UI.Action.Select2;
        }
        internal void SelectAnalyserMode()
        {
            pointSelector.isEnabled = false;
            barrySelector.isEnabled = false;
            routeAnalyster.m_barrierEdges=barrySelector.barrierEdges;
            routeAnalyster.m_barrierNodes= barrySelector.barrierNodes;
            routeAnalyster.m_Points = pointSelector.points;
            routeAnalyster.Initialize();
            if (pointSelector.points==null|| barrySelector.barrierEdges==null)
            {
                MessageBox.Show("请完成选点和障碍操作");
                return;
            }
            //SetRouteAnalyster();
            mapControl.Action = SuperMap.UI.Action.Select2;
        }
    }
}
