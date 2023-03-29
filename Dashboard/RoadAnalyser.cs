using Newtonsoft.Json;
using SuperMap.Analyst.NetworkAnalyst;
using SuperMap.Data;
using SuperMap.Mapping;
using SuperMap.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Dashboard
{
    public class RouteAnalyster
    {
        private MapControl m_mapControl;
        //private Workspace m_workspace;
        public DatasetVector m_datasetLine;
        public DatasetVector m_datasetPoint;
        private TrackingLayer m_trackingLayer;
        public Point2Ds m_Points;
        private GeoStyle m_style;
        public List<Int32> m_barrierNodes;
        public List<Int32> m_barrierEdges;
        public TransportationAnalyst m_analyst;
        private TransportationAnalystResult m_result;
        private DataGridView dataGridView;
        private Timer m_timer;
        private int m_count;
        public string weightName;
        //private int m_flag;
        public TransportationAnalystSetting setting;
        /// <summary>
        /// 选择模式枚举。
        /// Select mode enum
        /// </summary>
        public enum SelectMode
        {
            SelectPoint, //设置途经点
            SelectBarrier, //设置障碍
            None
        }


        /// <summary>
        /// 根据workspace、mapControl及DataGridView构造SampleRun对象。
        /// Initialize the SampleRun object with the specified workspace, mapControl, and dataGridView.
        /// </summary>
        public RouteAnalyster(MapControl mapControl,DataGridView dataGridView)
        {
            m_mapControl = mapControl;
            this.dataGridView = dataGridView;
        }

        /// <summary>
        /// 打开网络数据集并初始化相应变量。
        /// </summary>
        public void Initialize()
        {
            try
            {
                dataGridView.AutoResizeColumns();
                dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                m_trackingLayer = m_mapControl.Map.TrackingLayer;
                m_trackingLayer.IsAntialias = true;
                //m_flag = 1;
                //m_Points = new Point2Ds();
                m_style = new GeoStyle();
                m_timer = new Timer();
                m_timer.Interval = 200;
                m_timer.Enabled = false;
                m_mapControl.Action = SuperMap.UI.Action.Select;
                m_mapControl.Map.IsAntialias = true;
                m_mapControl.IsWaitCursorEnabled = false;
                m_mapControl.Map.Refresh();
                m_timer.Tick += SimulateCarMoving;
                dataGridView.Rows.Clear();
                dataGridView.Columns.Clear();
                dataGridView.Columns.Add("序号", "序号");
                dataGridView.Columns.Add("导引", "导引");
                dataGridView.Columns.Add("耗费", "耗费");
                dataGridView.Columns.Add("距离", "距离");
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
        }




        /// <summary>
        /// 进行最短路径分析。
        /// Optimal path analysis
        /// </summary>
        /// <returns></returns>
        public bool Analyst()
        {
            try
            {
                m_count = 0;
                var parameter = new TransportationAnalystParameter();
                // 设置障碍点及障碍边
                int[] barrierEdges = new int[m_barrierEdges.Count];
                for (int i = 0; i < barrierEdges.Length; i++)
                {
                    barrierEdges[i] = m_barrierEdges[i];
                }
                parameter.BarrierEdges = barrierEdges;
                int[] barrierNodes = new int[m_barrierNodes.Count];
                for (int i = 0; i < barrierNodes.Length; i++)
                {
                    barrierNodes[i] = m_barrierNodes[i];
                }
                parameter.BarrierNodes = barrierNodes;
                //设置权值字段
                parameter.WeightName = this.weightName;
                //设置途经点SHAPE_Leng
                parameter.Points = m_Points;
                // 设置返回内容
                parameter.IsNodesReturn = true;
                parameter.IsEdgesReturn = true;
                parameter.IsPathGuidesReturn = true;
                parameter.IsRoutesReturn = true;
                // 进行分析并显示结果
                m_result = m_analyst.FindPath(parameter, false);
                if (m_result == null)
                {
                    if (SuperMap.Data.Environment.CurrentCulture != "zh-CN")
                    {
                        MessageBox.Show("Failed");
                    }
                    else
                    {
                        MessageBox.Show("分析失败");
                    }
                    return false;
                }
              
                ShowResult();
                FillDataGridView(0);
                //m_selectMode = SelectMode.None;
                return true;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                return false;
            }
        }

        /// <summary>
        /// 显示结果。
        /// Show result
        /// </summary>
        public void ShowResult()
        {
            try
            {
                // 删除原有结果
                int count = m_trackingLayer.Count;
                for (int i = 0; i < count; i++)
                {
                    int index = m_trackingLayer.IndexOf("result");
                    if (index != -1)
                    {
                        m_trackingLayer.Remove(index);
                    }
                }

                FillDataGridView(0);//此处0，追踪到源码可知，
                GeoLineM geoLineM = m_result.Routes[0]; //最佳路径只有一条结果路由，即只有一条结果路径。
                m_style.LineColor = Color.Blue;
                m_style.LineWidth = 1;
                geoLineM.Style = m_style;
                m_trackingLayer.Add(geoLineM, "result");
                m_mapControl.Map.RefreshTrackingLayer();
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// 填充DataGridView。
        /// Fill DataGridView
        /// </summary>
        /// <param name="pathNum"></param>
        public void FillDataGridView(Int32 pathNum)
        {
            try
            {
                // 清除原数据，添加初始点信息
                dataGridView.Rows.Clear();
                Object[] objs = new Object[4];
                objs[0] = dataGridView.RowCount;
                if (SuperMap.Data.Environment.CurrentCulture != "zh-CN")
                {
                    objs[1] = "Start";
                }
                else
                {
                    objs[1] = "从起始点出发";
                }
                objs[2] = "--";
                objs[3] = "--";
                dataGridView.Rows.Add(objs);

                //PathGuides——各结果路由（路径）的引导对象集合。
                //每个引导对象PathGuide，展开对应一条具体结果路由（路径）的详细引导。
                PathGuide[] pathGuides = m_result.PathGuides;
                //最佳路径只有一条结果路由，即只有一条结果路径。
                //故，此处参数pathNum取值固定为0。 
                PathGuide pathGuide = pathGuides[pathNum];
                if (pathGuide.Count == 2)
                {

                }
                for (int j = 1; j < pathGuide.Count; j++)
                {
                    PathGuideItem item = pathGuide[j];
                    objs[0] = dataGridView.RowCount;

                    if (SuperMap.Data.Environment.CurrentCulture != "zh-CN")
                    {
                        // 导引子项为站点的添加方式
                        if (item.IsStop)
                        {
                            String side = "None";
                            if (item.SideType == SideType.Left)
                                side = "Left";
                            if (item.SideType == SideType.Right)
                                side = "Right";
                            if (item.SideType == SideType.Middle)
                                side = "On the road";
                            String dis = item.Distance.ToString();
                            if (item.Index == -1 && item.ID == -1)
                            {
                                continue;
                            }
                            if (j != pathGuide.Count - 1)
                            {
                                objs[1] = "Arrive at [" + item.Index + " route], on the " + side
                                        + dis;
                            }
                            else
                            {
                                objs[1] = "Arrive at destination " + side + dis;
                            }
                            objs[2] = "";
                            objs[3] = "";
                            dataGridView.Rows.Add(objs);
                        }
                        // 导引子项为弧段的添加方式
                        if (item.IsEdge)
                        {
                            String direct = "Go ahead";
                            if (item.DirectionType == DirectionType.East)
                                direct = "East";
                            if (item.DirectionType == DirectionType.West)
                                direct = "West";
                            if (item.DirectionType == DirectionType.South)
                                direct = "South";
                            if (item.DirectionType == DirectionType.North)
                                direct = "North";

                            String weight = item.Weight.ToString();
                            String roadName = item.Name;
                            if (weight.Equals("0") && roadName.Equals(""))
                            {
                                objs[1] = "Go " + direct + " " + item.Length;
                                objs[2] = weight;
                                objs[3] = item.Length;
                                dataGridView.Rows.Add(objs);
                            }
                            else
                            {
                                String roadString = roadName.Equals("") ? "Anonymous road" : roadName;
                                objs[1] = "Go along with [" + roadString + "], " + direct + " direction"
                                        + item.Length;
                                objs[2] = weight;
                                objs[3] = item.Length;
                                dataGridView.Rows.Add(objs);
                            }
                        }
                    }
                    else
                    {
                        // 导引子项为站点的添加方式
                        if (item.IsStop)
                        {
                            String side = "无";
                            if (item.SideType == SideType.Left)
                                side = "左侧";
                            if (item.SideType == SideType.Right)
                                side = "右侧";
                            if (item.SideType == SideType.Middle)
                                side = "道路上";
                            String dis = item.Distance.ToString();
                            if (item.Index == -1 && item.ID == -1)
                            {
                                continue;
                            }
                            if (j != pathGuide.Count - 1)
                            {
                                objs[1] = "到达[" + item.Index + "号路由点],在道路" + side
                                        + dis;
                            }
                            else
                            {
                                objs[1] = "到达终点,在道路" + side + dis;
                            }
                            objs[2] = "";
                            objs[3] = "";
                            dataGridView.Rows.Add(objs);
                        }
                        // 导引子项为弧段的添加方式
                        if (item.IsEdge)
                        {
                            String direct = "直行";
                            if (item.DirectionType == DirectionType.East)
                                direct = "东";
                            if (item.DirectionType == DirectionType.West)
                                direct = "西";
                            if (item.DirectionType == DirectionType.South)
                                direct = "南";
                            if (item.DirectionType == DirectionType.North)
                                direct = "北";

                            String weight = item.Weight.ToString();
                            String roadName = item.Name;
                            if (weight.Equals("0") && roadName.Equals(""))
                            {
                                objs[1] = "朝" + direct + "行走" + item.Length;
                                objs[2] = weight;
                                objs[3] = item.Length;
                                dataGridView.Rows.Add(objs);
                            }
                            else
                            {
                                String roadString = roadName.Equals("") ? "匿名路段" : roadName;
                                objs[1] = "沿着[" + roadString + "],朝" + direct + "行走"
                                        + item.Length;
                                objs[2] = weight;
                                objs[3] = item.Length;
                                dataGridView.Rows.Add(objs);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
            }
        }

        /// <summary>
        /// 开始导引。
        /// Start guiding
        /// </summary>
        public void Play()
        {
            m_timer.Start();
        }

        /// <summary>
        /// 停止导引。
        /// Stop guiding
        /// </summary>
        public void Stop()
        {
            m_timer.Stop();
        }

        /// <summary>
        /// 清除分析结果。
        /// Clear the analysis result
        /// </summary>
        public void Clear()
        {
            try
            {
                if (m_timer != null)
                    m_timer.Stop();

                dataGridView.Rows.Clear();
                dataGridView.Columns.Clear();
                //m_flag = 1;
                m_Points.Clear();
                m_barrierNodes.Clear();
                m_barrierEdges .Clear();
                m_mapControl.Map.Layers[0].Selection.Clear();
                m_mapControl.Map.Layers[1].Selection.Clear();
                m_mapControl.Map.TrackingLayer.Clear();
                m_mapControl.Map.Refresh();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// 进行行驶导引。
        /// Path guide
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SimulateCarMoving(object sender, EventArgs e)
        {
            try
            {
                int index = m_trackingLayer.IndexOf("playPoint");
                if (index != -1)
                {
                    m_trackingLayer.Remove(index);//模拟运动就是不断对红点擦除和重绘的过程。
                }
                if (m_result.Routes[0] == null)
                {
                    MessageBox.Show("请先规划路径");
                }
                GeoLineM lineM = m_result.Routes[0]; //最佳路径只有一条结果路由，因此也就只有一条结果路径（GeoLineM ）

                PointM pointM = lineM.GetPart(0)[m_count];//lineM.GetPart(0)获得路径上所有节点集合，通过timer.Tick事件，变化m_count值，变化点位。
                GeoPoint point = new GeoPoint(pointM.X, pointM.Y); // 构造模拟对象
                GeoStyle style = new GeoStyle();
                style.LineColor = Color.Red;
                style.MarkerSize = new Size2D(5, 5);
                point.Style = style;
                m_trackingLayer.Add(point, "playPoint");
                m_count++;// m_count值在变化，模拟运动。
                if (m_count >= lineM.GetPart(0).Count)
                {
                    m_count = 0; //走完，就重来。
                }
                m_mapControl.Map.RefreshTrackingLayer();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }

        }

    }
}
