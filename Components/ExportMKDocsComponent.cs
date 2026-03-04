using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using GHDocs.Core;
using System.IO;

namespace GHDocs.Components
{
    public class ExportMKDocsComponent : GH_Component
    {
        public ExportMKDocsComponent()
          : base("Export MkDocs Site", "ExportMkDocs",
              "Extracts component metadata and generates a fully configured MkDocs static website.",
              "GHDocs", "Export")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Plugin Name", "P", "Name of the plugin to document", GH_ParamAccess.item);
            pManager.AddTextParameter("Main Description", "D", "Introductory text for the site index", GH_ParamAccess.item);
            pManager.AddTextParameter("Output Directory", "OD", "Base folder path (e.g., your local git repository path)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run Export", "Run", "Set to true to generate the site files", GH_ParamAccess.item, false);
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

            GHDocsReader reader = new GHDocsReader();
            GHDocsOrganizer organizer = new GHDocsOrganizer(reader);
            GHDocsExporter exporter = new GHDocsExporter();

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
                DA.SetData(1, "Error: No valid components found to document.");
                return;
            }

            string statusMessage = exporter.ExportMkDocsSite(outputDirectory, pluginName, mainDescription, structuredDocs);

            DA.SetData(0, System.IO.Path.Join(outputDirectory, pluginName.Replace(" ", "_") + "_MkDocs"));
            DA.SetData(1, statusMessage);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("4277E556-101A-4BB4-B9BC-B65943D81F0C"); }
        }
    }
}