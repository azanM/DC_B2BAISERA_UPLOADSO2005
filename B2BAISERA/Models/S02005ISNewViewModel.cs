using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace B2BAISERA.Models
{
    public class S02005ISNewViewModel
    {
        public int ID
        {
            get;
            set;
        }

        public Nullable<int> HSID
        {
            get;
            set;
        }

        public string GroupingCode
        {
            get;
            set;
        }

        public string BillingNo
        {
            get;
            set;
        }

        public string KuitansiNo
        {
            get;
            set;
        }

        public string CurrencyCode
        {
            get;
            set;
        }

        public string BusinessAreaCode
        {
            get;
            set;
        }

        public string CustomerNo
        {
            get;
            set;
        }

        public string SpesNumber
        {
            get;
            set;
        }

        public string SONumber
        {
            get;
            set;
        }

        public string Salesman
        {
            get;
            set;
        }

        public string ChasisNumber
        {
            get;
            set;
        }

        public string PONumberSERA
        {
            get;
            set;
        }

        public Nullable<int> VersionPOSERA
        {
            get;
            set;
        }

        public Nullable<decimal> PaymentAmountDC
        {
            get;
            set;
        }

        public Nullable<decimal> PaymentAmountLC
        {
            get;
            set;
        }

        //add by fhi 08082014
        public Nullable<System.DateTime> PaymentDate
        {
            get;
            set;
        }

        public Nullable<int> Version
        {
            get;
            set;
        }

        public string Flags
        {
            get;
            set;
        }

        public string CompanyCodeAI
        {
            get;
            set;
        }
        //end
    }
}
