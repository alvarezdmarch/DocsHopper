using System;
using System.Collections.Generic;
using DocsHopper.Core;
using DocsHopper.Properties;
using Grasshopper.Kernel;

namespace DocsHopper.Components.Export
{
    public class ExportVitePressComponent : GH_Component
    {
        public ExportVitePressComponent()
          : base("Export VitePress", "ExportVitePress", "Generates a site structure compatible with VitePress documentation framework.", "DocsHopper", "Export")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Plugin Name", "N", "Name of the plugin to document", GH_ParamAccess.item);
            pManager.AddTextParameter("Main Description", "MD", "Introductory text for the site index", GH_ParamAccess.item);
            pManager.AddTextParameter("Output Directory", "OD", "Base folder path (e.g., your local git repository path)", GH_ParamAccess.item);
            pManager.AddTextParameter("Examples Directory", "EX", "Optional directory containing .gh example files", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run Export", "Run", "Set to true to generate the site files", GH_ParamAccess.item, false);
            pManager[3].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("VitePress Folder", "Dir", "VitePress folder", GH_ParamAccess.item);
            pManager.AddTextParameter("Status", "S", "Export status message", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string pluginName = string.Empty;
            string mainDescription = string.Empty;
            string outputDirectory = string.Empty;
            string examplesDirectory = null;
            bool runExport = false;

            if (!DA.GetData(0, ref pluginName)) return;
            if (!DA.GetData(1, ref mainDescription)) return;
            if (!DA.GetData(2, ref outputDirectory)) return;
            DA.GetData(3, ref examplesDirectory);
            if (!DA.GetData(4, ref runExport)) return;

            if (!runExport)
            {
                DA.SetData(1, "Waiting for Run toggle...");
                return;
            }

            DocsHopperReader reader = new DocsHopperReader();
            DocsHopperOrganizer organizer = new DocsHopperOrganizer(reader);
            DocsHopperExporter exporter = new DocsHopperExporter();

            var pluginInfo = reader.GetPluginByName(pluginName);
            if (pluginInfo == null)
            {
                DA.SetData(1, $"Error: Plugin '{pluginName}' not found.");
                return;
            }

            var proxies = reader.GetProxiesForPlugin(pluginInfo.Id);
            List<DocComponent> structuredDocs = organizer.OrganizeComponents(proxies);

            if (structuredDocs.Count == 0)
            {
                DA.SetData(1, "Error: No valid components found to document.");
                return;
            }

            string statusMessage = exporter.ExportVitePressSite(outputDirectory, pluginName, mainDescription, structuredDocs, examplesDirectory);

            DA.SetData(0, outputDirectory);
            DA.SetData(1, statusMessage);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.exportHTMLCSS;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("A4B869E4-D4C8-4DE3-9A16-7CE8896C7A1D"); }
        }
    }
}
