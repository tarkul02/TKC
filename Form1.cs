using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Configuration;


namespace SAP_Batch_GR_TR
{
    public partial class Form1 : Form
    {
       // private void GetconnectionString() 
        //{
           // ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings["BarcodeEntities"];
           // string conString = "";
            //if (setting != null) {
          //      conString = setting.ConnectionString;
          //  }
       // }
        public Form1()
        {
            InitializeComponent();
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            Start_update();
            UpLodeGRtoSAP();
            UpLodeTRtoSAP();
            End_update();
            Application.Exit();
        }
        string start_Time = "";
        private void Start_update()
        {
            var sql = " select isnull(A.GR_NO,0)GR_NO, isnull(B.GR_Re_NO,0)GR_Re_NO, isnull(C.TR_NO,0)TR_NO, isnull(D.TR_Re_NO,0)TR_Re_NO,FORMAT(getdate(), 'yyyy-MM-dd HH:mm:ss:fff') as Start_Time  From (select count(*) GR_NO, Action from [Barcode].[dbo].[v_sap_batch_gr] where Action = 1 group by Action) A " +
                "left join(select count(*) GR_Re_NO, Action from [Barcode].[dbo].[v_sap_batch_gr_redo] where Action = 1 group by Action) B ON A.Action = B.Action " +
                "left join(select count(*) TR_NO, Action From (select count(*) TR_NO, SLIPNO, Action from [Barcode].[dbo].[v_sap_batch_tr] where Action = 1 GROUP BY SLIPNO, Action) C1 GROUP BY C1.Action ) C ON B.Action = C.Action or A.Action = C.Action " +
                "left join(select count(*) TR_Re_NO, Action From (select count(*) TR_Re_NO, SLIPNO, Action from [Barcode].[dbo].[v_sap_batch_tr_redo] where Action = 1 GROUP BY SLIPNO, Action)D1 Group by D1.Action) D ON C.Action = D.Action or B.Action = D.Action or A.Action = D.Action";
            var dt = GetQuery(sql);
            start_Time = dt.Rows[0]["Start_Time"].ToString();
            sql = "INSERT INTO [Barcode].[dbo].[T_SAP_Batch_GR_TR_Log] (GR_NO, GR_Re_NO,TR_NO,TR_Re_NO,Start_Time) VALUES (@GR_NO,@GR_Re_NO,@TR_NO,@TR_Re_NO,@Start_Time)";
            //string connString = "Data Source=172.18.1.49;User ID=tkcdba;Password=tkcdba;";
            ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings["BarcodeEntities"];
            string connString = "";
            
            if (setting != null)
            {
                connString = setting.ConnectionString;
            }

            SqlConnection conn = new SqlConnection(connString);
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {            
                cmd.Parameters.AddWithValue("@GR_NO", dt.Rows[0]["GR_NO"].ToString());
                cmd.Parameters.AddWithValue("@GR_Re_NO", dt.Rows[0]["GR_Re_NO"].ToString());
                cmd.Parameters.AddWithValue("@TR_NO", dt.Rows[0]["TR_NO"].ToString());
                cmd.Parameters.AddWithValue("@TR_Re_NO", dt.Rows[0]["TR_Re_NO"].ToString());
                cmd.Parameters.AddWithValue("@Start_Time", dt.Rows[0]["Start_Time"].ToString());
                conn.Open();
                int result = cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        private void End_update()
        {
            var sql = "UPDATE [Barcode].[dbo].[T_SAP_Batch_GR_TR_Log] SET End_Time = @End_Time where Start_Time = '"+ start_Time + "'";
            // string connString = "Data Source=172.18.1.49;User ID=tkcdba;Password=tkcdba;";

            ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings["BarcodeEntities"];
            string connString = "";
            if (setting != null)
            {
                connString = setting.ConnectionString;
            }

            SqlConnection conn = new SqlConnection(connString);
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@End_Time", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff"));
                conn.Open();
                int result = cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

         private void UpLodeGRtoSAP()
        {
            string sql = "";
            //GR
            string partno = "";
            int qty = 0;
            string custid = "";
            string store = "";
            string postdate = "";
            string headertext = "";
            DataTable GRdata = new DataTable();
            DataTable GRErrdata = new DataTable();
            var ws = new SapTransfer.post();
          

            sql = "select * from [Barcode].[dbo].[v_sap_batch_gr] where Action = 1";
            GRdata = GetQuery(sql);
            sql = "select * from [Barcode].[dbo].[v_sap_batch_gr_redo] where Action = 1";
            GRErrdata = GetQuery(sql);

            if (GRdata.Rows.Count > 0)
            {
                foreach (DataRow item in GRdata.Rows)
                {
                   
                        partno = item["MatNo"].ToString().Trim();
                        qty = Convert.ToInt32(item["QRQty"].ToString());
                        custid = item["CustID"].ToString().Trim();
                        store = item["SLoc"].ToString().Trim();
                        postdate = item["PostDate"].ToString().Trim();
                        headertext = "IT|" + item["HeaderText"].ToString().Trim();
                        var res = ws.ADDSTOCKBYEXCEL(partno, qty, custid, store, postdate, headertext);
                    
                }
            }
            if (GRErrdata.Rows.Count>0)
            {
                foreach (DataRow item in GRErrdata.Rows)
                {

                        partno = item["MatNo"].ToString().Trim();
                        qty = Convert.ToInt32(item["QRQty"].ToString());
                        custid = item["CustID"].ToString().Trim();
                        store = item["SLoc"].ToString().Trim();
                        postdate = item["PostDate"].ToString().Trim();
                        headertext = "IT|" + item["HeaderText"].ToString().Trim();
                        var res = ws.ADDSTOCKBYEXCEL(partno, qty, custid, store, postdate, headertext);
                    
                }
            }



            }


        private void UpLodeTRtoSAP() 
        {
            string sql = "";
            string Slipno = "";
            string Datatype = "";
            DataTable TRdata = new DataTable();
            DataTable TRErrdata = new DataTable();

            var ws = new SapTransfer.post();

            sql = "select count(*) ,SLIPNO from [Barcode].[dbo].[v_sap_batch_tr] where Action = 1 GROUP BY SLIPNO";
            TRdata = GetQuery(sql);
            sql = "select count(*) ,SLIPNO from [Barcode].[dbo].[v_sap_batch_tr_redo] where Action = 1 GROUP BY SLIPNO";
            TRErrdata = GetQuery(sql);

            if (TRdata.Rows.Count > 0)
            {
                foreach (DataRow item in TRdata.Rows)
                {
                    Slipno = "IT|" + item["SLIPNO"].ToString().Trim();
                    Datatype = "12";
                    var res = ws.TransferStockDataToSAP_311(Slipno, Datatype);
                }
            }

            if (TRErrdata.Rows.Count > 0)
            {
                foreach (DataRow item in TRErrdata.Rows)
                {
                    Slipno = "IT|" + item["SLIPNO"].ToString().Trim();
                    Datatype = "13";
                    var res = ws.TransferStockDataToSAP_311(Slipno, Datatype);
                }
            }
        }

        public DataTable GetQuery(string sql)
        {
            var dt = new DataTable();
            //string connString = "Data Source=DESKTOP-SBTKHDD\SQLEXPRESS;User ID=tkcdba;Password=tkcdba;";

            ConnectionStringSettings setting = ConfigurationManager.ConnectionStrings["BarcodeEntities"];
            string connString = "";
            if (setting != null)
            {
                connString = setting.ConnectionString;
            }

            SqlConnection conn = new SqlConnection(connString);
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            using (SqlDataAdapter da = new SqlDataAdapter(cmd))
            {
                conn.Open();
                da.Fill(dt);
                conn.Close();
                da.Dispose();
            }
            
            return dt;
        }



    }
}
