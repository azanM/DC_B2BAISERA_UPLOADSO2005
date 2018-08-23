using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using B2BAISERA.Log;
using B2BAISERA.Models.Providers;
using B2BAISERA.Properties;
using System.Net;
using B2BAISERA.Models;
using System.Globalization;
using B2BAISERA.Models.EFServer;
using System.Diagnostics;
using System.Configuration;
using System.IO;
using System.Net.Mail;

namespace B2BAISERA
{
    public partial class UploadS02005 : Form
    {
        private static string fileType = "S02005";
        private bool acknowledge;
        private string ticketNo = "";
        private string message = "";

        public UploadS02005()
        {
            InitializeComponent();
        }

        private void UploadS02005_Load(object sender, EventArgs e)
        {
            LogEvent logEvent = new LogEvent();
            TransactionProvider transactionProvider = new TransactionProvider();
            List<CUSTOM_S02005_TEMP_HS> tempHS = new List<CUSTOM_S02005_TEMP_HS>();
            List<CUSTOM_S02005_TEMP_IS> tempIS = new List<CUSTOM_S02005_TEMP_IS>();
            List<CUSTOM_S02005_TEMP_HS> tempHSISChecked = new List<CUSTOM_S02005_TEMP_HS>();
            List<CUSTOM_S02005_TEMP_IS> tempHSISChecked2 = new List<CUSTOM_S02005_TEMP_IS>();

            //add by fhi 28.11.2014 : penambahan feature send email notify bila ACTUALRECEIVEDINV is null/kosong
            List<string> proposals = new List<string>();
            //end

            try
            {
                transactionProvider.DeleteAllTempHSISS02005();
                do
                {
                    tempHS = transactionProvider.PaymentSeraToAIHS();
                    tempIS = transactionProvider.PaymentSeraToAIIS();

                    //add by fhi 03.12.2014 : add notify email jika ACTUALRECEIVEDINV null atau kosong


                    proposals = transactionProvider.collectDataProposal();

                    string logErr = string.Format("{0}\\Error.Log", ConfigurationManager.AppSettings["LogErrorFolder"]);
                    File.WriteAllText(logErr, string.Empty);

                    for (int i = 0; i < proposals.Count; i++)
                    {
                        string ponumber = proposals[i].ToString();
                        feedback(ponumber, "ACTUALRECEIVEDINV : Kosong/Null");
                    }

                    sendEmail(logErr);
                    //end

                    tempHSISChecked = transactionProvider.CheckingHistoryHSIS(tempHS, tempIS);
                    tempHSISChecked2 = transactionProvider.CheckingHistoryHSIS2(tempHS, tempIS);

                    //transactionProvider.DeleteAllTempHSISS02005();

                    if (tempHSISChecked.Count > 0 && tempHSISChecked2.Count > 0)
                    {
                        LoginAuthentication();
                        if (acknowledge == false || ticketNo == string.Empty)
                        {
                            //close
                        }
                        else Upload(tempHSISChecked, tempHSISChecked2);
                        //Upload(tempHSISChecked, tempHSISChecked2);

                        tempHS = new List<CUSTOM_S02005_TEMP_HS>();
                        tempIS = new List<CUSTOM_S02005_TEMP_IS>();
                        tempHSISChecked = new List<CUSTOM_S02005_TEMP_HS>();
                        tempHSISChecked2 = new List<CUSTOM_S02005_TEMP_IS>();

                        //tempHS = transactionProvider.PaymentSeraToAIHS();
                        //tempIS = transactionProvider.PaymentSeraToAIIS();

                        tempHSISChecked = transactionProvider.CheckingHistoryHSIS(tempHS, tempIS);
                        tempHSISChecked2 = transactionProvider.CheckingHistoryHSIS2(tempHS, tempIS);

                        transactionProvider.DeleteAllTempHSISS02005();

                    }
                } while (tempHSISChecked.Count > 0 && tempHSISChecked2.Count > 0);

                logEvent.WriteDBLog("", "UploadS02005_Load", false, "", "Upload Finish.", fileType, "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02005.exe");
            }
            catch (Exception ex)
            {
                LblResult.Text = ex.Message;
                LblAcknowledge.Text = "";
                LblTicketNo.Text = "";
                LblMessage.Text = "";

                //logevent login failed
                logEvent.WriteDBLog("", "UploadS02005_Load", false, "", ex.Message, fileType, "SERA");
                transactionProvider.DeleteAllTempHSISS02005();
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02005.exe");
            }
        }

        

