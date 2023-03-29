using Newtonsoft.Json;
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
    internal class PointSelector
    {
        public MapControl  mapControl { get; private set; }
        public DataGridView dataGridView { get; private set; }
        internal int m_flag;
        public  Point2Ds points{ get; private set; }
        private GeoStyle m_style;
        private TrackingLayer m_trackingLayer;
        internal bool isEnabled;

        public PointSelector(MapControl mapControl,DataGridView dataGridView)
        {
            this.mapControl = mapControl;
            this.dataGridView = dataGridView;
            m_flag = 1;
            mapControl.Action = SuperMap.UI.Action.Select;
            m_style = new GeoStyle();
            m_style.LineColor = Color.Green;
            m_style.MarkerSize = new Size2D(8, 8);
            m_trackingLayer = mapControl.Map.TrackingLayer;
            m_trackingLayer.IsAntialias = true;
            points = new Point2Ds();
            mapControl.MouseDown += new MouseEventHandler(m_mapControl_MouseDown);

        }

        private void m_mapControl_MouseDown(object sender, MouseEventArgs e)
        {
            try
            {
                if (e.Button == MouseButtons.Left&& this.isEnabled==true)
                {
                    Point point = new Point(e.X, e.Y);
                    Point2D mapPoint = mapControl.Map.PixelToMap(point);
                    if (mapControl.Map.Bounds.Contains(mapPoint))
                    {
                        if (mapControl.Action == SuperMap.UI.Action.Select )
                        {
                            AddPoint(mapPoint);
                            ShowPoint(mapPoint);
                            m_flag++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }

        internal void ShowPoint(Point2D mapPoint)
        {
            if (dataGridView.Rows.Count == 0 && dataGridView.Columns.Count == 0)
            {
                dataGridView.Columns.Add("点号", "点号");
                dataGridView.Columns.Add("经度", "经度");
                dataGridView.Columns.Add("纬度", "纬度");
                dataGridView.Columns.Add("地址信息", "地址信息");
                dataGridView.Columns.Add("周围地点", "周围地点");
                FillDataGrid(mapPoint);
            }
            else
            {
                FillDataGrid(mapPoint);
            }
        }

        private void FillDataGrid(Point2D mapPoint)
        {
            double x = mapPoint.X;
            double y = mapPoint.Y;
            string apiurl = $"https://api.map.baidu.com/reverse_geocoding/v3/?ak=OlraGzGct3ykquuvDS0xgnr7wEEcHnqq&output=json&coordtype=wgs84ll&location={y},{x}";
            HttpClient client = new HttpClient();
            HttpResponseMessage response = client.GetAsync(apiurl).Result;
            string responseContent = response.Content.ReadAsStringAsync().Result;
            // Parse the JSON response and extract the traffic data
            dynamic data = JsonConvert.DeserializeObject(responseContent);
            //busyRoads.Add(item.road_name);
            DataGridViewRow row = new DataGridViewRow();
            row.CreateCells(dataGridView);
            row.Cells[0].Value = m_flag;
            row.Cells[1].Value = data.result.location.lat;
            row.Cells[2].Value = data.result.location.lng;
            row.Cells[3].Value = data.result.formatted_address;
            row.Cells[4].Value = data.result.business;
            row.Tag = new GeoPoint(mapPoint);
            dataGridView.Rows.Add(row);
            dataGridView.Update();
        }

        internal void AddPoint(Point2D mapPoint )
        {
            try
            {
                //m_Points点坐标集合，收集来提供给TransportationAnalystParameter对象
                if (this.isEnabled)
                {
                    points.Add(mapPoint);
                }
                // 在跟踪图层上添加点，用于显示
                GeoPoint geoPoint = new GeoPoint(mapPoint);
                geoPoint.Style = m_style;
                m_trackingLayer.Add(geoPoint, "Point");

                // 在跟踪图层上添加文本对象，标注途经点的序号
                TextPart part = new TextPart();
                part.X = geoPoint.X;
                part.Y = geoPoint.Y;
                part.Text = m_flag.ToString();
                GeoText geoText = new GeoText(part);
                m_trackingLayer.Add(geoText, "Point");
                mapControl.Map.RefreshTrackingLayer();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
            }
        }
    }
}
