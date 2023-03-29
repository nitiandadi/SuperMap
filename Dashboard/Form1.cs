using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using SuperMap.Data;
using SuperMap.Mapping;
using SuperMap.UI;
using SuperMap.Analyst.SpatialAnalyst;
using Newtonsoft.Json;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Dashboard
{
    public partial class Form1 : Form
    {
        private Workspace workspace;
        private MapControl mapControl;
        private City city;
        //private List<string> busyRoads = new List<string>();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            workspace = new Workspace();
            mapControl = new MapControl();
            mapControl.Dock = DockStyle.Fill;
            this.panel4.Controls.Add(mapControl);
            string fileName = Application.StartupPath + @"\data\ChengDu\ChengDu.smwu";
            //避免连续打开工作空间导致程序异常     
            mapControl.Map.Close();
            workspace.Close();
            mapControl.Map.Refresh();
            WorkspaceConnectionInfo connectionInfo = new WorkspaceConnectionInfo(fileName);
            workspace.Open(connectionInfo);
            mapControl.Map.Workspace = workspace;

            if (workspace.Maps.Count == 0)
            {
                MessageBox.Show("当前工作空间不存在地图");
                return;
            }

            mapControl.Map.Open("综合地图");
            if (mapControl.Map.Layers != null)
            {
                mapControl.Map.Layers.Remove(0);
                mapControl.Map.Layers.Remove(0);
            }
            string serverLink1 = "http://t0.tianditu.gov.cn/vec_c";
            string serverLink2 = "http://t0.tianditu.gov.cn/cva_c";
            serverLink1 += "/wmts?tk=\t8ddf018e7cd71698c3a2da476a0956b0";
            serverLink2 += "/wmts?tk=\t8ddf018e7cd71698c3a2da476a0956b0";
            DatasourceConnectionInfo dsconnectinfo1 = new DatasourceConnectionInfo();
            dsconnectinfo1.EngineType = EngineType.OGC;
            dsconnectinfo1.Server = serverLink1;
            dsconnectinfo1.Driver = "WMTS";
            dsconnectinfo1.Alias = "天地图底图";
            DatasourceConnectionInfo dsconnectinfo2 = new DatasourceConnectionInfo();
            dsconnectinfo2.EngineType = EngineType.OGC;
            dsconnectinfo2.Server = serverLink2;
            dsconnectinfo2.Driver = "WMTS";
            dsconnectinfo2.Alias = "天地图底图标记";
            Datasource ds1 = workspace.Datasources.Open(dsconnectinfo1);
            Datasource ds2 = workspace.Datasources.Open(dsconnectinfo2);
            mapControl.Map.Layers.Add(ds1.Datasets[0], true);
            mapControl.Map.Layers.Add(ds2.Datasets[0], true);
            city = new ChengDu(mapControl, toolStripComboBox1.ComboBox, dataGridView, trackBar);
            Initalize(city);

        }
        private void Initalize(City city)
        {
            this.city=city;
            mapControl.Map.Center = city.Center;
            trackBar.Visible = false;
            btn_Show.Visible = false;
            Btn_Find.Visible = false;
            btn_AddBarry.Visible = false;
            this.btn_Guide.Visible = false;
            this.Btn_Stop.Visible = false;
            this.Btn_Clear.Visible = false;
            this.btn_Show.Visible = false;
            this.btn_AddBarry.Visible = false;
            city.SelectPointMode();
            mapControl.Map.Refresh();
        }
        private void btnPoint_Click(object sender, EventArgs e)
        {
            pnlNav.Height = btnPoint.Height;
            pnlNav.Top = btnPoint.Top;
            pnlNav.Left = btnPoint.Left;
            btnPoint.BackColor = Color.FromArgb(46, 51, 73);
            trackBar.Visible = false;
            btn_Show.Visible = false;
            Btn_Find.Visible = false;
            btn_AddBarry.Visible = false;
            this.btn_Guide.Visible = false;
            this.Btn_Stop.Visible = false;
            this.Btn_Clear.Visible = false;
            toolStrip1.Items[5].Visible = true;
            city.SelectPointMode();
        }

        private void btnbarry_Click(object sender, EventArgs e)
        {
            pnlNav.Height = btnbarry.Height;
            pnlNav.Top = btnbarry.Top;
            btnbarry.BackColor = Color.FromArgb(46, 51, 73);
            city.trackBar = this.trackBar;
            this.trackBar.Visible = true;
            this.btn_Show.Visible = false;
            this.btn_AddBarry.Visible = false;
            this.Btn_Find.Visible = true;
            this.btn_Guide.Visible = false;
            this.Btn_Stop.Visible = false;
            this.Btn_Clear.Visible = false;
            toolStrip1.Items[5].Visible = false;
            city.SelectBarryMode();
            dataGridView.AutoResizeColumns();
            dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            //routeAnalyster= new RouteAnalyster(workspace, mapControl, dataGridView1);
        }

        private void BtnAnalyse_Click(object sender, EventArgs e)
        {
            pnlNav.Height = BtnAnalyse.Height;
            pnlNav.Top = BtnAnalyse.Top;
            BtnAnalyse.BackColor = Color.FromArgb(46, 51, 73);
            city.SelectAnalyserMode();
            city.routeAnalyster.Analyst();
            this.trackBar.Visible = false;
            this.btn_Show.Visible = false;
            this.Btn_Find.Visible = false;
            btn_AddBarry.Visible = false;
            this.btn_Guide.Visible = true;
            this.Btn_Stop.Visible = true;
            this.Btn_Clear.Visible = true;
            btnbarry.Enabled = false;
            btnPoint.Enabled = false;
            this.btn_Show.Visible = false;
            this.btn_AddBarry.Visible = false;
            toolStrip1.Items[5].Visible = false;
        }

        private void btnContactUs_Click(object sender, EventArgs e)
        {
            pnlNav.Height = btnContactUs.Height;
            pnlNav.Top = btnContactUs.Top;
            btnContactUs.BackColor = Color.FromArgb(46, 51, 73);
        }

        private void btnsettings_Click(object sender, EventArgs e)
        {
            pnlNav.Height = btnsettings.Height;
            pnlNav.Top = btnsettings.Top;
            btnsettings.BackColor = Color.FromArgb(46, 51, 73);
        }

        private void btnDashbord_Leave(object sender, EventArgs e)
        {
            btnPoint.BackColor = Color.FromArgb(24, 30, 54);
        }

        private void btnAnalytics_Leave(object sender, EventArgs e)
        {
            btnbarry.BackColor = Color.FromArgb(24, 30, 54);
        }

        private void btnCalender_Leave(object sender, EventArgs e)
        {
            BtnAnalyse.BackColor = Color.FromArgb(24, 30, 54);
        }

        private void btnContactUs_Leave(object sender, EventArgs e)
        {
            btnContactUs.BackColor = Color.FromArgb(24, 30, 54);
        }

        private void btnsettings_Leave(object sender, EventArgs e)
        {
            //btnsettings.BackColor = Color.FromArgb(24, 30, 54);
        }

        private void toolStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
        }

        
        private void button1_Click(object sender, EventArgs e)
        {
            Point2Ds points = city.pointSelector.points;
            this.city.barrySelector.FindBusyRoads(points);
            this.btn_Show.Visible = true;    
            this.btn_AddBarry.Visible=true;
        }
       

        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            mapControl.Action = SuperMap.UI.Action.Select2;
        }

        private void toolStripButton2_Click(object sender, EventArgs e)
        {
            mapControl.Action = SuperMap.UI.Action.Pan;
        }

        private void toolStripButton3_Click(object sender, EventArgs e)
        {
            mapControl.Action = SuperMap.UI.Action.ZoomFree;
        }

        private void toolStripButton4_Click(object sender, EventArgs e)
        {
            mapControl.Action = SuperMap.UI.Action.ZoomIn;

        }

        private void toolStripButton5_Click(object sender, EventArgs e)
        {
            mapControl.Action = SuperMap.UI.Action.ZoomOut;
        }

        private void btn_Show_Click(object sender, EventArgs e)
        {
            MessageBox.Show("请稍等片刻");
            this.city.barrySelector.ShowbusyRoads();
        }
       

        private void dataGridView1_RowHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            city.ShowGeometry(e.RowIndex);
        }

        private void btn_AddBarry_Click(object sender, EventArgs e)
        {
            city.barrySelector.transformBarry();
        }


        private void Btn_Stop_Click(object sender, EventArgs e)
        {
            city.routeAnalyster.Stop();

        }

        private void btn_Guide_Click(object sender, EventArgs e)
        {
            city.routeAnalyster.Play();
        }

        private void Btn_Clear_Click(object sender, EventArgs e)
        {
            city.routeAnalyster.Clear();
            btnPoint.Enabled = true;
            btnbarry.Enabled = true;
            city.pointSelector.m_flag = 1;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            City city;
            if (comboBox1.SelectedIndex==0)
            {
                //MessageBox.Show("请稍等片刻");
                city = new BeiJing(mapControl, toolStripComboBox1.ComboBox, dataGridView, trackBar);
                Initalize(city);
            }
            else
            {
                //MessageBox.Show("请稍等片刻");
                city = new ChengDu(mapControl, toolStripComboBox1.ComboBox, dataGridView, trackBar);
                Initalize(city);
            }
        }
    }
}
 