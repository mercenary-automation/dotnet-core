using System;
using Mercenary.Core.Engine;

namespace Mercenary.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            EngineBase engine = EngineBase.GetEngine();
            engine.StartRunning();
        }
    }
}
