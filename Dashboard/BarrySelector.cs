using Newtonsoft.Json;
using SuperMap.Analyst.SpatialAnalyst;
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
    internal class BarrySelector
    {
       private TrackBar trackBar1;
       private MapControl mapControl;
       private DataGridView dataGridView;
       internal PointSelector pointSelector;
       internal Point2Ds points;
       internal DatasetVector targetDataset;
       internal DatasetVector resultDataset;
       internal OverlayAnalystParameter overlayParameter;
       //internal QueryParameter queryParameter;
       internal Layer m_layerPoint;
       internal Layer m_layerLine;
       internal bool isEnabled;
       internal List<int> barrierNodes;
       internal List<int> barrierEdges;
       internal List<int> busyRoadsId;
        GeoStyle style;
        public BarrySelector(TrackBar trackBar,MapControl mapControl,DataGridView dataGridView)
      {
            this.trackBar1 = trackBar;
            this.dataGridView = dataGridView;
            this.mapControl = mapControl;
            this.trackBar1.Scroll += new System.EventHandler(this.trackBar1_Scroll);
            this.mapControl.GeometrySelected += MapControl_GeometrySelected;
            style = new GeoStyle();
            style.LineColor = Color.Red;
            style.LineWidth = 0.8;
            style.LineSymbolID = 15;
            barrierEdges = new List<int>();
            barrierNodes=new List<int>();
            busyRoadsId=new List<int>();
        }

        private void MapControl_GeometrySelected(object sender, GeometrySelectedEventArgs e)
        {
            if (this.isEnabled==true)
            {
                AddBarrier();
            }
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            //Point2Ds points = pointSelector.m_Points;
            GeoPoint geoPoint = new GeoPoint();
            BufferAnalystParameter bufferAnalystParam;
            bufferAnalystParam = new BufferAnalystParameter();
            bufferAnalystParam.EndType = BufferEndType.Round;
            bufferAnalystParam.RadiusUnit = BufferRadiusUnit.MiliMeter;
            GeoStyle style = new GeoStyle();
            style.LineColor = Color.Red;
            GeoRegion geoRegion;
            //清除前面画的缓冲区
            for (int i = 0; i < points.Count; i++)
            {
                int index = mapControl.Map.TrackingLayer.IndexOf("缓冲区");
                if (index != -1)
                {
                    mapControl.Map.TrackingLayer.Remove(index);
                }
            }
           //创建新的缓冲区
            for (int i = 0; i < points.Count; i++)
            {
                geoPoint = new GeoPoint(points[i]);
                bufferAnalystParam.LeftDistance = trackBar1.Value;
                geoRegion = BufferAnalystGeometry.CreateBuffer(geoPoint, bufferAnalystParam);
                geoRegion.Style = style;
                mapControl.Map.TrackingLayer.Add(geoRegion, "缓冲区");
            }
            mapControl.Map.RefreshTrackingLayer();
            //FindBusyRoads(geoPoint);
        }
        public void FindBusyRoads(Point2Ds points)
        {
            this.dataGridView.Columns.Clear();
            this.dataGridView.Rows.Clear();
            CreateTemplate();
            for (int i = 0; i < points.Count; i++)
            {
                double x = points[i].X;
                double y = points[i].Y;
                int radius = trackBar1.Value * 100;
                string apiurl = $"https://api.map.baidu.com/traffic/v1/around?ak=2ASrHePtG3YBKKgXpTKeYDICcrNvMi50&center={y},{x}&radius={radius}&coord_type_input=gcj02&coord_type_output=gcj02";
                HttpClient client = new HttpClient();
                HttpResponseMessage response = client.GetAsync(apiurl).Result;
                string responseContent = response.Content.ReadAsStringAsync().Result;
                // Parse the JSON response and extract the traffic data
                dynamic data = JsonConvert.DeserializeObject(responseContent);
                foreach (var item in data.road_traffic)
                {
                    if (item.congestion_sections != null)
                    {
                        //busyRoads.Add(item.road_name);
                        DataGridViewRow row = new DataGridViewRow();
                        row.CreateCells(dataGridView);
                        row.Cells[0].Value = i;
                        row.Cells[1].Value = item.road_name;
                        //busyRoads.Add(item.road_name);
                        row.Cells[2].Value = item.congestion_sections[0].status;
                        row.Cells[3].Value = item.congestion_sections[0].speed;
                        row.Cells[4].Value = item.congestion_sections[0].congestion_distance;
                        row.Cells[5].Value = item.congestion_sections[0].congestion_trend;
                        row.Cells[6].Value = item.congestion_sections[0].section_desc;
                        dataGridView.Rows.Add(row);
                    }
                }
            }
            dataGridView.Update();
        }
        private void CreateTemplate()
        {
            this.dataGridView.Columns.Add("点号", "点号");
            this.dataGridView.Columns.Add("道路名字", "道路名字");
            this.dataGridView.Columns.Add("拥堵指数", "拥堵指数");
            this.dataGridView.Columns.Add("通行速度", "通行速度");
            this.dataGridView.Columns.Add("拥堵距离", "拥堵距离");
            this.dataGridView.Columns.Add("拥堵趋势", "拥堵趋势");
            this.dataGridView.Columns.Add("描述", "描述");
        }
        public void ShowbusyRoads()
        {
            //DatasetVector result;
            GeoRegion[] geoRegions = new GeoRegion[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                int index = mapControl.Map.TrackingLayer.IndexOf("缓冲区");
                if (index != -1)
                {
                    geoRegions[i] = (GeoRegion)mapControl.Map.TrackingLayer.Get(index);
                    mapControl.Map.TrackingLayer.Remove(index);
                }
            }
            OverlayAnalyst.Intersect(targetDataset, geoRegions, resultDataset, overlayParameter);
            QueryParameter queryParameter = new QueryParameter();
            queryParameter.ResultFields = overlayParameter.SourceRetainedFields;
            queryParameter.OrderBy = overlayParameter.SourceRetainedFields;
            queryParameter.CursorType = CursorType.Static;
            queryParameter.HasGeometry = true;//后续才能基于Geometry进行记录定位d
            int count = dataGridView.RowCount;
            string value;
            GeoLine geoLine;
            for (int i = 0; i < count - 1; i++)
            {
                value = (string)dataGridView.Rows[i].Cells[1].Value.ToString();
                queryParameter.AttributeFilter = string.Format("{0}='{1}'OR {0} like '%{1}%'", "name", value);
                var queryresult = resultDataset.Query(queryParameter);
                if (queryresult.RecordCount != 0)
                {
                    queryresult.MoveFirst();
                    geoLine = (GeoLine)queryresult.GetGeometry();
                    busyRoadsId.Add(queryresult.GetID());
                    geoLine.Style = style;
                    mapControl.Map.TrackingLayer.Add(geoLine, "选中记录");
                    dataGridView.Rows[i].Tag = geoLine;
                    busyRoadsId.Add(queryresult.GetID());
                }
                else
                {
                    queryParameter.AttributeFilter = string.Format("{0} like '%{1}%'", "name", value[1]);
                    queryresult = resultDataset.Query(queryParameter);
                    queryresult.MoveFirst();
                    if (queryresult.RecordCount == 0)
                    {
                        continue;
                    }
                    geoLine = (GeoLine)queryresult.GetGeometry();
                    busyRoadsId.Add(queryresult.GetID());
                    geoLine.Style = style;
                    mapControl.Map.TrackingLayer.Add(geoLine, "选中记录");
                    dataGridView.Rows[i].Tag = geoLine;
                }
            }
            mapControl.Map.RefreshTrackingLayer();
        }
        public void AddBarrier()
        {
            GeoStyle m_style = new GeoStyle();
            Selection selection1 = m_layerPoint.Selection;//网络数据集结点层
            Selection selection2 = m_layerLine.Selection;//网络数据集路段层
            m_style.LineColor = Color.Red;
            m_style.MarkerSize = new Size2D(5, 5);
            m_style.LineWidth = 1;
            Recordset recordset1 = selection1.ToRecordset();
            Recordset recordset2 = selection2.ToRecordset();
            //ShowInfo(recordset1);
            //此处只展示障碍线的信息
            ShowInfo(recordset2);
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dataGridView.AutoResizeColumns();
            try
            {
                Geometry geometry;
                recordset1.MoveFirst();
                recordset2.MoveFirst();
                while (!recordset1.IsEOF)
                {
                    geometry = recordset1.GetGeometry();
                    int id1 = recordset1.GetID();
                    barrierNodes.Add(id1);
                    GeoPoint geoPoint = (GeoPoint)geometry;
                    geoPoint.Style = m_style;
                    mapControl.Map.TrackingLayer.Add(geoPoint, "barrierNode");
                    recordset1.MoveNext();
                }
                while (!recordset2.IsEOF)
                {
                    geometry = recordset2.GetGeometry();
                    int id2 = recordset2.GetID();
                    barrierEdges.Add(id2);
                    GeoLine geoLine = (GeoLine)geometry;
                    geoLine.Style = m_style;
                    mapControl.Map.TrackingLayer.Add(geoLine, "barrierEdge");
                    recordset2.MoveNext();

                }
                //recordset1.Dispose();
                //recordset2.Dispose();
                mapControl.Map.Refresh();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
            finally
            {
                recordset1.Dispose();
                recordset2.Dispose();
            }
        }
        public void transformBarry()
        {
            barrierEdges.AddRange(busyRoadsId);
            MessageBox.Show("添加成功");
        }
        private void ShowInfo(Recordset recordset)
        {
            this.dataGridView.Columns.Clear();
            this.dataGridView.Rows.Clear();

            for (int i = 0; i < recordset.FieldCount; i++)
            {
                //定义并获得字段名称
                String fieldName = recordset.GetFieldInfos()[i].Name;
                //将得到的字段名称添加到dataGridView列中
                this.dataGridView.Columns.Add(fieldName, fieldName);
            }

            //初始化row
            DataGridViewRow row = null;

            //根据选中记录的个数，将选中对象的信息添加到dataGridView中显示
            while (!recordset.IsEOF) //游标性质
            {
                row = new DataGridViewRow();

                //将本条记录的属性部分，构建成一行，添加进入dataGridView中
                for (int i = 0; i < recordset.FieldCount; i++)
                {
                    //定义并获得字段值
                    Object fieldValue = recordset.GetFieldValue(i);
                    //将字段值添加到dataGridView中对应的位置.
                    DataGridViewTextBoxCell cell = new DataGridViewTextBoxCell();
                    if (fieldValue != null)
                    {
                        cell.ValueType = fieldValue.GetType();
                        cell.Value = fieldValue;
                    }
                    row.Cells.Add(cell);
                }

                //技巧：将本条记录的Geometry信息，记录到row.Tag中，将来以便使用。
                row.Tag = recordset.GetGeometry();
                this.dataGridView.Rows.Add(row);
                recordset.MoveNext();
            }
            this.dataGridView.Update();

        }
    }
}
