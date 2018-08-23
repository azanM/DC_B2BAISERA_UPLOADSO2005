using System;
using System.Collections.Generic;
using System.Linq;
//using B2BAISERA.Models.DataAccess;
//using B2BAISERA.Helper;
//using B2BAISERA.Logic;
using Microsoft.Practices.Unity;
using System.Text;
using System.Configuration;
using System.Data.SqlClient;
using System.Data.Common;
using B2BAISERA.Models.EFServer;
using B2BAISERA.Helper;
using System.Data.EntityClient;
using System.Data;
using B2BAISERA.wsB2B;
using System.Globalization;
using System.Diagnostics;
using B2BAISERA.Log;
using System.IO;

namespace B2BAISERA.Models.Providers
{
    public class TransactionProvider : DataAccessBase
    {

        //private string sconnStr = ConfigurationManager.ConnectionStrings["EProcEntities"].ConnectionString;
        //SqlConnection conn;
        //SqlCommand cmd;

        public string version;
        public TransactionProvider()
            : base()
        {
        }

        public TransactionProvider(EProcEntities context)
            : base(context)
        {
        }

        #region MAIN
        //B2BAISERAEntities ctx = new B2BAISERAEntities(Repository.ConnectionStringEF);

        //public User GetUser(string userName, string password, string clientTag)
        //{
        //    var User = (from o in ctx.Users
        //                where o.UserName == userName && o.Password == password && o.ClientTag == clientTag
        //                select o).FirstOrDefault();

        //    return User;
        //}

        public CUSTOM_USER GetUser(string userName, string password, string clientTag)
        {
            var user = (from o in entities.CUSTOM_USER
                        where o.UserName == userName && o.Password == password && o.ClientTag == clientTag
                        select o).FirstOrDefault();

            return user;
        }

