using DocsHopper.Properties;
using Eto.Drawing;
using Eto.Forms;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;

namespace DocsHopper.Components.Preview
{
    public class InternalPreviewComponent : GH_Component
    {
        private Form _previewForm;
        private WebView _webView;
        private bool _launchTriggered = false;

        public InternalPreviewComponent()
          : base("Internal Preview", "IntPreview", "Spawns a native floating web viewer inside Rhino to preview documentation.", "DocsHopper", "Preview")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("URL or Folder", "P", "A localhost URL or the HTML output folder path.", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Launch", "Run", "Connect a Button to launch or refresh the preview window.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Execution status", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string pathOrUrl = string.Empty;
            bool launch = false;

            if (!DA.GetData(0, ref pathOrUrl)) return;
            if (!DA.GetData(1, ref launch)) return;

            if (launch && !_launchTriggered)
            {
                _launchTriggered = true;
                LaunchEtoPreview(pathOrUrl);
                DA.SetData(0, "Preview window is active.");
            }
            else if (!launch)
            {
                _launchTriggered = false;
                DA.SetData(0, "Waiting for launch button...");
            }
        }

        private void LaunchEtoPreview(string pathOrUrl)
        {
            Uri targetUri;

            if (pathOrUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                targetUri = new Uri(pathOrUrl);
            }
            else
            {
                string fullPath = pathOrUrl;
                if (Directory.Exists(fullPath))
                {
                    fullPath = Path.Combine(fullPath, "index.html");
                }

                if (!File.Exists(fullPath))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Could not find a valid file at: {fullPath}");
                    return;
                }

                targetUri = new Uri(fullPath);
            }

            if (_previewForm != null && !_previewForm.IsDisposed)
            {
                _webView.Url = targetUri;
                _previewForm.BringToFront();
            }
            else
            {
                _previewForm = new Form
                {
                    Title = "DocsHopper Live Preview",
                    ClientSize = new Size(1000, 700),
                    Topmost = true,
                    Owner = Rhino.UI.RhinoEtoApp.MainWindow
                };

                _webView = new WebView { Url = targetUri };
                _previewForm.Content = _webView;

                _previewForm.Closed += (sender, e) =>
                {
                    _previewForm = null;
                    _webView = null;
                };

                _previewForm.Show();
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
            get { return new Guid("37EA40BE-7914-47B4-8573-C8F50BA5B45C"); }
        }
    }
}