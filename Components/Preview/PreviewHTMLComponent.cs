using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DocsHopper.Properties;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DocsHopper.Components.Preview
{
    public class PreviewHTMLComponent : GH_Component
    {
        public PreviewHTMLComponent()
          : base("Preview HTML", "PreviewHTML", "Opens the generated HTML documentation in your default web browser.", "DocsHopper", "Preview")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("HTML Directory", "DIR", "The output folder path from your Export HTML component", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run Preview", "Run", "Set to true to launch the site in your browser", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Execution status", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string htmlDir = string.Empty;
            bool runPreview = false;

            if (!DA.GetData(0, ref htmlDir)) return;
            if (!DA.GetData(1, ref runPreview)) return;

            if (!runPreview)
            {
                DA.SetData(0, "Waiting for Run toggle...");
                return;
            }

            if (!Directory.Exists(htmlDir))
            {
                DA.SetData(0, "Error: the specified directory does not exist.");
                return;
            }

            string indexPath = Path.Combine(htmlDir, "index.html");

            if (!File.Exists(indexPath))
            {
                DA.SetData(0, "Error: Could not find index.html in the specified directory.");
                return;
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = indexPath,
                    UseShellExecute = true
                };

                Process.Start(psi);

                DA.SetData(0, "Preview launched successfully.");
            }
            catch (Exception ex)
            {
                DA.SetData(0, $"Error launching preview: {ex.Message}");
            }

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.previewHTMLCSS;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("9BD76147-3E87-49BC-94EA-6748521EF1FD"); }
        }
    }
}