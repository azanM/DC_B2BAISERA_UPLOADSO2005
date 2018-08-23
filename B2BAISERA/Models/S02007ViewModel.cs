using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace B2BAISERA.Models
{
    public class S02007ViewModel
    {
        public int ID
        {
            get;
            set;
        }

        public string PONUMBER
        {
            get;
            set;
        }

        public Nullable<int> TransactionDataID
        {
            get;
            set;
        }

        public Nullable<int> VERSIONPOSERA
        {
            get;
            set;
        }

        public string BILLINGNO
        {
            get;
            set;
        }

        public Nullable<System.DateTime> INVOICERECEIPTDATE
        {
            get;
            set;
        }

        public Nullable<int> DATAVERSION
        {
            get;
            set;
        }

        public string CompanyCodeAI
        {
            get;
            set;
        }

        public Nullable<System.DateTime> payPlan { get; set; }
    }
}
