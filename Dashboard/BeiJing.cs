using SuperMap.Analyst.NetworkAnalyst;
using SuperMap.Analyst.SpatialAnalyst;
using SuperMap.Data;
using SuperMap.Mapping;
using SuperMap.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dashboard
{
    public class BeiJing : City
    {
        public BeiJing(MapControl mapControl, ComboBox comboBox, DataGridView dataGridView, TrackBar trackBar)
        {
            this.name = "北京";
            this.Center = new Point2D(116.40, 39.90);
            this.comboBox = comboBox;
            this.dataGridView = dataGridView;
            this.mapControl = mapControl;
            this.SetDatasource();
            this.pointSelector = new PointSelector(mapControl, dataGridView);
            this.barrySelector = new BarrySelector(trackBar, mapControl, dataGridView);
            this.routeAnalyster = new RouteAnalyster(mapControl, dataGridView);
            this.SetQueryer();
            SetBarrySelector();
            //异步加载环境网络模型
            Task.Run(() =>
            {
                MessageBox.Show("请稍等片刻");
                SetRouteAnalyster();
                bool IsFinished = false;
                IsFinished = routeAnalyster.m_analyst.Load();
                if (IsFinished)
                {
                    MessageBox.Show("北京市网络模型加载成功");
                }
            });
        }

        internal override void SetBarrySelector()
        {
            barrySelector.targetDataset = (DatasetVector)this.datasource.Datasets[0];
            barrySelector.resultDataset = (DatasetVector)this.datasource.Datasets[2];
            barrySelector.overlayParameter = new OverlayAnalystParameter();
            barrySelector.overlayParameter.SourceRetainedFields = new string[1] { "name" };
            barrySelector.overlayParameter.Tolerance = 0.000000000000000000000000000000001;
            m_datasetLine = (DatasetVector)datasource.Datasets[3];
            m_datasetPoint = m_datasetLine.ChildDataset;
            barrySelector.points = pointSelector.points;
            barrySelector.m_layerLine = mapControl.Map.Layers.Add(m_datasetLine, true);
            barrySelector.m_layerLine.IsSelectable = false;
            barrySelector.m_layerLine.IsVisible = true;
            LayerSettingVector lineSetting = (LayerSettingVector)barrySelector.m_layerLine.AdditionalSetting;
            GeoStyle lineStyle = new GeoStyle();
            lineStyle.LineColor = Color.White;
            lineStyle.LineWidth = 0.00000000000000000000000000000000000000000000000000000000000000000000000000000000001;
            lineSetting.Style = lineStyle;
            barrySelector.m_layerPoint = mapControl.Map.Layers.Add(m_datasetPoint, true);
            barrySelector.m_layerPoint.IsVisible = true;
            barrySelector.m_layerPoint.IsSelectable = false;
            LayerSettingVector pointSetting = (LayerSettingVector)barrySelector.m_layerPoint.AdditionalSetting;
            GeoStyle pointStyle = new GeoStyle();
            pointStyle.LineColor = Color.White;
            pointStyle.MarkerSize = new Size2D(0.000005, 0.000005);
            pointSetting.Style = pointStyle;
        }

        internal override void SetDatasource()
        {
            this.datasource = mapControl.Map.Workspace.Datasources[2];
        }

        internal override void SetQueryer()
        {
            this.queryer = new Queryer(mapControl, comboBox);
            queryer.datasetVector = (DatasetVector)datasource.Datasets[1];
            QueryParameter queryParameter = new QueryParameter();
            queryParameter.ResultFields = new string[1] { "name" };
            queryParameter.OrderBy = new string[1] { "name" };
            queryParameter.CursorType = CursorType.Static;
            queryParameter.HasGeometry = true;//后续才能基于Geometry进行记录定位d录定位d
            queryer.queryParameter = queryParameter;
            queryer.comboBox.DisplayMember = "Name";
        }

        internal override void SetRouteAnalyster()
        {
            TransportationAnalystSetting setting = new TransportationAnalystSetting();
            setting.EdgeIDField = "SmEdgeID";//网络数据集中的弧段ID字段名
            setting.NodeIDField = "SmNodeID";//网络数据集中的结点ID字段名
            setting.EdgeNameField = "name"; //弧段名称字段
            WeightFieldInfos weightFieldInfos = new WeightFieldInfos();
            WeightFieldInfo weightFieldInfo = new WeightFieldInfo();
            weightFieldInfo.FTWeightField = "SmLength";//设置正向阻力字段名，从iDesktop下观察网络数据集-RoadNet的属性表可见，固定字段名。
            weightFieldInfo.TFWeightField = "SmLength";//设置反向阻力字段名，从iDesktop下观察网络数据集-RoadNet的属性表可见，固定字段名。
            weightFieldInfo.Name = "length";//设置权值字段名称
            weightFieldInfos.Add(weightFieldInfo);//用于在权值字段信息集合中加入一个权重元素
            setting.WeightFieldInfos = weightFieldInfos;//记录要经过某一弧段需要多少权值字段信息（WeightFieldInfos），来源于弧段属性表
            setting.FNodeIDField = "SmFNode";//弧段起结点ID字段名，从iDesktop下观察网络数据集-RoadNet的属性表可见，固定字段名。
            setting.TNodeIDField = "SmTNode";//弧段终结点ID字段名，从iDesktop下观察网络数据集-RoadNet的属性表可见，固定字段名。
            //m_datasetLine = (DatasetVector)datasource.Datasets[0];
            //m_datasetPoint = m_datasetLine.ChildDataset;
            setting.NetworkDataset = m_datasetLine;
            routeAnalyster.weightName = "length";
            routeAnalyster.setting = setting;
            routeAnalyster.m_analyst = new TransportationAnalyst();//构造交通网络分析对象
            routeAnalyster.m_analyst.AnalystSetting = setting;//设置环境设置对象
            //Task.Run( () =>
            //{
            //    bool IsFinished = false;
            //    IsFinished = routeAnalyster.m_analyst.Load();
            //    if (IsFinished)
            //    {
            //        MessageBox.Show("网络模型加载成功");
            //    }
            //});
            //return IsFinished;
        }
    }
}
