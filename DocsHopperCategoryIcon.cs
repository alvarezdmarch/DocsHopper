using DocsHopper.Properties;
using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DocsHopper
{
    public class DocsHopperCategoryIcon : GH_AssemblyPriority
    {
        public override GH_LoadingInstruction PriorityLoad()
        {
            Instances.ComponentServer.AddCategoryIcon("DocsHopper", Resources.pluginSmallCleanClean);
            Instances.ComponentServer.AddCategorySymbolName("DocsHopper", 'D');
            return GH_LoadingInstruction.Proceed;
        }
    }

}
