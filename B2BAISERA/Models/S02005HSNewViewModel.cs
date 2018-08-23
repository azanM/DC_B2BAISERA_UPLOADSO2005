using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace B2BAISERA.Models
{
    public class S02005HSNewViewModel
    {
        public int ID
        {
            get;
            set;
        }

        public Nullable<int> TransactionDataID
        {
            get;
            set;
        }

        public string GroupingCode
        {
            get;
            set;
        }

        public Nullable<System.DateTime> PaymentDate
        {
            get;
            set;
        }

        public Nullable<decimal> TotalPayment
        {
            get;
            set;
        }

        //add by fhi 08082014
        public Nullable<int> version
        {
            get;
            set;
        }

        public string CompanyCodeAI
        {
            get;
            set;
        }
    }
}
