using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;
using Mercenary.Core.Engine;

namespace Mercenary.Core.Server
{
    public class ServerEngine : EngineBase
    {
        public ServerEngine(ServiceBase service) : base(service) { }
    }
}
