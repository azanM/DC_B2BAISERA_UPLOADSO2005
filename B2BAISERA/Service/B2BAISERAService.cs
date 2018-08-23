using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Configuration;
using System.Data.OleDb;
using System.Timers;
using B2BAISERA.Properties;
using System.Net;
using B2BAISERA.wsB2B;

namespace B2BAISERA.Service
{
    partial class B2BAISERAService : ServiceBase
    {
        private string ConnectionString = ConfigurationManager.AppSettings["ConnectionString"];
        private OleDbConnection DBConnection;
        private Timer timService = new Timer();
        private int iErrorCount;

        //private bool acknowledge;
        //private string ticketNo = "";
        //private string message = "";

        public B2BAISERAService()
        {
            InitializeComponent();
            if (!EventLog.SourceExists("B2BAISERAServiceSource"))
            {
                EventLog.CreateEventSource("B2BAISERAServiceSource", "B2BAISERALog");
            }
            this.evLog.Source = "B2BAISERAServiceSource";
            this.evLog.Log = "B2BAISERALog";
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Add code here to start your service.
            this.evLog.WriteEntry("B2BAISERA Service Started.");
            try
            {
                this.DBConnection = new OleDbConnection(this.ConnectionString);
                this.DBConnection.Open();
                this.evLog.WriteEntry("Open Database Connection.");
                this.timService.Elapsed += new ElapsedEventHandler(this.OnElapsedTime);
                this.timService.Interval = 60000.0;
                this.timService.Enabled = true;
            }
            catch (Exception ex)
            {
                this.evLog.WriteEntry("Error Open Database : " + ex.Message.ToString());
            }
        }

        private void OnElapsedTime(object source, ElapsedEventArgs e)
        {
            this.timService.Enabled = false;
            if (this.DBConnection.State != ConnectionState.Open)
            {
                try
                {
                    this.DBConnection.Open();
                    this.evLog.WriteEntry("Open Database Connection.");
                }
                catch (Exception ex)
                {
                    this.evLog.WriteEntry("Error Open Database : " + ex.Message.ToString());
                    this.iErrorCount++;
                    if (this.iErrorCount == 5)
                    {
                        base.Stop();
                    }
                }
            }
            if (this.DBConnection.State == ConnectionState.Open)
            {
                try
                {
                    if (this.iErrorCount < 10)
                    {
                        this.iErrorCount = 0;
                    }
                    ////INITIALIZE LOGIN
                    //var loginReq = new B2BAIWsDMZ.LoginRequest();
                    //loginReq.UserName = Resources.EncryptUserName;
                    //loginReq.Password = Resources.EncryptPassword;
                    //loginReq.ClientTag = Resources.ClientTag;
                    //B2BAIWsDMZ.B2BAIWebServiceDMZSoapClient wsB2B = new B2BAIWsDMZ.B2BAIWebServiceDMZSoapClient();
                    //WebProxy myProxy = new WebProxy(Resources.WebProxyAddress, true);
                    //myProxy.Credentials = new NetworkCredential(Resources.NetworkCredentialUserName, Resources.NetworkCredentialPassword, Resources.NetworkCredentialProxy);
                    //wsB2B.ClientCredentials.Windows.ClientCredential = System.Net.CredentialCache.DefaultCredentials as NetworkCredential;
                    //var wsResult = wsB2B.LoginAuthentication(loginReq);
                    //acknowledge = wsResult.Acknowledge;
                    //ticketNo = wsResult.TicketNo;
                    ////Session["TicketNo"] = wsResult.TicketNo;
                    //message = wsResult.Message;

                    ////END INITIALIZE LOGIN


                    //string StrQuery = "SELECT DISTINCT BRANCH_ID, GL_YEAR FROM GEN_GL_PROCESS WHERE GL_STATUS IN ('O','P') ";
                    //OleDbCommand DBCommand = new OleDbCommand(StrQuery, this.DBConnection);
                    //OleDbDataReader myRead = DBCommand.ExecuteReader();
                    //while (myRead.Read())
                    //{
                    //    this.evLog.WriteEntry("Start Process Branch " + Convert.ToString(myRead["BRANCH_ID"]) + " On Year " + Convert.ToString(myRead["GL_YEAR"]));
                    //    OleDbCommand DBCommand2 = new OleDbCommand(string.Concat(new string[]
                    //    {
                    //        " UPDATE GEN_GL_PROCESS set GL_STATUS = 'P' , GL_START_DATE = SYSDATE, GL_START_TIME = TO_CHAR(SYSDATE,'HH24:MI:SS') WHERE GL_STATUS IN ('O','P') AND GL_YEAR = ", 
                    //        Convert.ToString(myRead["GL_YEAR"]), 
                    //        " AND BRANCH_ID = '", 
                    //        Convert.ToString(myRead["BRANCH_ID"]), 
                    //        "' "
                    //    }), this.DBConnection);
                    //    DBCommand2.CommandType = CommandType.Text;
                    //    DBCommand2.ExecuteNonQuery();
                    //    DBCommand2.Dispose();
                    //    DBCommand2 = new OleDbCommand("sp_add_to_report_gl", this.DBConnection);
                    //    DBCommand2.CommandType = CommandType.StoredProcedure;
                    //    DBCommand2.Parameters.Clear();
                    //    DBCommand2.Parameters.Add("v_branch_id", OleDbType.VarChar).Value = Convert.ToString(myRead["BRANCH_ID"]);
                    //    DBCommand2.Parameters.Add("v_year", OleDbType.Integer).Value = Convert.ToInt32(myRead["GL_YEAR"]);
                    //    DBCommand2.ExecuteNonQuery();
                    //    DBCommand2.Dispose();
                    //    DBCommand2 = new OleDbCommand(string.Concat(new string[]
                    //    {
                    //        " UPDATE GEN_GL_PROCESS set GL_STATUS = 'C' , GL_END_DATE = SYSDATE, GL_END_TIME = TO_CHAR(SYSDATE,'HH24:MI:SS') WHERE GL_STATUS IN ('O','P') AND GL_YEAR = ", 
                    //        Convert.ToString(myRead["GL_YEAR"]), 
                    //        " AND BRANCH_ID = '", 
                    //        Convert.ToString(myRead["BRANCH_ID"]), 
                    //        "' "
                    //    }), this.DBConnection);
                    //    DBCommand2.CommandType = CommandType.Text;
                    //    DBCommand2.ExecuteNonQuery();
                    //    DBCommand2.Dispose();
                    //    this.evLog.WriteEntry("Finish Process Branch " + Convert.ToString(myRead["BRANCH_ID"]) + " On Year " + Convert.ToString(myRead["GL_YEAR"]));
                    //}
                    //myRead.Close();
                    //DBCommand.Dispose();
                }
                catch (Exception ex2)
                {
                    this.evLog.WriteEntry("Error : " + ex2.Message.ToString());
                    if (this.iErrorCount == 0)
                    {
                        this.iErrorCount = 10;
                    }
                    this.iErrorCount++;
                    if (this.iErrorCount == 15)
                    {
                        base.Stop();
                    }
                }
            }
            this.timService.Enabled = true;
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
            if (this.DBConnection.State == ConnectionState.Open)
            {
                try
                {
                    this.DBConnection.Close();
                    this.evLog.WriteEntry("Close Database Connection.");
                }
                catch (Exception ex)
                {
                    this.evLog.WriteEntry("Error Close Database : " + ex.Message.ToString());
                }
            }
            this.evLog.WriteEntry("B2BAISERA Service Stoped.");
        }
    }
}
