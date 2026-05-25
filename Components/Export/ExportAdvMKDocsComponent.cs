using DocsHopper.Core;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using DocsHopper.Properties;

namespace DocsHopper.Components.Export
{
    public class ExportAdvMKDocsComponent : GH_Component
    {
        public ExportAdvMKDocsComponent()
          : base("Export Advanced MkDocs Site", "AdvExportMkDocs",
              "Extracts component metadata and generates a customized MkDocs static website.",
              "DocsHopper", "Export")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Plugin Name", "N", "Name of the plugin", GH_ParamAccess.item);
            pManager.AddTextParameter("Main Description", "MD", "Introductory text", GH_ParamAccess.item);
            pManager.AddTextParameter("Output Directory", "OD", "Base folder path", GH_ParamAccess.item);
            pManager.AddTextParameter("Custom Config", "CC", "Optional custom MkDocs YAML", GH_ParamAccess.item);
            pManager.AddGenericParameter("Homepage Sections", "SEC", "List of custom sections from the HomeSec component", GH_ParamAccess.list);
            pManager.AddTextParameter("Examples Directory", "EX", "Optional directory containing .gh example files", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run Export", "Run", "Set to true to generate", GH_ParamAccess.item, false);
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("MkDocs Folder", "MkD", "MkDocs folder path", GH_ParamAccess.item);
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

            string customConfig = null;
            DA.GetData(3, ref customConfig);

            List<Grasshopper.Kernel.Types.IGH_Goo> rawSections = new List<Grasshopper.Kernel.Types.IGH_Goo>();
            List<DocSection> customSections = new List<DocSection>();

            if(DA.GetDataList(4, rawSections))
            { 
                foreach (var goo in rawSections)
                {
                    if (goo is Grasshopper.Kernel.Types.GH_ObjectWrapper wrapper && wrapper.Value is DocSection section)
                    {
                        customSections.Add(section);
                    }
                }
            }

            string examplesDirectory = null;
            DA.GetData(5, ref examplesDirectory);

            if (!DA.GetData(6, ref runExport)) return;

            if (!runExport)
            {
                DA.SetData(1, "Waiting for Run toogle...");
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

            string statusMessage = exporter.ExportAdvancedMkDocsSite(outputDirectory, pluginName, mainDescription, structuredDocs, customSections, customConfig, examplesDirectory);

            DA.SetData(0, outputDirectory);
            DA.SetData(1, statusMessage);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.exportAdvMkDocs;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("AFCEF264-3C96-41D9-A727-12D139C36465"); }
        }
    }
}