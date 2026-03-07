using System;
using System.Collections.Generic;
using DocsHopper.Core;
using DocsHopper.Properties;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DocsHopper.Components.Export
{
    public class ExportJsonComponent : GH_Component
    {
        public ExportJsonComponent()
          : base("Export JSON", "ExportJSON", "Serializes component metadata and Base64 icons into a single headless .json file.", "DocsHopper", "Export")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Plugin Name", "N", "Name of the plugin to document", GH_ParamAccess.item);
            pManager.AddTextParameter("Main Description", "MD", "Introductory text for the site index", GH_ParamAccess.item);
            pManager.AddTextParameter("Output Directory", "OD", "Base folder path (e.g., your local git repository path)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run Export", "Run", "Set to true to generate the site files", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("JSON Folder", "JSON", "JSON folder", GH_ParamAccess.item);
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

            string statusMessage = exporter.ExportJson(outputDirectory, pluginName, mainDescription, structuredDocs);

            DA.SetData(0, outputDirectory);
            DA.SetData(1, statusMessage);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.exportJSON;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("00504172-79BC-4153-B24C-6F846EEF7D64"); }
        }
    }
}