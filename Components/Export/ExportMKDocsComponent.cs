using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using DocsHopper.Core;
using System.IO;
using DocsHopper.Properties;

namespace DocsHopper.Components.Export
{
    public class ExportMKDocsComponent : GH_Component
    {
        public ExportMKDocsComponent()
          : base("Export MkDocs Site", "ExportMkDocs",
              "Extracts component metadata and generates a fully configured MkDocs static website.",
              "DocsHopper", "Export")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Plugin Name", "N", "Name of the plugin to document", GH_ParamAccess.item);
            pManager.AddTextParameter("Main Description", "MD", "Introductory text for the site index", GH_ParamAccess.item);
            pManager.AddTextParameter("Output Directory", "OD", "Base folder path (e.g., your local git repository path)", GH_ParamAccess.item);
            pManager.AddTextParameter("Custom Config", "CC", "Optional custom MkDocs configuration YAML", GH_ParamAccess.item);
            pManager.AddTextParameter("Examples Directory", "EX", "Optional directory containing .gh example files", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run Export", "Run", "Set to true to generate the site files", GH_ParamAccess.item, false);
            pManager[3].Optional = true;
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("MkDocs Folder", "MkD", "MkDocs folder", GH_ParamAccess.item);
            pManager.AddTextParameter("Status", "S", "Export status message", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string pluginName = string.Empty;
            string mainDescription = string.Empty;
            string outputDirectory = string.Empty;
            string customConfig = null;
            string examplesDirectory = null;
            bool runExport = false;

            if (!DA.GetData(0, ref pluginName)) return;
            if (!DA.GetData(1, ref mainDescription)) return;
            if (!DA.GetData(2, ref outputDirectory)) return;

            DA.GetData(3, ref customConfig);
            DA.GetData(4, ref examplesDirectory);

            if (!DA.GetData(5, ref runExport)) return;

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

            string statusMessage = exporter.ExportMkDocsSite(outputDirectory, pluginName, mainDescription, structuredDocs, customConfig, examplesDirectory);

            DA.SetData(0, outputDirectory);
            DA.SetData(1, statusMessage);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.exportMkDocs;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("4277E556-101A-4BB4-B9BC-B65943D81F0C"); }
        }
    }
}