        private void LoginAuthentication()
        {
             LogEvent logEvent = new LogEvent();
            TransactionProvider transactionProvider = new TransactionProvider();
            try
            {
                using (wsB2B.B2BAIWebServiceDMZ wsB2B = new wsB2B.B2BAIWebServiceDMZ())
                {
                    var User = transactionProvider.GetUser("SERA", "SERA", "B2BAITAG");
                    if (User != null)
                    {
                        var loginReq = new wsB2B.LoginRequest();
                        loginReq.UserName = User.UserCode;
                        loginReq.Password = User.PassCode;
                        loginReq.ClientTag = User.ClientTag;

                        //WebProxy myProxy = new WebProxy(Resources.WebProxyAddress, true);
                        //myProxy.Credentials = new NetworkCredential(Resources.NetworkCredentialUserName, Resources.NetworkCredentialPassword, Resources.NetworkCredentialProxy);

                        //WebProxy myProxy = new WebProxy();
                        //myProxy.Credentials = new NetworkCredential(Resources.NetworkCredentialUserName, Resources.NetworkCredentialPassword, Resources.NetworkCredentialProxy);
                        //myProxy.Credentials = new NetworkCredential("backup", "serasibackup", "trac.astra.co.id");
                        //myProxy.Credentials = new NetworkCredential("rika009692", "mickey1988", "trac.astra.co.id");
                        //myProxy.Credentials = new NetworkCredential("genrpt", "serasera", "trac.astra.co.id");
                        //wsB2B.Proxy = myProxy;

                        var wsResult = wsB2B.LoginAuthentication(loginReq);
                        acknowledge = wsResult.Acknowledge;
                        ticketNo = wsResult.TicketNo;
                        message = wsResult.Message;
                        //logevent login succeded
                        logEvent.WriteDBLog("B2BAIWebServiceDMZ", "LoginAuthentication", acknowledge, ticketNo, message, fileType, "SERA");
                    }
                }
            }
            catch (Exception ex)
            {
                LblResult.Text = ex.Message;
                LblAcknowledge.Text = "";
                LblTicketNo.Text = "";
                LblMessage.Text = "";

                //logevent login failed
                logEvent.WriteDBLog("B2BAIWebServiceDMZ", "LoginAuthentication", acknowledge, ticketNo, "webservice message : " + message + ". exception message : " + ex.Message, fileType, "SERA");

                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02005.exe");
            }
        }

        private void Upload(List<CUSTOM_S02005_TEMP_HS> tempHSISChecked, List<CUSTOM_S02005_TEMP_IS> tempHSISChecked2)
        {
            LogEvent logEvent = new LogEvent();
            TransactionProvider transactionProvider = new TransactionProvider();
            TransactionViewModel transaction = null;
            wsB2B.TransactionData[] transactionDataArray = null;
            List<S02005HSNewViewModel> transactionDataDetailHS = new List<S02005HSNewViewModel>();
            List<S02005ISNewViewModel> transactionDataDetailIS = new List<S02005ISNewViewModel>();
            List<string> arrHSIS = null;

            try
            {
                ////1.Get HS/IS FROM EPROC+STREAMLINER //2.INSERT INTO TEMP_HS + TEMP_IS //3.GET FROM TEMP_HS + TEMP_IS
                //var tempHS = transactionProvider.PaymentSeraToAIHS();
                //var tempIS = transactionProvider.PaymentSeraToAIIS();

                ////CHECKING EVER SEND TO UPLOAD BY PONUMBER
                //var tempHSISChecked = transactionProvider.CheckingHistoryHSIS(tempHS, tempIS);
                //var tempHSISChecked2 = transactionProvider.CheckingHistoryHSIS2(tempHS, tempIS);

                //4.INSERT INTO LOG TRANSACTION HEADER DETAIL + DELETE TEMP
                var intResult = transactionProvider.InsertLogTransactionS02005New(tempHSISChecked, tempHSISChecked2);

                //5.GET DATA FROM LOG TRANSACTION HEADER DETAIL 
                if (intResult != 0)
                {
                    //a.GET TRANSACTION
                    transaction = transactionProvider.GetTransactionS02005New();

                    //b.GET TRANSACTION DATA
                    if (transaction != null)
                    {
                        //transactionData = transactionProvider.GetTransactionData(transaction.ID);
                        transactionDataArray = transactionProvider.GetTransactionDataArray(transaction.ID);
                        int flagUpdatePoStatusID = 0;

                        //c.GET TRANSACTIONDATA DETAIL / HS-IS
                        for (int i = 0; i < transactionDataArray.Count(); i++)
                        {
                            var DataDetailHS = transactionProvider.GetTransactionDataDetailHSS02005New(transactionDataArray[i].ID);
                            var DataDetailIS = transactionProvider.GetTransactionDataDetailISS02005New(transactionDataArray[i].ID);
                            
                            for (int j = 0; j < DataDetailHS.Count; j++)
                            {
                                transactionDataDetailHS.Add(DataDetailHS[j]);
                                //masukan ke array
                                arrHSIS = new List<string>();
                                arrHSIS.Add(transactionProvider.ConcateStringHSS02005New(DataDetailHS[j]));

                                for (int k = 0; k < DataDetailIS.Count; k++)
                                {
                                    if (DataDetailHS[j].GroupingCode == DataDetailIS[k].GroupingCode)
                                    {
                                        transactionDataDetailIS.Add(DataDetailIS[k]);
                                        //masukan ke array
                                        arrHSIS.Add(transactionProvider.ConcateStringISS02005New(DataDetailIS[k]));
                                    }
                                }
                                //masukan ke transactionDataArray.
                                transactionDataArray[i].Data = arrHSIS.ToArray();
                                transactionDataArray[i].DataLength = arrHSIS.Count;
                            }
                        }
                        //6.SEND TO WEB SERVICE
                        using (wsB2B.B2BAIWebServiceDMZ wsB2B = new wsB2B.B2BAIWebServiceDMZ())
                        {
                            wsB2B.UploadRequest uploadRequest = new wsB2B.UploadRequest();
                            var lastTicketNo = transactionProvider.GetLastTicketNo(fileType);
                            uploadRequest.TicketNo = lastTicketNo; //from session ticketNo login
                            uploadRequest.ClientTag = Resources.ClientTag;
                            uploadRequest.transactionData = transactionDataArray;

                            //WebProxy myProxy = new WebProxy(Resources.WebProxyAddress, true);
                            //myProxy.Credentials = new NetworkCredential(Resources.NetworkCredentialUserName, Resources.NetworkCredentialPassword, Resources.NetworkCredentialProxy);

                            //wsB2B.Proxy = myProxy;

                            var wsResult = wsB2B.UploadDocument(uploadRequest);
                            acknowledge = wsResult.Acknowledge;
                            ticketNo = wsResult.TicketNo;
                            message = wsResult.Message;
                            if (wsResult.Acknowledge && wsResult.Message.Contains("Succeeded")) flagUpdatePoStatusID = 1;
                        }

                        //NEW FOR LIVE 2013-NOV-20
                        if (flagUpdatePoStatusID == 1)
                        {
                            for (int i = 0; i < transactionDataArray.Count(); i++)
                            {
                                for (int j = 1; j <= transactionDataArray[i].Data.Count() - 1; j++)
                                {
                                    string poNumber = transactionDataArray[i].Data[j].Split('|')[10].Trim();
                                    
                                    transactionProvider.UpdateCustomPOPaymentStatusTerkirim(poNumber, 1);
                                }
                            }
                        }
                    }
                }
                else if (intResult == 0)
                {
                    //delete temp table 
                    transactionProvider.DeleteAllTempHSISS02005();
                    acknowledge = false;
                    ticketNo = "";
                    message = "No Data Upload.";
                }

                LblResult.Text = "Service Result = ";
                LblAcknowledge.Text = "Acknowledge : " + acknowledge;
                LblTicketNo.Text = "TicketNo : " + ticketNo;
                LblMessage.Text = "Message :" + message;

                //logevent login succeded
                logEvent.WriteDBLog("B2BAIWebServiceDMZ", "UploadDocumentS02005", acknowledge, ticketNo, message, fileType, "SERA");
                
            }
            catch (Exception ex)
            {
                LblResult.Text = ex.Message;
                LblAcknowledge.Text = "";
                LblTicketNo.Text = "";
                LblMessage.Text = "";

                //logevent login failed
                logEvent.WriteDBLog("B2BAIWebServiceDMZ", "UploadDocumentS02005", acknowledge, ticketNo, "webservice message : " + message + ". exception message : " + ex.Message, fileType, "SERA");
                // add taks kill when failed : by fhi 04.06.2014
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02005.exe");
                //end
            }
        }

