using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using GHDocs.Core;
using GHDocs.Properties;

namespace GHDocs.Components.Export
{
    public class ExportHTMLComponent : GH_Component
    {
        public ExportHTMLComponent()
          : base("Export HTML/CSS", "ExportHTML/CSS", "Generates a zero-dependency, static HTML/CSS website for your documentation.", "GHDocs", "Export")
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
            pManager.AddTextParameter("HTML/CSS Folder", "HTML/CSS", "HTML/CSS folder", GH_ParamAccess.item);
            pManager.AddTextParameter("Status", "S", "Export status message", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string pluginName = string.Empty;
            string mainDescription = string.Empty;
            string outputDirectory = string.Empty;
            bool runExport = false;

            if(!DA.GetData(0, ref pluginName)) return;
            if(!DA.GetData(1, ref mainDescription)) return;
            if(!DA.GetData(2, ref outputDirectory)) return;
            if(!DA.GetData(3, ref runExport)) return;

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
                DA.SetData(1, $"Error: Plugin '{pluginName}' not found.");
                return;
            }

            var proxies = reader.GetProxiesForPlugin(pluginInfo.Id);
            List<DocComponent> structuredDocs = organizer.OrganizeComponents(proxies);

            if (structuredDocs.Count == 0)
            {
                DA.SetData(1, $"Error: No components found for plugin '{pluginName}'.");
                return;
            }

            string statusMessage = exporter.ExportHtmlSite(outputDirectory, pluginName, mainDescription, structuredDocs);

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
            get { return new Guid("C0F54440-3C9A-49EF-9364-03186FF3B953"); }
        }
    }
}