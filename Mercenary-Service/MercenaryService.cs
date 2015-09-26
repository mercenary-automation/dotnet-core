using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;

namespace Mercenary.Service
{
    public partial class MercenaryService : ServiceBase
    {
        public MercenaryService()
        {
            InitializeComponent();

            //serviceLog.Source = source;
            //serviceLog.Log = logName;
        }

        protected override void OnStart(string[] args)
        {

        }

        protected override void OnStop()
        {
        }
    }
}
