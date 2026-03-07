using System;
using System.Collections.Generic;
using System.Linq;
using Grasshopper;
using Grasshopper.Kernel;

namespace DocsHopper.Core
{
    public class DocsHopperReader
    {
        public DocsHopperReader()
        {
            // Constructor can be expanded later if you need to inject settings or logging.
        }

        public List<GH_AssemblyInfo> GetAllLoadedPlugins()
        {
            return Instances.ComponentServer.Libraries.ToList();
        }

        public GH_AssemblyInfo GetPluginByName(string pluginName)
        {
            if (string.IsNullOrWhiteSpace(pluginName)) return null;

            return Instances.ComponentServer.Libraries
                .FirstOrDefault(lib => lib.Name.Equals(pluginName, StringComparison.OrdinalIgnoreCase) ||
                                       lib.Name.IndexOf(pluginName, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public List<IGH_ObjectProxy> GetProxiesForPlugin(Guid libraryId)
        {
            return Instances.ComponentServer.ObjectProxies
                .Where(proxy => proxy.LibraryGuid == libraryId)
                .ToList();
        }

        public IGH_Component InstantiateComponent(IGH_ObjectProxy proxy)
        {
            if (proxy.Obsolete) return null;

            try
            {
                var instance = proxy.CreateInstance();
                return instance as IGH_Component;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}