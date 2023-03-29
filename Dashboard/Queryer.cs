using SuperMap.Data;
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
    public class Queryer
    {
        public DatasetVector datasetVector;
        //private Datasource datasource;
        private MapControl mapControl;
        public ComboBox comboBox;
        public QueryParameter queryParameter;
        private List<Item> selecteditems;
        internal PointSelector selectPointer;
        public List<Item> Selecteditems
        {
            get { return selecteditems; }
        }

        public class Item
        {
            private string name;
            public string Name
            {
                get { return name; }
                set { name = value; }
            }
            private Geometry geometry;
            public Geometry Geometry
            {
                get { return geometry; }
                set { geometry = value; }
            }

            public Item(string name, Geometry geometry)
            {
                this.name = name;
                this.geometry = geometry;
            }
        }
        public Queryer(MapControl mapControl, ComboBox comboBox)
        {
            this.comboBox = comboBox;
            this.mapControl = mapControl;
            //this.datasource = datasource;
            Initialize();
        }
        private void Initialize()
        {
            //this.datasetVector = (DatasetVector)datasource.Datasets["交通点"];
            //comboBox.DisplayMember = "Name";
            //QueryParameter queryParameter = new QueryParameter();
            //queryParameter.ResultFields = new string[1] { "name" };
            //queryParameter.OrderBy = new string[1] { "name" };
            //queryParameter.CursorType = CursorType.Static;
            //queryParameter.HasGeometry = true;//后续才能基于Geometry进行记录定位d
            this.comboBox.TextChanged += ComboBox_TextChanged;
            this.comboBox.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
        }
        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Item item = comboBox.SelectedItem as Item;
            var geoPoint =(GeoPoint) item.Geometry;
            this.mapControl.Map.EnsureVisible(geoPoint);//居中显示要素
            //this.mapControl1.Map.TrackingLayer.Clear();//清除临时绘画层
            //var style = new GeoStyle();
            //style.MarkerSize = new Size2D(8, 8);
            //style.LineColor = Color.Blue;
            //geometry.Style = style;

            ////TextPart textPart=new TextPart (geoPoint.X,geoPoint.Y)
            //this.mapControl.Map.TrackingLayer.Add(geometry, "选中记录");//将geometry符号化后，加入临时绘画层显示
            selectPointer.AddPoint(geoPoint.InnerPoint);
            selectPointer.ShowPoint(geoPoint.InnerPoint);
            selectPointer.m_flag++;
            this.mapControl.Map.Refresh();//刷新地图
        }

        private void ComboBox_TextChanged(object sender, EventArgs e)
        {
            string search = comboBox.Text.Trim();
            Recordset result = null;
            if (string.IsNullOrWhiteSpace(search))
            {
                return;
            }
            comboBox.Items.Clear();
            if (datasetVector == null)
            {
                return;
            }
            queryParameter.AttributeFilter = string.Format("{0} like '%{1}%'", "name", search);
            result = datasetVector.Query(queryParameter);
            //var info = result.GetFieldInfos();
            comboBox.SelectionStart = search.Length;
            string name;
            while (!result.IsEOF)
            {
                name = result.GetFieldValue(1) as string;
                if (name == null)
                {
                    return;
                }
                comboBox.Items.Add(new Item(name, result.GetGeometry()));
                result.MoveNext();
            }
            comboBox.DroppedDown = true;
        }

        private void ShowinfoInMap(Item item)
        {
            Geometry geometry = item.Geometry;
            this.mapControl.Map.EnsureVisible(geometry);//居中显示要素
            //this.mapControl1.Map.TrackingLayer.Clear();//清除临时绘画层
            var style = new GeoStyle();
            style.MarkerSize = new Size2D(8, 8);
            style.LineColor = Color.Blue;
            geometry.Style = style;

            //TextPart textPart=new TextPart (geoPoint.X,geoPoint.Y)
            this.mapControl.Map.TrackingLayer.Add(geometry, "选中记录");//将geometry符号化后，加入临时绘画层显示
            this.mapControl.Map.Refresh();//刷新地图
        }
        private Recordset GetResult()
        {
            string search = comboBox.Text.Trim();
            Recordset result = null;
            if (string.IsNullOrWhiteSpace(search))
            {
                return null;
            }
            comboBox.Items.Clear();
            if (datasetVector == null)
            {
                return null;
            }
            queryParameter.AttributeFilter = string.Format("{0} like '%{1}%'", "name", search);
            result = datasetVector.Query(queryParameter);
            //var info = result.GetFieldInfos();
            comboBox.SelectionStart = search.Length;
            return result;
        }
        private void ShowResult(Recordset result)
        {
            string name;
            while (!result.IsEOF)
            {
                name = result.GetFieldValue(1) as string;
                if (name == null)
                {
                    return;
                }
                comboBox.Items.Add(new Item(name, result.GetGeometry()));
                result.MoveNext();
            }
            comboBox.DroppedDown = true;
            //this.Cursor = Cursors.Arrow;
        }
    }
}