        //add by fhi 03.12.2014 : setting untuk log
        public static void feedback(string ponumber, string message)
        {
            if (ConfigurationManager.AppSettings["AppLogger"].ToLower() != "on") return;
            string logErr = string.Format("{0}\\Error.log", ConfigurationManager.AppSettings["LogErrorFolder"]);

            StreamWriter tx = new StreamWriter(logErr, true, Encoding.UTF8);
            tx.WriteLine(string.Format("{0} {1} {2}", DateTime.Now, ponumber, message));
            tx.Close();
            GC.Collect();
        }

        private void sendEmail(string logInfo)
        {
            FileInfo info = new FileInfo(logInfo);

            var streams = info.OpenText();
            string ErrMessage = streams.ReadToEnd();
            streams.Close();

            string templete = "Dear Bapak/Ibu \r\n \r\n" + "Berikut ini PO Number yang ACTUALRECEIVEDINV null : \r\n" + ErrMessage + "\r\n\r\n terima kasih\r\nNB: Email ini dikirim otomatis oleh sistem, mohon untuk tidak membalas email ini. ";

            if (info.Length > 0)
            {
                try
                {
                    MailMessage message = new MailMessage();
                    SmtpClient smtpC = new SmtpClient();
                    message.From = new MailAddress("no-reply@sera.astra.co.id");
                    message.To.Add("is10@trac.astra.co.id");
                    message.Subject = "Error Log : ";
                    //message.IsBodyHtml = true;
                    message.Body = templete;

                    // smtpC.Port = 25;
                    // smtpC.Host = "webmail.sera.astra.co.id";
                    //  smtpC.UseDefaultCredentials = true;
                    // smtpC.DeliveryMethod = SmtpDeliveryMethod.Network;

                    //smtpC.Send(message);
                    //MessageBox.Show("sent");
                    //GC.Collect();

                }
                catch (Exception ex)
                {
                    MessageBox.Show("err : " + ex.InnerException.Message);
                }
            }

            

        }

        //end
    }
}
