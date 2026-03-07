using System;
using System.Collections.Generic;
using System.Text;
using Grasshopper.Kernel;
using DocsHopper.Core;
using DocsHopper.Properties;

namespace DocsHopper.Components.Util
{
    public class TestReaderComponent : GH_Component
    {
        public TestReaderComponent()
          : base("Plugin Reader", "Reader", "Reads all the plugin information available.", "DocsHopper", "Util")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Plugin Name", "N", "Name of the plugin to search for", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Plugin component Info", "I", "Formatted information about the components", GH_ParamAccess.list);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string pluginName = string.Empty;
            if (!DA.GetData(0, ref pluginName)) return;

            DocsHopperReader reader = new DocsHopperReader();
            DocsHopperOrganizer organizer = new DocsHopperOrganizer(reader);

            var pluginInfo = reader.GetPluginByName(pluginName);
            if (pluginInfo == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Plugin matching '{pluginName}' was not found.");
                return;
            }

            var proxies = reader.GetProxiesForPlugin(pluginInfo.Id);
            List<DocComponent> structuredDocs = organizer.OrganizeComponents(proxies);

            List<string> formattedOutputs = new List<string>();

            foreach (var doc in structuredDocs)
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"--- {doc.Name} ({doc.NickName}) ---");
                sb.AppendLine($"Description: {doc.Description}");
                sb.AppendLine($"Location: {doc.Category} > {doc.SubCategory}");
                sb.AppendLine();

                sb.AppendLine("INPUTS:");
                if (doc.Inputs.Count == 0) sb.AppendLine("  (None)");
                foreach (var input in doc.Inputs)
                {
                    sb.AppendLine($"  [{input.Access}] {input.Name} ({input.TypeName}) : {input.Description}");
                }
                sb.AppendLine();

                sb.AppendLine("OUTPUTS:");
                if (doc.Outputs.Count == 0) sb.AppendLine("  (None)");
                foreach (var output in doc.Outputs)
                {
                    sb.AppendLine($"  [{output.Access}] {output.Name} ({output.TypeName}) : {output.Description}");
                }

                formattedOutputs.Add(sb.ToString());
            }

            DA.SetDataList(0, formattedOutputs);
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("B304924E-5F79-4376-8738-2AA73ADF981D"); }
        }

        protected override System.Drawing.Bitmap Icon => Resources.listComponents;
    }
}