using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;
using Mercenary.Core.Engine;

namespace Mercenary.Core.Target
{
    public class TargetEngine : EngineBase
    {
        public TargetEngine(ServiceBase service) : base(service) { }
    }
}
