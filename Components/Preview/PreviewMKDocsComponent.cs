using System;
using System.Diagnostics;
using System.IO;
using DocsHopper.Properties;
using Grasshopper.Kernel;

namespace DocsHopper.Components.Preview
{
    public class PreviewMkDocsComponent : GH_Component
    {
        public PreviewMkDocsComponent()
          : base("Preview MkDocs", "PreviewDocs", "Launches a local MkDocs server, opens your browser, and provides an option to install dependencies.", "DocsHopper", "Preview")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("MkDocs Folder", "MKD", "The root directory of your MkDocs site (where mkdocs.yml is located)", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Launch Server", "LS", "Set to true to start the server and open the browser", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Install MkDocs", "IMKD", "Run this if 'mkdocs' is not recognized. Installs mkdocs-material via pip.", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Connection status or errors", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string siteDir = string.Empty;
            bool launch = false;
            bool install = false;

            if (!DA.GetData(0, ref siteDir)) return;
            if (!DA.GetData(1, ref launch)) return;
            if (!DA.GetData(2, ref install)) return;

            if (install)
            {
                DA.SetData(0, "Installing MkDocs Material... Check terminal for progress.");
                try
                {
                    RunInstallation();
                    DA.SetData(0, "Installation complete. You can now try 'Launch Server'.");
                }
                catch (Exception ex)
                {
                    DA.SetData(0, "Installation Error: " + ex.Message);
                }
                return;
            }

            if (!launch)
            {
                DA.SetData(0, "Server offline.");
                return;
            }

            if (!Directory.Exists(siteDir))
            {
                DA.SetData(0, "Error: Directory does not exist.");
                return;
            }

            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c mkdocs serve",
                    WorkingDirectory = siteDir,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Minimized
                };
                Process.Start(startInfo);

                System.Threading.Thread.Sleep(1000);

                Process.Start(new ProcessStartInfo("http://127.0.0.1:8000/") { UseShellExecute = true });

                DA.SetData(0, "Server running and browser opened.");
            }
            catch (Exception ex)
            {
                DA.SetData(0, "Error: " + ex.Message + ". Try running 'Install MkDocs' if command is not found.");
            }
        }

        private void RunInstallation()
        {
            string command = "python -m pip install --upgrade pip && pip install mkdocs-material";
            ProcessStartInfo installInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };

            Process p = Process.Start(installInfo);
            p.WaitForExit();
        }

        public override Guid ComponentGuid => new Guid("1D05CA62-C1E8-4C10-B55A-02EBAEA44156");

        protected override System.Drawing.Bitmap Icon => Resources.previewMKDocs;
    }
}