        public string GetLastTicketNo(string fileType)
        {
            var result = "";
            try
            {
                var query = (entities.CUSTOM_LOG
                    .Where(log => (log.Acknowledge == true) && (log.FileType == fileType))
                    .Select(log => new LogViewModel
                    {
                        ID = log.ID,
                        WebServiceName = log.WebServiceName,
                        MethodName = log.MethodName,
                        TicketNo = log.TicketNo,
                        Message = log.Message
                    })
                    ).OrderByDescending(log => log.ID).FirstOrDefault();

                result = query != null ? Convert.ToString(query.TicketNo) : "";
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }
        #endregion

        #region UPLOAD
        
        #region S02005 HSIS New Update 25092013

        //add by fhi 28.11.2014 : penambahan feature send email notify bila ACTUALRECEIVEDINV is null/kosong
        public List<string> collectDataProposal()
        {
            List<string> listStr = new List<string>();            

            //var query = ((from o in entities.CUSTOM_S02005_TEMP_IS
            //             select new proposalModel()
            //             {
            //                 groupingCode = o.GroupingCode
            //             }).Select(x => x.groupingCode).Distinct()).ToList();

            //listStr = query;

            var query = ((from o in entities.CUSTOMPOes
                          join p in entities.CUSTOM_S02005_TEMP_IS on o.PROPOSALNUMBER equals p.GroupingCode
                          join q in entities.CUSTOMIRs on o.PONUMBER equals q.PONUMBER
                          where (q.ACTUALRECEIVEDINV == null)
                         select new ponumberModel()
                         {
                             poNumber=o.PONUMBER
                         }).Select(x => x.poNumber).Distinct()).ToList();

            listStr = query;
            return listStr;
        }

       

        //end

        public List<CUSTOM_S02005_TEMP_HS> PaymentSeraToAIHS()
        {
            LogEvent logEvent = new LogEvent();
            List<CUSTOM_S02005_TEMP_HS> listTempHS = new List<CUSTOM_S02005_TEMP_HS>();
            try
            {
                listTempHS = entities.sp_PaymentSeraToAI_HS().ToList();
                return listTempHS;
            }
            catch (Exception ex)
            {
                logEvent.WriteDBLog("", "UploadS02005_Load", false, "", ex.StackTrace, "S02005", "SERA");
                logEvent.WriteDBLog("", "UploadS02005_Load", false, "", ex.InnerException.Message, "S02005", "SERA");
                logEvent.WriteDBLog("", "UploadS02005_Load", false, "", ex.InnerException.StackTrace, "S02005", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02005.exe");
                throw ex;
            }
        }

        public List<CUSTOM_S02005_TEMP_IS> PaymentSeraToAIIS()
        {
            LogEvent logEvent = new LogEvent();
            List<CUSTOM_S02005_TEMP_IS> listTempIS = new List<CUSTOM_S02005_TEMP_IS>();
            try
            {
                listTempIS = entities.sp_PaymentSeraToAI_IS().ToList();
                return listTempIS;
            }
            catch (Exception ex)
            {
                logEvent.WriteDBLog("", "UploadS02005_Load", false, "", ex.StackTrace, "S02005", "SERA");
                logEvent.WriteDBLog("", "UploadS02005_Load", false, "", ex.InnerException.Message, "S02005", "SERA");
                logEvent.WriteDBLog("", "UploadS02005_Load", false, "", ex.InnerException.StackTrace, "S02005", "SERA");
                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02005.exe");
                throw ex;
            }
        }

        public int InsertLogTransactionS02005New(List<CUSTOM_S02005_TEMP_HS> listTempHS, List<CUSTOM_S02005_TEMP_IS> listTempIS)
        {
            int result = 0;
            try
            {
                if (listTempHS.Count > 0)
                {
                    //insert into CUSTOM_TRANSACTION
                    CUSTOM_TRANSACTION transaction = new CUSTOM_TRANSACTION();
                    transaction.TicketNo = "";
                    transaction.ClientTag = "";
                    EntityHelper.SetAuditForInsert(transaction, "SERA");
                    entities.CUSTOM_TRANSACTION.AddObject(transaction);

                    var countListTempHS = listTempHS.Count;
                    var countListTempIS = listTempIS.Count;

                    for (int i = 0; i < listTempHS.Count; i++)
                    {
                        //insert into CUSTOM_TRANSACTIONDATA
                        CUSTOM_TRANSACTIONDATA transactionData = new CUSTOM_TRANSACTIONDATA();
                        transactionData.CUSTOM_TRANSACTION = transaction;
                        transactionData.TransGUID = Guid.NewGuid().ToString();
                        transactionData.DocumentNumber = listTempHS[i].GroupingCode;
                        transactionData.FileType = "S02005";
                        transactionData.IPAddress = "118.97.80.12"; //IP ADDRESS KOMP SERVER, dan HARUS TERDAFTAR DI DB AI
                        transactionData.DestinationUser = "AI";
                        transactionData.Key1 = listTempHS[i].GroupingCode;
                        transactionData.Key2 = listTempHS[i].CompanyCodeAI;
                        transactionData.Key3 = "";
                        transactionData.DataLength = null;
                        transactionData.RowStatus = "";
                        EntityHelper.SetAuditForInsert(transactionData, "SERA");
                        entities.CUSTOM_TRANSACTIONDATA.AddObject(transactionData);

                        //CHECK IF DATA HS BY GROUPINGCODE SUDAH ADA, DELETE DULU BY ID, supaya tidak redundant ponumber nya
                        var groupingCode = listTempHS[i].GroupingCode;
                        var query = (from o in entities.CUSTOM_S02005_HS
                                     where o.GroupingCode == groupingCode
                                     select o).ToList();
                        if (query.Count > 0)
                        {
                            for (int d = 0; d < query.Count; d++)
                            {
                                //delete
                                var delID = query[d].ID;
                                CUSTOM_S02005_HS delHS = entities.CUSTOM_S02005_HS.Single(o => o.ID == delID);
                                entities.CUSTOM_S02005_HS.DeleteObject(delHS);
                            }
                        }

                        //insert into CUSTOM_S02005_HS
                        CUSTOM_S02005_HS DataDetailHS = new CUSTOM_S02005_HS();
                        DataDetailHS.CUSTOM_TRANSACTIONDATA = transactionData;
                        DataDetailHS.GroupingCode = listTempHS[i].GroupingCode;
                        DataDetailHS.PaymentDate = listTempHS[i].PaymentDate;
                        DataDetailHS.TotalPayment = listTempHS[i].TotalPayment;
                        //add by fhi 08082014
                        DataDetailHS.version = listTempHS[i].version;

                        //start add identitas penambahan row CUSTOM_S02005_HS : by fhi 05.06.2014
                        DataDetailHS.dibuatOleh = "system";
                        DataDetailHS.dibuatTanggal = DateTime.Now;
                        DataDetailHS.diubahOleh = "system";
                        DataDetailHS.diubahTanggal = DateTime.Now;
                        //end

                        entities.CUSTOM_S02005_HS.AddObject(DataDetailHS);

                        //build HS separator
                        var strHS = ConcateStringHSS02005New(listTempHS[i]);

                        //insert into CUSTOM_TRANSACTIONDATADETAIL for HS
                        CUSTOM_TRANSACTIONDATADETAIL transactionDataDetail = new CUSTOM_TRANSACTIONDATADETAIL();
                        transactionDataDetail.CUSTOM_TRANSACTIONDATA = transactionData;
                        transactionDataDetail.Data = strHS;

                        //start add identitas penambahan row CUSTOM_TRANSACTIONDATADETAIL : by fhi 05.06.2014
                        transactionDataDetail.dibuatOleh = "system";
                        transactionDataDetail.dibuatTanggal = DateTime.Now;
                        transactionDataDetail.diubahOleh = "system";
                        transactionDataDetail.diubahTanggal = DateTime.Now;
                        //end

                        entities.CUSTOM_TRANSACTIONDATADETAIL.AddObject(transactionDataDetail);

                        if (listTempIS != null)
                        {
                            for (int j = 0; j < countListTempIS; j++)
                            {
                                if (listTempIS[j].GroupingCode == listTempHS[i].GroupingCode)
                                {
                                    //CHECK IF DATA IS BY PONUMBER SUDAH ADA, DELETE DULU BY ID
                                    var poNumbIS = listTempIS[j].PONumberSERA;
                                    var queryIS = (from o in entities.CUSTOM_S02005_IS
                                                   where o.PONumberSERA == poNumbIS
                                                   select o).ToList();
                                    if (queryIS.Count > 0)
                                    {
                                        for (int d = 0; d < queryIS.Count; d++)
                                        {
                                            //delete
                                            var delIDIS = queryIS[d].ID;
                                            CUSTOM_S02005_IS delIS = entities.CUSTOM_S02005_IS.Single(o => o.ID == delIDIS);
                                            entities.CUSTOM_S02005_IS.DeleteObject(delIS);
                                        }
                                    }

                                    //insert into CUSTOM_S02005_IS
                                    CUSTOM_S02005_IS DataDetailIS = new CUSTOM_S02005_IS();
                                    DataDetailIS.CUSTOM_TRANSACTIONDATA = transactionData;
                                    DataDetailIS.GroupingCode = listTempIS[j].GroupingCode;
                                    DataDetailIS.BillingNo = listTempIS[j].BillingNo;
                                    DataDetailIS.KuitansiNo = listTempIS[j].KuitansiNo;
                                    DataDetailIS.CurrencyCode = listTempIS[j].CurrencyCode;
                                    DataDetailIS.BusinessAreaCode = listTempIS[j].BusinessAreaCode;
                                    DataDetailIS.CustomerNo = listTempIS[j].CustomerNo;
                                    DataDetailIS.SpesNumber = listTempIS[j].SpesNumber;
                                    DataDetailIS.SONumber = listTempIS[j].SONumber;
                                    DataDetailIS.Salesman = listTempIS[j].Salesman;
                                    DataDetailIS.ChasisNumber = listTempIS[j].ChasisNumber;
                                    DataDetailIS.PONumberSERA = listTempIS[j].PONumberSERA;
                                    DataDetailIS.VersionPOSERA = listTempIS[j].VersionPOSERA;
                                    DataDetailIS.PaymentAmountDC = listTempIS[j].PaymentAmountDC;
                                    DataDetailIS.PaymentAmountLC = listTempIS[j].PaymentAmountLC;
                                    
                                    //add by fhi 08082014
                                    DataDetailIS.paymentDate=listTempIS[j].paymentDate;
                                    //DataDetailIS.version = listTempIS[j].version;
                                    DataDetailIS.version = listTempIS[j].version == null ? 1 : listTempIS[j].version;


                                    //start add identitas penambahan row CUSTOM_S02005_IS : by fhi 05.06.2014
                                    DataDetailIS.dibuatOleh = "system";
                                    DataDetailIS.dibuatTanggal = DateTime.Now;
                                    DataDetailIS.diubahOleh = "system";
                                    DataDetailIS.diubahTanggal = DateTime.Now;
                                    //end

                                    entities.CUSTOM_S02005_IS.AddObject(DataDetailIS);

                                    //build IS separator
                                    var strIS = ConcateStringISS02005New(listTempIS[j]);

                                    //insert into CUSTOM_TRANSACTIONDATADETAIL for IS
                                    transactionDataDetail = new CUSTOM_TRANSACTIONDATADETAIL();
                                    transactionDataDetail.CUSTOM_TRANSACTIONDATA = transactionData;
                                    transactionDataDetail.Data = strIS;

                                    //start add identitas penambahan row CUSTOM_TRANSACTIONDATADETAIL : by fhi 05.06.2014
                                    transactionDataDetail.dibuatOleh = "system";
                                    transactionDataDetail.dibuatTanggal = DateTime.Now;
                                    transactionDataDetail.diubahOleh = "system";
                                    transactionDataDetail.diubahTanggal = DateTime.Now;
                                    //end

                                    entities.CUSTOM_TRANSACTIONDATADETAIL.AddObject(transactionDataDetail);
                                }
                            }
                        }
                    }
                    entities.SaveChanges();
                    //todo: delete temp table
                    int feedbackDelete = DeleteAllTempHSISS02005();
                    result = feedbackDelete;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return result;
        }

        public TransactionViewModel GetTransactionS02005New()
        {
            TransactionViewModel transaction = null;
            try
            {
                DateTime dateNow = DateTime.Now.Date;
                transaction = (from h in entities.CUSTOM_TRANSACTION
                               join d in entities.CUSTOM_TRANSACTIONDATA
                               on h.ID equals d.TransactionID
                               where d.FileType == "S02005" && h.CreatedWhen >= dateNow
                               select new TransactionViewModel()
                               {
                                   ID = h.ID
                               }).OrderByDescending(z => z.ID).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return transaction;
        }

        public List<S02005HSNewViewModel> GetTransactionDataDetailHSS02005New(int? transactionDataID)
        {
            List<S02005HSNewViewModel> dataHS = null;
            try
            {
                dataHS = (entities.CUSTOM_S02005_HS
                          .Where(o => o.TransactionDataID == transactionDataID)
                          .Select(o => new S02005HSNewViewModel
                          {
                              ID = o.ID,
                              TransactionDataID = o.TransactionDataID == null ? null : o.TransactionDataID,
                              GroupingCode = o.GroupingCode,
                              PaymentDate = o.PaymentDate,
                              TotalPayment = o.TotalPayment,
                              //add by fhi 08082014
                              version = o.version,
                          }).ToList());
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dataHS;
        }

        public List<S02005ISNewViewModel> GetTransactionDataDetailISS02005New(int? transactionDataID)
        {
            List<S02005ISNewViewModel> dataIS = null;
            try
            {
                dataIS = (entities.CUSTOM_S02005_IS
                          .Where(o => o.TransactionDataID == transactionDataID)
                          .Select(o => new S02005ISNewViewModel
                          {
                              ID = o.ID,
                              HSID = (int)(!o.TransactionDataID.HasValue ? 0 : o.TransactionDataID),
                              GroupingCode = o.GroupingCode,
                              BillingNo = o.BillingNo,
                              KuitansiNo = o.KuitansiNo,
                              CurrencyCode = o.CurrencyCode,
                              BusinessAreaCode = o.BusinessAreaCode,
                              CustomerNo = o.CustomerNo,
                              SpesNumber = o.SpesNumber,
                              SONumber = o.SONumber,
                              Salesman = o.Salesman,
                              ChasisNumber = o.ChasisNumber,
                              PONumberSERA = o.PONumberSERA,
                              VersionPOSERA = o.VersionPOSERA,
                              PaymentAmountDC = o.PaymentAmountDC,
                              PaymentAmountLC = o.PaymentAmountLC,

                              //add by fhi 08082014
                              PaymentDate=o.paymentDate,
                              Version=o.version
                          }).ToList());
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return dataIS;
        }

        public wsB2B.TransactionData[] GetTransactionDataArray(int? transactionID)
        {
            wsB2B.TransactionData[] transactionData = null;
            try
            {
                transactionData = (from o in entities.CUSTOM_TRANSACTIONDATA
                                   //join p in entities.CUSTOM_S02001_HS
                                   //on o.ID equals p.TransactionDataID
                                   //join q in entities.CUSTOM_S02001_IS
                                   //on o.ID equals q.TransactionDataID
                                   //join s in entities.CUSTOM_TRANSACTIONDATADETAIL
                                   //on o.ID equals s.TransactionDataID
                                   where o.TransactionID == transactionID
                                   select new wsB2B.TransactionData
                                   {
                                       ID = o.ID,
                                       TransGUID = o.TransGUID,
                                       DocumentNumber = o.DocumentNumber,
                                       FileType = o.FileType,
                                       IPAddress = o.IPAddress,
                                       DestinationUser = o.DestinationUser,
                                       Key1 = o.Key1,
                                       Key2 = o.Key2,
                                       Key3 = o.Key3,
                                       //DataLength = 
                                       //Data = ConcateStringHS()//new ArrayOfString { s.Data, "","" }
                                   }).ToArray();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return transactionData;
        }

        public string ConcateStringHSS02005New(S02005HSNewViewModel HS)
        {
            StringBuilder strHS = new StringBuilder(1000);
            strHS.Append("HS|");
            strHS.Append(HS.GroupingCode);
            strHS.Append("|");
            strHS.Append(HS.PaymentDate == null ? "19000101" : String.Format("{0:yyyyMMdd}", HS.PaymentDate));
            strHS.Append("|");
            strHS.Append(HS.version);
            strHS.Append("|");
            strHS.Append(HS.TotalPayment);

            return strHS.ToString();
        }

        public string ConcateStringHSS02005New(CUSTOM_S02005_TEMP_HS HS)
        {
            StringBuilder strHS = new StringBuilder(1000);
            strHS.Append("HS|");
            //strHS.Append("S02005");
            //strHS.Append("|");
            strHS.Append(HS.GroupingCode);
            strHS.Append("|");
            strHS.Append(HS.PaymentDate == null ? "19000101" : String.Format("{0:yyyyMMdd}", HS.PaymentDate));
            //add by fhi 08082014
            strHS.Append("|");
            strHS.Append(HS.version);
            //end
            strHS.Append("|");
            strHS.Append(HS.TotalPayment);
           

            return strHS.ToString();
        }

        public string ConcateStringISS02005New(S02005ISNewViewModel IS)
        {
            StringBuilder strIS = new StringBuilder(1000);
            strIS.Append("IS|");
            strIS.Append(IS.GroupingCode);
            strIS.Append("|");
            //add by fhi 11082014            
            strIS.Append(IS.PaymentDate == null ? "19000101" : string.Format("{0:yyyyMMdd}", IS.PaymentDate));
            strIS.Append("|");
            strIS.Append(IS.Version);
            strIS.Append("|");
            //end
            strIS.Append(IS.BillingNo);
            strIS.Append("|");
            strIS.Append(IS.KuitansiNo);
            strIS.Append("|");
            strIS.Append(IS.CurrencyCode);
            strIS.Append("|");
            strIS.Append(IS.BusinessAreaCode);
            strIS.Append("|");
            strIS.Append(IS.CustomerNo);
            strIS.Append("|");
            strIS.Append(IS.SpesNumber);
            strIS.Append("|");
            strIS.Append(IS.SONumber);
            strIS.Append("|");
            strIS.Append(IS.Salesman);
            strIS.Append("|");
            strIS.Append(IS.ChasisNumber);
            strIS.Append("|");
            strIS.Append(IS.PONumberSERA);
            strIS.Append("|");
            strIS.Append(IS.VersionPOSERA);
            strIS.Append("|");
            strIS.Append(IS.PaymentAmountDC);
            strIS.Append("|");
            strIS.Append(IS.PaymentAmountLC);

           

            return strIS.ToString();
        }

        public string ConcateStringISS02005New(CUSTOM_S02005_TEMP_IS IS)
        {
            StringBuilder strIS = new StringBuilder(1000);
            strIS.Append("IS|");
            strIS.Append(IS.GroupingCode);
            strIS.Append("|");
            //add by fhi 08082014
            strIS.Append(IS.paymentDate == null ? "19000101" : string.Format("{0:yyyyMMdd}", IS.paymentDate));
            strIS.Append("|");
            strIS.Append(IS.version);
            strIS.Append("|");
            //end
            strIS.Append(IS.BillingNo);
            strIS.Append("|");
            strIS.Append(IS.KuitansiNo);
            strIS.Append("|");
            strIS.Append(IS.CurrencyCode);
            strIS.Append("|");
            strIS.Append(IS.BusinessAreaCode);
            strIS.Append("|");
            strIS.Append(IS.CustomerNo);
            strIS.Append("|");
            strIS.Append(IS.SpesNumber);
            strIS.Append("|");
            strIS.Append(IS.SONumber);
            strIS.Append("|");
            strIS.Append(IS.Salesman);
            strIS.Append("|");
            strIS.Append(IS.ChasisNumber);
            strIS.Append("|");
            strIS.Append(IS.PONumberSERA);
            strIS.Append("|");
            strIS.Append(IS.VersionPOSERA);
            strIS.Append("|");
            strIS.Append(IS.PaymentAmountDC);
            strIS.Append("|");
            strIS.Append(IS.PaymentAmountLC);

            return strIS.ToString();
        }

        public List<CUSTOM_S02005_TEMP_HS> CheckingHistoryHSIS(List<CUSTOM_S02005_TEMP_HS> tempHS, List<CUSTOM_S02005_TEMP_IS> tempIS)
        {
            LogEvent logEvent = new LogEvent();
            List<CUSTOM_S02005_TEMP_HS> listDataHS = new List<CUSTOM_S02005_TEMP_HS>();
            List<CUSTOM_S02005_TEMP_IS> listDataIS = new List<CUSTOM_S02005_TEMP_IS>();
            try
            {
                if (tempHS.Count > 0)
                {
                    listDataHS = tempHS;
                    //select data antara CUSTOM_S02005_HS dan tempHS
                    var existingRowHS = (from o in tempHS
                                         where entities.CUSTOM_S02005_HS.Any(e => o.GroupingCode == e.GroupingCode)
                                         select new S02005HSNewViewModel
                                         {
                                             GroupingCode = o.GroupingCode,
                                             PaymentDate = o.PaymentDate,
                                             TotalPayment = o.TotalPayment,
                                             //add by fhi 08082014
                                             version = o.version,
                                             CompanyCodeAI = o.CompanyCodeAI

                                         }).ToList();

                    //check data hasil dari existingRowHS by groupingcode
                    for (int i = 0; i < existingRowHS.Count; i++)
                    {
                        //check per row data hasil dari existingRowHS by groupingcode
                        string existGroupingCode = existingRowHS[i].GroupingCode;
                        var q = (from o in entities.CUSTOM_S02005_HS
                                 where o.GroupingCode == existGroupingCode
                                 select new S02005HSNewViewModel
                                 {
                                     GroupingCode = o.GroupingCode,
                                     PaymentDate = o.PaymentDate,
                                     TotalPayment = o.TotalPayment,
                                     //add by fhi 08082014
                                     version = o.version
                                 }).SingleOrDefault();

                        //compare data hasil check data antara existingrowhs dengan hasil query by groupingcode 
                        var compareHS = EqualS02005HS(existingRowHS[i], q);

                        //jika HS ada perubahan
                        if (!compareHS)
                        {

                            //b.cek IS
                            if (tempIS.Count > 0)
                            {
                                listDataIS = tempIS;
                                //select data antara CUSTOM_S02005_is dan tempIS
                                var existingRowIS = (from o in tempIS
                                                     where entities.CUSTOM_S02005_IS.Any(e => o.GroupingCode == e.GroupingCode)
                                                         && o.GroupingCode == existGroupingCode
                                                     select new S02005ISNewViewModel
                                                     {
                                                         GroupingCode = o.GroupingCode,
                                                         BillingNo = o.BillingNo,
                                                         KuitansiNo = o.KuitansiNo,
                                                         CurrencyCode = o.CurrencyCode,
                                                         BusinessAreaCode = o.BusinessAreaCode,
                                                         CustomerNo = o.CustomerNo,
                                                         SpesNumber = o.SpesNumber,
                                                         SONumber = o.SONumber,
                                                         Salesman = o.Salesman,
                                                         ChasisNumber = o.ChasisNumber,
                                                         PONumberSERA = o.PONumberSERA,
                                                         VersionPOSERA = o.VersionPOSERA,
                                                         PaymentAmountDC = o.PaymentAmountDC,
                                                         PaymentAmountLC = o.PaymentAmountLC,
                                                         //add by fhi 11082014
                                                         PaymentDate = o.paymentDate,
                                                         Version=o.version,
                                                         Flags = o.flags,   
                                                         CompanyCodeAI = o.CompanyCodeAI
                                                     }).ToList();

                                //check data hasil dari existingRowIS by groupingcode
                                for (int j = 0; j < existingRowIS.Count; j++)
                                {
                                    if (existingRowIS[j].GroupingCode == existingRowHS[i].GroupingCode)
                                    {
                                        string existPoNumberIS = existingRowIS[j].PONumberSERA;
                                        versionPO((existPoNumberIS));

                                        var zVer = (from o in entities.CUSTOM_S02005_IS
                                                 where o.PONumberSERA == existPoNumberIS
                                                 select new S02005ISNewViewModel
                                                 {
                                                     GroupingCode = o.GroupingCode,
                                                     BillingNo = o.BillingNo,
                                                     KuitansiNo = o.KuitansiNo,
                                                     CurrencyCode = o.CurrencyCode,
                                                     BusinessAreaCode = o.BusinessAreaCode,
                                                     CustomerNo = o.CustomerNo,
                                                     SpesNumber = o.SpesNumber,
                                                     SONumber = o.SONumber,
                                                     Salesman = o.Salesman,
                                                     ChasisNumber = o.ChasisNumber,
                                                     PONumberSERA = o.PONumberSERA,
                                                     VersionPOSERA = o.VersionPOSERA,
                                                     PaymentAmountDC = o.PaymentAmountDC,
                                                     PaymentAmountLC = o.PaymentAmountLC,
                                                     //add by fhi 11082014
                                                     PaymentDate = o.paymentDate,
                                                     Version = o.version
                                                 }).SingleOrDefault();

                                        //compare data hasil check data antara existingrowIS dengan hasil query by groupingcode 
                                        var compareIS = EqualS02005IS(existingRowIS[j], zVer);



                                        //jika HS ada perubahan dan IS ada perubahan, NEW SIT:update HS-IS
                                        if (!compareIS)
                                        {
                                            //NEW SIT
                                            //a.update list HS
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.GroupingCode == existingRowHS[i].GroupingCode
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02005_TEMP_HS row = new CUSTOM_S02005_TEMP_HS();
                                            row.GroupingCode = existingRowHS[i].GroupingCode;
                                            row.PaymentDate = existingRowHS[i].PaymentDate;
                                            row.TotalPayment = existingRowHS[i].TotalPayment != null ? existingRowHS[i].TotalPayment : 0;

                                            //add by fhi 08082014
                                            row.version = q.version != null ? q.version + 1 : existingRowHS[i].version != null ? existingRowHS[i].version : 1;
                                            row.CompanyCodeAI = existingRowHS[i].CompanyCodeAI;

                                            listDataHS.Add(row);
                                            //END NEW SIT

                                            //update list IS
                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumberSERA == existingRowIS[j].PONumberSERA
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02005_TEMP_IS rowIS = new CUSTOM_S02005_TEMP_IS();
                                            rowIS.GroupingCode = existingRowIS[j].GroupingCode;
                                            rowIS.BillingNo = existingRowIS[j].BillingNo;
                                            rowIS.KuitansiNo = existingRowIS[j].KuitansiNo;
                                            rowIS.CurrencyCode = existingRowIS[j].CurrencyCode;
                                            rowIS.BusinessAreaCode = existingRowIS[j].BusinessAreaCode;
                                            rowIS.CustomerNo = existingRowIS[j].CustomerNo;
                                            rowIS.SpesNumber = existingRowIS[j].SpesNumber;
                                            rowIS.SONumber = existingRowIS[j].SONumber;
                                            rowIS.Salesman = existingRowIS[j].Salesman;
                                            rowIS.ChasisNumber = existingRowIS[j].ChasisNumber;
                                            rowIS.PONumberSERA = existingRowIS[j].PONumberSERA;
                                            rowIS.VersionPOSERA = Convert.ToInt32(version);
                                            //rowIS.VersionPOSERA = z.VersionPOSERA != null ? z.VersionPOSERA + 1 : existingRowIS[j].VersionPOSERA != null ? existingRowIS[j].VersionPOSERA : 1;
                                            rowIS.PaymentAmountDC = existingRowIS[j].PaymentAmountDC;
                                            rowIS.PaymentAmountLC = existingRowIS[j].PaymentAmountLC;

                                            //add by fhi 11082014
                                            rowIS.paymentDate = existingRowIS[j].PaymentDate;
                                            rowIS.version = zVer != null && zVer.Version != null ? zVer.Version + 1 : existingRowIS[j].Version != null ? existingRowIS[j].Version : 1;
                                            rowIS.CompanyCodeAI = existingRowIS[i].CompanyCodeAI;

                                            listDataIS.Add(rowIS);

                                            
                                        }
                                        //jika HS ada perubahan dan IS ga ada perubahan, NEW SIT:update HS-IS
                                        else if (compareIS)
                                        {
                                            //NEW SIT
                                            //a.update list HS
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.GroupingCode == existingRowHS[i].GroupingCode
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02005_TEMP_HS row = new CUSTOM_S02005_TEMP_HS();
                                            row.GroupingCode = existingRowHS[i].GroupingCode;
                                            row.PaymentDate = existingRowHS[i].PaymentDate;
                                            row.TotalPayment = existingRowHS[i].TotalPayment != null ? existingRowHS[i].TotalPayment : 0;
                                            //add by fhi 08082014
                                            row.version = q.version != null ? q.version + 1 : existingRowHS[i].version != null ? existingRowHS[i].version : 1;
                                            row.CompanyCodeAI = existingRowHS[i].CompanyCodeAI;
                                            listDataHS.Add(row);

                                            //update list IS
                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumberSERA == existingRowIS[j].PONumberSERA
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02005_TEMP_IS rowIS = new CUSTOM_S02005_TEMP_IS();
                                            rowIS.GroupingCode = existingRowIS[j].GroupingCode;
                                            rowIS.BillingNo = existingRowIS[j].BillingNo;
                                            rowIS.KuitansiNo = existingRowIS[j].KuitansiNo;
                                            rowIS.CurrencyCode = existingRowIS[j].CurrencyCode;
                                            rowIS.BusinessAreaCode = existingRowIS[j].BusinessAreaCode;
                                            rowIS.CustomerNo = existingRowIS[j].CustomerNo;
                                            rowIS.SpesNumber = existingRowIS[j].SpesNumber;
                                            rowIS.SONumber = existingRowIS[j].SONumber;
                                            rowIS.Salesman = existingRowIS[j].Salesman;
                                            rowIS.ChasisNumber = existingRowIS[j].ChasisNumber;
                                            rowIS.PONumberSERA = existingRowIS[j].PONumberSERA;
                                            rowIS.PONumberSERA = existingRowIS[j].PONumberSERA;
                                            rowIS.VersionPOSERA = Convert.ToInt32(version);
                                            //rowIS.VersionPOSERA = z.VersionPOSERA != null ? z.VersionPOSERA + 1 : existingRowIS[j].VersionPOSERA != null ? existingRowIS[j].VersionPOSERA : 1;
                                            rowIS.PaymentAmountDC = existingRowIS[j].PaymentAmountDC;
                                            rowIS.PaymentAmountLC = existingRowIS[j].PaymentAmountLC;

                                            //add by fhi 11082014
                                            rowIS.paymentDate = existingRowIS[j].PaymentDate;
                                            rowIS.version = zVer != null && zVer.Version != null ? zVer.Version + 1 : existingRowIS[j].Version != null ? existingRowIS[j].Version : 1;
                                            rowIS.CompanyCodeAI = existingRowIS[i].CompanyCodeAI;

                                            listDataIS.Add(rowIS);
                                            //END NEW SIT
                                        }
                                    }
                                }
                            }
                        }
                        //jika HS ga ada perubahan
                        else if (compareHS)
                        {
                            //a.cek IS
                            if (tempIS.Count > 0)
                            {
                                int flagChangeIS = 0;
                                List<string> listRowISDelete = new List<string>();
                                listDataIS = tempIS;
                                var existingRowIS = (from o in tempIS
                                                     where entities.CUSTOM_S02005_IS.Any(e => o.GroupingCode == e.GroupingCode)
                                                        && o.GroupingCode == existGroupingCode
                                                     select new S02005ISNewViewModel
                                                     {
                                                         GroupingCode = o.GroupingCode,
                                                         BillingNo = o.BillingNo,
                                                         KuitansiNo = o.KuitansiNo,
                                                         CurrencyCode = o.CurrencyCode,
                                                         BusinessAreaCode = o.BusinessAreaCode,
                                                         CustomerNo = o.CustomerNo,
                                                         SpesNumber = o.SpesNumber,
                                                         SONumber = o.SONumber,
                                                         Salesman = o.Salesman,
                                                         ChasisNumber = o.ChasisNumber,
                                                         PONumberSERA = o.PONumberSERA,
                                                         VersionPOSERA = o.VersionPOSERA,
                                                         PaymentAmountDC = o.PaymentAmountDC,
                                                         PaymentAmountLC = o.PaymentAmountLC,

                                                         //add by fhi 11082014
                                                         PaymentDate=o.paymentDate,
                                                         Version=o.version,
                                                         Flags=o.flags,
                                                         CompanyCodeAI=o.CompanyCodeAI
                                                     }).ToList();
                                for (int j = 0; j < existingRowIS.Count; j++)
                                {
                                    if (existingRowIS[j].GroupingCode == existingRowHS[i].GroupingCode)
                                    {
                                        string existPoNumberIS = existingRowIS[j].PONumberSERA;
                                        versionPO((existPoNumberIS));
                                        var zVer = (from o in entities.CUSTOM_S02005_IS
                                                 where o.PONumberSERA == existPoNumberIS
                                                 select new S02005ISNewViewModel
                                                 {
                                                     GroupingCode = o.GroupingCode,
                                                     BillingNo = o.BillingNo,
                                                     KuitansiNo = o.KuitansiNo,
                                                     CurrencyCode = o.CurrencyCode,
                                                     BusinessAreaCode = o.BusinessAreaCode,
                                                     CustomerNo = o.CustomerNo,
                                                     SpesNumber = o.SpesNumber,
                                                     SONumber = o.SONumber,
                                                     Salesman = o.Salesman,
                                                     ChasisNumber = o.ChasisNumber,
                                                     PONumberSERA = o.PONumberSERA,
                                                     VersionPOSERA = o.VersionPOSERA,
                                                     PaymentAmountDC = o.PaymentAmountDC,
                                                     PaymentAmountLC = o.PaymentAmountLC,

                                                     //add by fhi 11082014
                                                     PaymentDate = o.paymentDate,
                                                     Version = o.version
                                                 }).SingleOrDefault();
                                        var compareIS = EqualS02005IS(existingRowIS[j], zVer);

                                        //jika HS ga ada perubahan dan IS ada perubahan, NEW SIT: Update HS-IS
                                        if (!compareIS)
                                        {
                                            flagChangeIS = 1;

                                            //NEW SIT
                                            //a.update list HS
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.GroupingCode == existingRowHS[i].GroupingCode
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02005_TEMP_HS row = new CUSTOM_S02005_TEMP_HS();
                                            row.GroupingCode = existingRowHS[i].GroupingCode;
                                            row.PaymentDate = existingRowHS[i].PaymentDate;
                                            row.TotalPayment = existingRowHS[i].TotalPayment != null ? existingRowHS[i].TotalPayment : 0;
                                            //add by fhi 08082014
                                            row.version = q.version != null ? q.version + 1 : existingRowHS[i].version != null ? existingRowHS[i].version : 1;
                                            row.CompanyCodeAI = existingRowHS[i].CompanyCodeAI;

                                            listDataHS.Add(row);
                                            //END NEW SIT
                                            //update list IS
                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumberSERA == existingRowIS[j].PONumberSERA
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02005_TEMP_IS rowIS = new CUSTOM_S02005_TEMP_IS();
                                            rowIS.GroupingCode = existingRowIS[j].GroupingCode;
                                            rowIS.BillingNo = existingRowIS[j].BillingNo;
                                            rowIS.KuitansiNo = existingRowIS[j].KuitansiNo;
                                            rowIS.CurrencyCode = existingRowIS[j].CurrencyCode;
                                            rowIS.BusinessAreaCode = existingRowIS[j].BusinessAreaCode;
                                            rowIS.CustomerNo = existingRowIS[j].CustomerNo;
                                            rowIS.SpesNumber = existingRowIS[j].SpesNumber;
                                            rowIS.SONumber = existingRowIS[j].SONumber;
                                            rowIS.Salesman = existingRowIS[j].Salesman;
                                            rowIS.ChasisNumber = existingRowIS[j].ChasisNumber;
                                            rowIS.PONumberSERA = existingRowIS[j].PONumberSERA;
                                            rowIS.VersionPOSERA = Convert.ToInt32(version);
                                            //rowIS.VersionPOSERA = z.VersionPOSERA != null ? z.VersionPOSERA + 1 : existingRowIS[j].VersionPOSERA != null ? existingRowIS[j].VersionPOSERA : 1;
                                            rowIS.PaymentAmountDC = existingRowIS[j].PaymentAmountDC;
                                            rowIS.PaymentAmountLC = existingRowIS[j].PaymentAmountLC;

                                            //add by fhi 11082014
                                            rowIS.paymentDate = existingRowIS[j].PaymentDate;
                                            rowIS.version = zVer != null && zVer.Version != null ? zVer.Version + 1 : existingRowIS[j].Version != null ? existingRowIS[j].Version : 1;
                                            rowIS.CompanyCodeAI = existingRowIS[i].CompanyCodeAI;
                                            listDataIS.Add(rowIS);                                            
                                        }
                                        //jika HS ga ada perubahan dan IS ga ada perubahan
                                        else if (compareIS)
                                        {
                                            ////do nothing
                                            ////ADD LIST GROUPINGCODE+PONUMBER
                                            ////List<string> listRowISDelete = new List<string>();
                                            //listRowISDelete.Add(existingRowIS[j].GroupingCode + "|" + existingRowIS[j].PONumberSERA);

                                            //NEW SIT
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.GroupingCode == existingRowHS[i].GroupingCode
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumberSERA == existingRowIS[j].PONumberSERA
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);
                                            //END NEW SIT
                                        }
                                    }
                                }
                                //if (flagChangeIS == 0)
                                //{
                                //    //flag = 1, //jika HS ga ada perubahan dan IS ada perubahan
                                //    //flag = 0, //jika HS ga ada perubahan dan IS ga ada perubahan

                                //    //remove row HS
                                //    var rowDel = (from o in listDataHS
                                //                  where o.GroupingCode == existingRowHS[i].GroupingCode
                                //                  select o).SingleOrDefault();
                                //    listDataHS.Remove(rowDel);

                                //    //remove row IS                                    
                                //    for (int c = 0; c < listRowISDelete.Count; c++)
                                //    {
                                //        var gCode = listRowISDelete[c].Split('|')[0].Trim();
                                //        var poNumb = listRowISDelete[c].Split('|')[1].Trim();
                                //        var rowDelIS = (from o in listDataIS
                                //                        where o.PONumberSERA == poNumb
                                //                        select o).SingleOrDefault();
                                //        listDataIS.Remove(rowDelIS);
                                //    }
                                //}
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //add task kill : by  fhi 05.06.2014
                //logEvent.WriteDBLog("", "UploadS02005_Load", false, "", ex.Message, "S02005", "SERA");
                logEvent.WriteDBLog("", "UploadS02005_Load", false, "", ex.StackTrace, "S02005", "SERA");
                logEvent.WriteDBLog("", "UploadS02005_Load", false, "", ex.InnerException.Message, "S02005", "SERA");
                logEvent.WriteDBLog("", "UploadS02005_Load", false, "", ex.InnerException.StackTrace, "S02005", "SERA");

                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02005.exe");
                //end
                throw ex;
            }
            return listDataHS;
        }

        private void versionPO( string existPoNumberIS)
         {
            int result;
            EntityCommand cmd = new EntityCommand("EProcEntities.SP_VersionPO", entityConnection);
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Parameters.Add("PONUMBER", DbType.String).Value = existPoNumberIS;
            OpenConnection();
            //result = Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<CUSTOM_S02005_TEMP_IS> CheckingHistoryHSIS2(List<CUSTOM_S02005_TEMP_HS> tempHS, List<CUSTOM_S02005_TEMP_IS> tempIS)
        {
            LogEvent logEvent = new LogEvent();
            List<CUSTOM_S02005_TEMP_HS> listDataHS = new List<CUSTOM_S02005_TEMP_HS>();
            List<CUSTOM_S02005_TEMP_IS> listDataIS = new List<CUSTOM_S02005_TEMP_IS>();
            try
            {
                if (tempHS.Count > 0)
                {
                    listDataHS = tempHS;
                    listDataIS = tempIS;
                    var existingRowHS = (from o in tempHS
                                         where entities.CUSTOM_S02005_HS.Any(e => o.GroupingCode == e.GroupingCode)
                                         select new S02005HSNewViewModel
                                         {
                                             GroupingCode = o.GroupingCode,
                                             PaymentDate = o.PaymentDate,
                                             TotalPayment = o.TotalPayment,
                                             //add by fhi 08082014
                                             version = o.version,
                                             CompanyCodeAI=o.CompanyCodeAI
                                         }).ToList();

                    for (int i = 0; i < existingRowHS.Count; i++)
                    {
                        string existGroupingCode = existingRowHS[i].GroupingCode;
                        var q = (from o in entities.CUSTOM_S02005_HS
                                 where o.GroupingCode == existGroupingCode
                                 select new S02005HSNewViewModel
                                 {
                                     GroupingCode = o.GroupingCode,
                                     PaymentDate = o.PaymentDate,
                                     TotalPayment = o.TotalPayment,
                                     //add by fhi 08082014
                                     version = o.version

                                 }).SingleOrDefault();

                        var compareHS = EqualS02005HS(existingRowHS[i], q);

                        //jika HS ada perubahan
                        if (!compareHS)
                        {

                            //b.cek IS
                            if (tempIS.Count > 0)
                            {
                                listDataIS = tempIS;
                                var existingRowIS = (from o in tempIS
                                                     where entities.CUSTOM_S02005_IS.Any(e => o.GroupingCode == e.GroupingCode)
                                                     select new S02005ISNewViewModel
                                                     {
                                                         GroupingCode = o.GroupingCode,
                                                         BillingNo = o.BillingNo,
                                                         KuitansiNo = o.KuitansiNo,
                                                         CurrencyCode = o.CurrencyCode,
                                                         BusinessAreaCode = o.BusinessAreaCode,
                                                         CustomerNo = o.CustomerNo,
                                                         SpesNumber = o.SpesNumber,
                                                         SONumber = o.SONumber,
                                                         Salesman = o.Salesman,
                                                         ChasisNumber = o.ChasisNumber,
                                                         PONumberSERA = o.PONumberSERA,
                                                         VersionPOSERA = o.VersionPOSERA,
                                                         PaymentAmountDC = o.PaymentAmountDC,
                                                         PaymentAmountLC = o.PaymentAmountLC,

                                                         //add by fhi 11082014
                                                         PaymentDate = o.paymentDate,
                                                         Version=o.version,
                                                         Flags=o.flags,
                                                         CompanyCodeAI=o.CompanyCodeAI

                                                     }).ToList();

                                for (int j = 0; j < existingRowIS.Count; j++)
                                {
                                    if (existingRowIS[j].GroupingCode == existingRowHS[i].GroupingCode)
                                    {
                                        string existPoNumberIS = existingRowIS[j].PONumberSERA;
                                        versionPO((existPoNumberIS));
                                        var zVer = (from o in entities.CUSTOM_S02005_IS
                                                 where o.PONumberSERA == existPoNumberIS
                                                 select new S02005ISNewViewModel
                                                 {
                                                     GroupingCode = o.GroupingCode,
                                                     BillingNo = o.BillingNo,
                                                     KuitansiNo = o.KuitansiNo,
                                                     CurrencyCode = o.CurrencyCode,
                                                     BusinessAreaCode = o.BusinessAreaCode,
                                                     CustomerNo = o.CustomerNo,
                                                     SpesNumber = o.SpesNumber,
                                                     SONumber = o.SONumber,
                                                     Salesman = o.Salesman,
                                                     ChasisNumber = o.ChasisNumber,
                                                     PONumberSERA = o.PONumberSERA,
                                                     VersionPOSERA = o.VersionPOSERA,
                                                     PaymentAmountDC = o.PaymentAmountDC,
                                                     PaymentAmountLC = o.PaymentAmountLC,
                                                     //add by fhi 11082014
                                                     PaymentDate = o.paymentDate,
                                                     Version = o.version
                                                 }).SingleOrDefault();
                                        var compareIS = EqualS02005IS(existingRowIS[j], zVer);

                                        //jika HS ada perubahan dan IS ada perubahan, NEW SIT:update HS-IS
                                        if (!compareIS)
                                        {
                                            //NEW SIT
                                            //a.update list HS
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.GroupingCode == existingRowHS[i].GroupingCode
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02005_TEMP_HS row = new CUSTOM_S02005_TEMP_HS();
                                            row.GroupingCode = existingRowHS[i].GroupingCode;
                                            row.PaymentDate = existingRowHS[i].PaymentDate;
                                            row.TotalPayment = existingRowHS[i].TotalPayment != null ? existingRowHS[i].TotalPayment : 0;
                                            //add by fhi 08082014
                                            row.version = q.version != null ? q.version + 1 : (existingRowHS[i].version != null ? existingRowHS[i].version:1);
                                            row.CompanyCodeAI = existingRowHS[i].CompanyCodeAI;

                                            listDataHS.Add(row);
                                            //END NEW SIT

                                            //update list IS
                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumberSERA == existingRowIS[j].PONumberSERA
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02005_TEMP_IS rowIS = new CUSTOM_S02005_TEMP_IS();
                                            rowIS.GroupingCode = existingRowIS[j].GroupingCode;
                                            rowIS.BillingNo = existingRowIS[j].BillingNo;
                                            rowIS.KuitansiNo = existingRowIS[j].KuitansiNo;
                                            rowIS.CurrencyCode = existingRowIS[j].CurrencyCode;
                                            rowIS.BusinessAreaCode = existingRowIS[j].BusinessAreaCode;
                                            rowIS.CustomerNo = existingRowIS[j].CustomerNo;
                                            rowIS.SpesNumber = existingRowIS[j].SpesNumber;
                                            rowIS.SONumber = existingRowIS[j].SONumber;
                                            rowIS.Salesman = existingRowIS[j].Salesman;
                                            rowIS.ChasisNumber = existingRowIS[j].ChasisNumber;
                                            rowIS.PONumberSERA = existingRowIS[j].PONumberSERA;
                                            rowIS.VersionPOSERA = Convert.ToInt32(version);
                                            //rowIS.VersionPOSERA = z.VersionPOSERA != null ? z.VersionPOSERA + 1 : existingRowIS[j].VersionPOSERA != null ? existingRowIS[j].VersionPOSERA : 1;
                                            rowIS.PaymentAmountDC = existingRowIS[j].PaymentAmountDC;
                                            rowIS.PaymentAmountLC = existingRowIS[j].PaymentAmountLC;

                                            //add by fhi 11082014
                                            rowIS.paymentDate = existingRowIS[j].PaymentDate;
                                            rowIS.version = zVer != null && zVer.Version != null ? zVer.Version + 1 : existingRowIS[j].Version != null ? existingRowIS[j].Version : 1;
                                            rowIS.CompanyCodeAI = existingRowIS[i].CompanyCodeAI;
                                            listDataIS.Add(rowIS);

                                            UpdateCustomPoFlags(existingRowIS[j].PONumberSERA);
                                        }
                                        //jika HS ada perubahan dan IS ga ada perubahan, NEW SIT:update HS-IS
                                        else if (compareIS)
                                        {
                                            //NEW SIT
                                            //a.update list HS
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.GroupingCode == existingRowHS[i].GroupingCode
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02005_TEMP_HS row = new CUSTOM_S02005_TEMP_HS();
                                            row.GroupingCode = existingRowHS[i].GroupingCode;
                                            row.PaymentDate = existingRowHS[i].PaymentDate;
                                            row.TotalPayment = existingRowHS[i].TotalPayment != null ? existingRowHS[i].TotalPayment : 0;
                                            //add by fhi 08082014
                                            row.version = q.version != null ? q.version + 1 : (existingRowHS[i].version != null ? existingRowHS[i].version : 1);
                                            row.CompanyCodeAI = existingRowHS[i].CompanyCodeAI;
                                            listDataHS.Add(row);

                                            //update list IS
                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumberSERA == existingRowIS[j].PONumberSERA
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02005_TEMP_IS rowIS = new CUSTOM_S02005_TEMP_IS();
                                            rowIS.GroupingCode = existingRowIS[j].GroupingCode;
                                            rowIS.BillingNo = existingRowIS[j].BillingNo;
                                            rowIS.KuitansiNo = existingRowIS[j].KuitansiNo;
                                            rowIS.CurrencyCode = existingRowIS[j].CurrencyCode;
                                            rowIS.BusinessAreaCode = existingRowIS[j].BusinessAreaCode;
                                            rowIS.CustomerNo = existingRowIS[j].CustomerNo;
                                            rowIS.SpesNumber = existingRowIS[j].SpesNumber;
                                            rowIS.SONumber = existingRowIS[j].SONumber;
                                            rowIS.Salesman = existingRowIS[j].Salesman;
                                            rowIS.ChasisNumber = existingRowIS[j].ChasisNumber;
                                            rowIS.PONumberSERA = existingRowIS[j].PONumberSERA;

                                            rowIS.VersionPOSERA = Convert.ToInt32(version);
                                            //rowIS.VersionPOSERA = z.VersionPOSERA != null ? z.VersionPOSERA + 1 : existingRowIS[j].VersionPOSERA != null ? existingRowIS[j].VersionPOSERA : 1;
                                            rowIS.PaymentAmountDC = existingRowIS[j].PaymentAmountDC;
                                            rowIS.PaymentAmountLC = existingRowIS[j].PaymentAmountLC;

                                            //add by fhi 11082014
                                            rowIS.paymentDate = existingRowIS[j].PaymentDate;
                                            rowIS.version = zVer != null && zVer.Version != null ? zVer.Version + 1 : existingRowIS[j].Version != null ? existingRowIS[j].Version : 1;
                                            rowIS.CompanyCodeAI = existingRowIS[i].CompanyCodeAI;

                                            listDataIS.Add(rowIS);

                                            UpdateCustomPoFlags(existingRowIS[j].PONumberSERA);
                                            //END NEW SIT
                                        }
                                    }
                                }
                            }
                        }
                        //jika HS ga ada perubahan
                        else if (compareHS)
                        {
                            //a.cek IS
                            if (tempIS.Count > 0)
                            {
                                int flagChangeIS = 0;
                                List<string> listRowISDelete = new List<string>();
                                listDataIS = tempIS;
                                var existingRowIS = (from o in tempIS
                                                     where entities.CUSTOM_S02005_IS.Any(e => o.GroupingCode == e.GroupingCode)
                                                     select new S02005ISNewViewModel
                                                     {
                                                         GroupingCode = o.GroupingCode,
                                                         BillingNo = o.BillingNo,
                                                         KuitansiNo = o.KuitansiNo,
                                                         CurrencyCode = o.CurrencyCode,
                                                         BusinessAreaCode = o.BusinessAreaCode,
                                                         CustomerNo = o.CustomerNo,
                                                         SpesNumber = o.SpesNumber,
                                                         SONumber = o.SONumber,
                                                         Salesman = o.Salesman,
                                                         ChasisNumber = o.ChasisNumber,
                                                         PONumberSERA = o.PONumberSERA,
                                                         VersionPOSERA = o.VersionPOSERA,
                                                         PaymentAmountDC = o.PaymentAmountDC,
                                                         PaymentAmountLC = o.PaymentAmountLC,

                                                         //add by fhi 11082014
                                                         PaymentDate = o.paymentDate,
                                                         Version=o.version,
                                                         Flags = o.flags,
                                                         CompanyCodeAI=o.CompanyCodeAI
                                                     }).ToList();
                                for (int j = 0; j < existingRowIS.Count; j++)
                                {
                                    if (existingRowIS[j].GroupingCode == existingRowHS[i].GroupingCode)
                                    {
                                        string existPoNumberIS = existingRowIS[j].PONumberSERA;
                                        versionPO(existPoNumberIS);
                                        var zVer = (from o in entities.CUSTOM_S02005_IS
                                                 where o.PONumberSERA == existPoNumberIS
                                                 select new S02005ISNewViewModel
                                                 {
                                                     GroupingCode = o.GroupingCode,
                                                     BillingNo = o.BillingNo,
                                                     KuitansiNo = o.KuitansiNo,
                                                     CurrencyCode = o.CurrencyCode,
                                                     BusinessAreaCode = o.BusinessAreaCode,
                                                     CustomerNo = o.CustomerNo,
                                                     SpesNumber = o.SpesNumber,
                                                     SONumber = o.SONumber,
                                                     Salesman = o.Salesman,
                                                     ChasisNumber = o.ChasisNumber,
                                                     PONumberSERA = o.PONumberSERA,
                                                     VersionPOSERA = o.VersionPOSERA,
                                                     PaymentAmountDC = o.PaymentAmountDC,
                                                     PaymentAmountLC = o.PaymentAmountLC,

                                                     //add by fhi 11082014
                                                     PaymentDate = o.paymentDate,
                                                     Version = o.version

                                                 }).SingleOrDefault();
                                        var compareIS = EqualS02005IS(existingRowIS[j], zVer);

                                        //jika HS ga ada perubahan dan IS ada perubahan, NEW SIT: Update HS-IS
                                        if (!compareIS)
                                        {
                                            flagChangeIS = 1;

                                            //NEW SIT
                                            //a.update list HS
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.GroupingCode == existingRowHS[i].GroupingCode
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //add new row HS
                                            CUSTOM_S02005_TEMP_HS row = new CUSTOM_S02005_TEMP_HS();
                                            row.GroupingCode = existingRowHS[i].GroupingCode;
                                            row.PaymentDate = existingRowHS[i].PaymentDate;
                                            row.TotalPayment = existingRowHS[i].TotalPayment != null ? existingRowHS[i].TotalPayment : 0;
                                            //add by fhi 08082014
                                            row.version = q.version != null ? q.version + 1 : (existingRowHS[i].version != null ? existingRowHS[i].version : 1);
                                            row.CompanyCodeAI = existingRowHS[i].CompanyCodeAI;

                                            listDataHS.Add(row);
                                            //END NEW SIT
                                            //update list IS
                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumberSERA == existingRowIS[j].PONumberSERA
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);

                                            //add new row IS
                                            CUSTOM_S02005_TEMP_IS rowIS = new CUSTOM_S02005_TEMP_IS();
                                            rowIS.GroupingCode = existingRowIS[j].GroupingCode;
                                            rowIS.BillingNo = existingRowIS[j].BillingNo;
                                            rowIS.KuitansiNo = existingRowIS[j].KuitansiNo;
                                            rowIS.CurrencyCode = existingRowIS[j].CurrencyCode;
                                            rowIS.BusinessAreaCode = existingRowIS[j].BusinessAreaCode;
                                            rowIS.CustomerNo = existingRowIS[j].CustomerNo;
                                            rowIS.SpesNumber = existingRowIS[j].SpesNumber;
                                            rowIS.SONumber = existingRowIS[j].SONumber;
                                            rowIS.Salesman = existingRowIS[j].Salesman;
                                            rowIS.ChasisNumber = existingRowIS[j].ChasisNumber;
                                            rowIS.PONumberSERA = existingRowIS[j].PONumberSERA;
                                            rowIS.VersionPOSERA = Convert.ToInt32(version);
                                            //rowIS.VersionPOSERA = z.VersionPOSERA != null ? z.VersionPOSERA + 1 : existingRowIS[j].VersionPOSERA != null ? existingRowIS[j].VersionPOSERA : 1;
                                            rowIS.PaymentAmountDC = existingRowIS[j].PaymentAmountDC;
                                            rowIS.PaymentAmountLC = existingRowIS[j].PaymentAmountLC;

                                            //add by fhi 11082014
                                            rowIS.paymentDate = existingRowIS[j].PaymentDate;
                                            rowIS.version = zVer != null && zVer.Version != null ? zVer.Version + 1 : existingRowIS[j].Version != null ? existingRowIS[j].Version : 1;
                                            rowIS.CompanyCodeAI = existingRowIS[i].CompanyCodeAI;

                                            listDataIS.Add(rowIS);
                                            UpdateCustomPoFlags(existingRowIS[j].PONumberSERA);
                                        }
                                        //jika HS ga ada perubahan dan IS ga ada perubahan
                                        else if (compareIS)
                                        {
                                            ////do nothing
                                            ////ADD LIST GROUPINGCODE+PONUMBER
                                            ////List<string> listRowISDelete = new List<string>();
                                            //listRowISDelete.Add(existingRowIS[j].GroupingCode + "|" + existingRowIS[j].PONumberSERA);

                                            //NEW SIT
                                            //remove row HS
                                            var rowDel = (from o in listDataHS
                                                          where o.GroupingCode == existingRowHS[i].GroupingCode
                                                          select o).SingleOrDefault();
                                            listDataHS.Remove(rowDel);

                                            //remove row IS
                                            var rowDelIS = (from o in listDataIS
                                                            where o.PONumberSERA == existingRowIS[j].PONumberSERA
                                                            select o).SingleOrDefault();
                                            listDataIS.Remove(rowDelIS);
                                            //END NEW SIT
                                        }
                                    }
                                }
                                //if (flagChangeIS == 0)
                                //{
                                //    //flag = 1, //jika HS ga ada perubahan dan IS ada perubahan
                                //    //flag = 0, //jika HS ga ada perubahan dan IS ga ada perubahan

                                //    //remove row HS
                                //    var rowDel = (from o in listDataHS
                                //                  where o.GroupingCode == existingRowHS[i].GroupingCode
                                //                  select o).SingleOrDefault();
                                //    listDataHS.Remove(rowDel);

                                //    //remove row IS                                    
                                //    for (int c = 0; c < listRowISDelete.Count; c++)
                                //    {
                                //        var gCode = listRowISDelete[c].Split('|')[0].Trim();
                                //        var poNumb = listRowISDelete[c].Split('|')[1].Trim();
                                //        var rowDelIS = (from o in listDataIS
                                //                        where o.PONumberSERA == poNumb
                                //                        select o).SingleOrDefault();
                                //        listDataIS.Remove(rowDelIS);
                                //    }
                                //}
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                //add task kill : by  fhi 05.06.2014
                //logEvent.WriteDBLog("", "UploadS02005_Load", false, "", ex.Message, "S02005", "SERA");
                logEvent.WriteDBLog("", "UploadS02005_Load", false, "", ex.StackTrace, "S02005", "SERA");
                logEvent.WriteDBLog("", "UploadS02005_Load", false, "", ex.InnerException.Message, "S02005", "SERA");
                logEvent.WriteDBLog("", "UploadS02005_Load", false, "", ex.InnerException.StackTrace, "S02005", "SERA");

                Process.Start("taskkill.exe", "/f /im B2BAISERA_S02005.exe");
                //end
                throw ex;
            }
            return listDataIS;
        }

        public bool EqualS02005HS(S02005HSNewViewModel item1, S02005HSNewViewModel item2)
        {
            //jika value sama return true, jika value beda return false
            if (item1 == null && item2 == null)
                return true;
            else if ((item1 != null && item2 == null) || (item1 == null && item2 != null))
                return false;

            var GroupingCode1 = !string.IsNullOrEmpty(item1.GroupingCode) ? item1.GroupingCode : "";
            var PaymentDate1 = item1.PaymentDate != null ? item1.PaymentDate : Convert.ToDateTime("01/01/1900");
            var TotalPayment1 = item1.TotalPayment != null ? item1.TotalPayment : 0;

            var GroupingCode2 = !string.IsNullOrEmpty(item2.GroupingCode) ? item2.GroupingCode : "";
            var PaymentDate2 = item2.PaymentDate != null ? item2.PaymentDate : Convert.ToDateTime("01/01/1900");
            var TotalPayment2 = item2.TotalPayment != null ? item2.TotalPayment : 0;

            //add by fhi 08082014
            var version1 = item1.version != null ? item1.version : 0;
            var version2 = item2.version != null ? item2.version : 0;

            return GroupingCode1.Equals(GroupingCode2) &&
                PaymentDate1.Equals(PaymentDate2) &&
                TotalPayment1.Equals(TotalPayment2);
        }

        public bool EqualS02005IS(S02005ISNewViewModel item1, S02005ISNewViewModel item2)
        {
            //jika value sama return true, jika value beda return false
            if (item1 == null && item2 == null)
                return true;
            else if ((item1 != null && item2 == null) || (item1 == null && item2 != null))
                return false;

            var BillingNo1 = !string.IsNullOrEmpty(item1.BillingNo) ? item1.BillingNo : "";
            var KuitansiNo1 = !string.IsNullOrEmpty(item1.KuitansiNo) ? item1.KuitansiNo : "";
            //var CurrencyCode1 = !string.IsNullOrEmpty(item1.CurrencyCode) ? item1.CurrencyCode : "";
            var CurrencyCode1 = !string.IsNullOrEmpty(item1.Flags) ? item1.Flags : "";

            var BusinessAreaCode1 = !string.IsNullOrEmpty(item1.BusinessAreaCode) ? item1.BusinessAreaCode : "";
            var CustomerNo1 = !string.IsNullOrEmpty(item1.CustomerNo) ? item1.CustomerNo : "";
            var SpesNumber1 = !string.IsNullOrEmpty(item1.SpesNumber) ? item1.SpesNumber : "";
            var SONumber1 = !string.IsNullOrEmpty(item1.SONumber) ? item1.SONumber : "";
            var Salesman1 = !string.IsNullOrEmpty(item1.Salesman) ? item1.Salesman : "";
            var ChasisNumber1 = !string.IsNullOrEmpty(item1.ChasisNumber) ? item1.ChasisNumber : "";
            var PONumberSERA1 = !string.IsNullOrEmpty(item1.PONumberSERA) ? item1.PONumberSERA : "";
            var PaymentAmountDC1 = item1.PaymentAmountDC != null ? item1.PaymentAmountDC : 0;
            var PaymentAmountLC1 = item1.PaymentAmountLC != null ? item1.PaymentAmountLC : 0;

            var BillingNo2 = !string.IsNullOrEmpty(item2.BillingNo) ? item2.BillingNo : "";
            var KuitansiNo2 = !string.IsNullOrEmpty(item2.KuitansiNo) ? item2.KuitansiNo : "";
            var CurrencyCode2 = !string.IsNullOrEmpty(item2.CurrencyCode) ? item2.CurrencyCode : "";
            var BusinessAreaCode2 = !string.IsNullOrEmpty(item2.BusinessAreaCode) ? item2.BusinessAreaCode : "";
            var CustomerNo2 = !string.IsNullOrEmpty(item2.CustomerNo) ? item2.CustomerNo : "";
            var SpesNumber2 = !string.IsNullOrEmpty(item2.SpesNumber) ? item2.SpesNumber : "";
            var SONumber2 = !string.IsNullOrEmpty(item2.SONumber) ? item2.SONumber : "";
            var Salesman2 = !string.IsNullOrEmpty(item2.Salesman) ? item2.Salesman : "";
            var ChasisNumber2 = !string.IsNullOrEmpty(item2.ChasisNumber) ? item2.ChasisNumber : "";
            var PONumberSERA2 = !string.IsNullOrEmpty(item2.PONumberSERA) ? item2.PONumberSERA : "";
            var PaymentAmountDC2 = item2.PaymentAmountDC != null ? item2.PaymentAmountDC : 0;
            var PaymentAmountLC2 = item2.PaymentAmountLC != null ? item2.PaymentAmountLC : 0;

            //add by fhi 11082014
            var paymentDate1 = item1.PaymentDate != null ? item1.PaymentDate : Convert.ToDateTime("01/01/1900");
            var version1 = item1.Version != null ? item1.Version : 0;

            var paymentDate2 = item2.PaymentDate != null ? item2.PaymentDate : Convert.ToDateTime("01/01/1900");
            var version2 = item2.Version != null ? item2.Version : 0;



            return BillingNo1.Equals(BillingNo2) &&
                KuitansiNo1.Equals(KuitansiNo2) &&
                CurrencyCode1.Equals(CurrencyCode2) &&
                BusinessAreaCode1.Equals(BusinessAreaCode2) &&
                CustomerNo1.Equals(CustomerNo2) &&
                SpesNumber1.Equals(SpesNumber2) &&
                SONumber1.Equals(SONumber2) &&
                Salesman1.Equals(Salesman2) &&
                ChasisNumber1.Equals(ChasisNumber2) &&
                PONumberSERA1.Equals(PONumberSERA2) &&
                PaymentAmountDC1.Equals(PaymentAmountDC2) &&
                PaymentAmountLC1.Equals(PaymentAmountLC2) &&
                paymentDate1.Equals(paymentDate2) ;
        }

        public int DeleteAllTempHSISS02005()
        {
            int result = 1;
            try
            {
                EntityCommand cmd = new EntityCommand("EProcEntities.sp_DeleteAllTempHSISS02005", entityConnection);
                cmd.CommandType = CommandType.StoredProcedure;
                OpenConnection();
                cmd.ExecuteNonQuery();

            }
            catch (Exception ex)
            {
                result = 0;

                throw ex;
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        public int UpdateCustomPOStatusPOId(string poNumber, string poStatusId)
        {
            int result;
            try
            {
                EntityCommand cmd = new EntityCommand("EProcEntities.sp_UpdateCustomPOStatusPOId", entityConnection);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("PONUMBER", DbType.String).Value = poNumber;
                cmd.Parameters.Add("POSTATUSID", DbType.String).Value = poStatusId;
                OpenConnection();
                result = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                result = 0;
                throw ex;
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }

        public List<CUSTOMIR> GetCustomIR(string poNumber)
        {
            var customIR = entities.CUSTOMIRs.Where(x => x.PONUMBER == poNumber).Take(1).ToList();
            return customIR;
        }

        public CUSTOM_S02007 GetS02007(string poNumber)
        {
            CUSTOM_S02007 customS02007 = entities.CUSTOM_S02007.SingleOrDefault(x => x.PONUMBER == poNumber);
            return customS02007;
        }
        
        public bool UpdateCustomPOPaymentStatusTerkirim(string poNumber, byte? isTerkirim)
        {
            try
            {
                CUSTOMPROPOSALPAYMENT updateCustomPO = entities
                    .CUSTOMPROPOSALPAYMENTs
                    .FirstOrDefault(x => x.ProposalNumber == poNumber);

                if (updateCustomPO == null)
                    return false;

                updateCustomPO.Status = isTerkirim;
                
                entities.SaveChanges();

                return true;
            }
            catch
            {
                return false;
            }            
        }

        #endregion

        #endregion



        public int UpdateCustomPoFlags(string poNumber)
        {
            int result;
            try
            {
                EntityCommand cmd = new EntityCommand("EProcEntities.spUpdateFlagRejectionByS02005", entityConnection);
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.Add("PONUMBER", DbType.String).Value = poNumber;
                OpenConnection();
                result = Convert.ToInt32(cmd.ExecuteScalar());
            }
            catch (Exception ex)
            {
                result = 0;
                throw ex;
            }
            finally
            {
                CloseConnection();
            }
            return result;
        }


    }
}