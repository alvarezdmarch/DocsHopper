using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using DocsHopper.Core;
using DocsHopper.Properties;

namespace DocsHopper.Components.Export
{
    public class ExportDocsComponent : GH_Component
    {
        public ExportDocsComponent()
          : base("Export DocsHopper", "ExportDocs",
              "Extracts component metadata and exports it as structured Markdown documentation.",
              "DocsHopper", "Export")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Plugin Name", "N", "Name of the plugin to document (e.g., 'Kangaroo2')", GH_ParamAccess.item);
            pManager.AddTextParameter("Main Description", "D", "Introductory text for the README.md", GH_ParamAccess.item);
            pManager.AddTextParameter("Output Directory", "OD", "Base folder path to save the documentation", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run Export", "Run", "Set to true to generate files", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Export status message", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string pluginName = string.Empty;
            string mainDescription = string.Empty;
            string outputDirectory = string.Empty;
            bool runExport = false;

            if (!DA.GetData(0, ref pluginName)) return;
            if (!DA.GetData(1, ref mainDescription)) return;
            if (!DA.GetData(2, ref outputDirectory)) return;
            if (!DA.GetData(3, ref runExport)) return;

            if (!runExport)
            {
                DA.SetData(0, "Waiting for Run toggle...");
                return;
            }

            DocsHopperReader reader = new DocsHopperReader();
            DocsHopperOrganizer organizer = new DocsHopperOrganizer(reader);
            DocsHopperExporter exporter = new DocsHopperExporter();

            var pluginInfo = reader.GetPluginByName(pluginName);
            if (pluginInfo == null)
            {
                DA.SetData(0, $"Error: Plugin '{pluginName}' not found.");
                return;
            }

            var proxies = reader.GetProxiesForPlugin(pluginInfo.Id);
            List<DocComponent> structuredDocs = organizer.OrganizeComponents(proxies);

            if (structuredDocs.Count == 0)
            {
                DA.SetData(0, "Error: No valid components found to document.");
                return;
            }

            string statusMessage = exporter.ExportStandardMarkdown(outputDirectory, pluginName, mainDescription, structuredDocs);

            DA.SetData(0, statusMessage);
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("853A6FDE-0C09-4950-A661-539D29798322"); }
        }

        protected override System.Drawing.Bitmap Icon => Resources.exportMD;
    }
}