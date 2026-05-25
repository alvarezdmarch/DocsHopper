using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;
using DocsHopper.Properties;
using Grasshopper.Kernel;

namespace DocsHopper.Components.Preview
{
    public class PreviewDocsComponent : GH_Component
    {
        public PreviewDocsComponent()
          : base("Preview Documentation", "PreviewDocs", "Automatically detects the documentation format in the folder and launches the correct preview (dev server or browser file).", "DocsHopper", "Preview")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Docs Folder", "DIR", "The output folder path from any Export component", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Launch Preview", "Run", "Set to true to start the server or open the browser", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("Install Dependencies", "Install", "Set to true to install dependencies if they are missing (requires Node.js & npm installed)", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Detected Format", "F", "The detected documentation framework format", GH_ParamAccess.item);
            pManager.AddTextParameter("Status", "S", "Execution status and logging", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string siteDir = string.Empty;
            bool launch = false;
            bool install = false;

            if (!DA.GetData(0, ref siteDir)) return;
            if (!DA.GetData(1, ref launch)) return;
            if (!DA.GetData(2, ref install)) return;

            if (string.IsNullOrWhiteSpace(siteDir) || !Directory.Exists(siteDir))
            {
                DA.SetData(0, "None");
                DA.SetData(1, "Error: Directory path is empty or does not exist.");
                return;
            }

            string format = DetectFormat(siteDir, out string detailPath);
            DA.SetData(0, format);

            bool isNodeProject = format == "Docusaurus" || format == "VitePress";
            bool nodeInstalled = isNodeProject && IsNpmInstalled();
            bool depsMissing = isNodeProject && !Directory.Exists(Path.Combine(siteDir, "node_modules"));
            bool sphinxMissing = format == "Sphinx (Read the Docs)" && !IsSphinxInstalled();
            bool mkDocsMissing = format == "MkDocs" && !IsMkDocsInstalled();

            if (install)
            {
                if (isNodeProject)
                {
                    if (!nodeInstalled)
                    {
                        var result = MessageBox.Show(
                            "Node.js (npm) is required to install dependencies and preview Docusaurus/VitePress sites, but it was not found on your system.\n\n" +
                            "Would you like to automatically download and install Node.js LTS using Windows Package Manager (winget) now?\n\n" +
                            "Note: You must restart Rhino and Grasshopper after installation for the changes to take effect.",
                            "Node.js Required",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Question);

                        if (result == DialogResult.Yes)
                        {
                            DA.SetData(1, "Installing Node.js LTS via winget... Please check the installation window.");
                            try
                            {
                                RunNodeJsInstallation();
                                DA.SetData(1, "Node.js installation process finished! Please restart Rhino and Grasshopper.");
                            }
                            catch (Exception ex)
                            {
                                DA.SetData(1, $"Node.js installation failed: {ex.Message}. Please download and install from https://nodejs.org/");
                            }
                        }
                        else
                        {
                            DA.SetData(1, "Installation cancelled. Please manually install Node.js from https://nodejs.org/");
                        }
                        return;
                    }

                    DA.SetData(1, "Installing Node dependencies via 'npm install'... Please check the console window.");
                    try
                    {
                        RunNpmInstall(siteDir);
                        DA.SetData(1, "Dependencies installed successfully! Set 'Launch Preview' to True to start.");
                    }
                    catch (Exception ex)
                    {
                        DA.SetData(1, $"Installation failed: {ex.Message}. Make sure Node.js is installed.");
                    }
                }
                else if (format == "Sphinx (Read the Docs)")
                {
                    DA.SetData(1, "Installing Sphinx and Sphinx RTD theme via pip... Please check the console window.");
                    try
                    {
                        RunSphinxInstall();
                        DA.SetData(1, "Sphinx packages installed successfully! Set 'Launch Preview' to True to build and start.");
                    }
                    catch (Exception ex)
                    {
                        DA.SetData(1, $"Installation failed: {ex.Message}. Make sure Python and pip are installed.");
                    }
                }
                else if (format == "MkDocs")
                {
                    DA.SetData(1, "Installing MkDocs and MkDocs Material theme via pip... Please check the console window.");
                    try
                    {
                        RunMkDocsInstall();
                        DA.SetData(1, "MkDocs packages installed successfully! Set 'Launch Preview' to True to start.");
                    }
                    catch (Exception ex)
                    {
                        DA.SetData(1, $"Installation failed: {ex.Message}. Make sure Python and pip are installed.");
                    }
                }
                else
                {
                    DA.SetData(1, $"Format '{format}' does not require package installation.");
                }
                return;
            }

            if (depsMissing)
            {
                if (!nodeInstalled)
                {
                    DA.SetData(1, "Warning: Dependencies and Node.js are missing. Set 'Install Dependencies' to True to install Node.js via winget.");
                }
                else
                {
                    DA.SetData(1, "Warning: Dependencies are missing. Please set 'Install Dependencies' to True to run 'npm install'.");
                }
                return;
            }

            if (sphinxMissing)
            {
                DA.SetData(1, "Warning: Sphinx is not installed on your system. Set 'Install Dependencies' to True to install sphinx via pip.");
                return;
            }

            if (mkDocsMissing)
            {
                DA.SetData(1, "Warning: MkDocs is not installed on your system. Set 'Install Dependencies' to True to install mkdocs via pip.");
                return;
            }

            if (!launch)
            {
                DA.SetData(1, $"Server offline. Ready to preview {format} site.");
                return;
            }

            try
            {
                switch (format)
                {
                    case "Docusaurus":
                        LaunchDocusaurus(siteDir);
                        DA.SetData(1, "Docusaurus dev server launched via 'npx docusaurus start'.");
                        break;

                    case "VitePress":
                        LaunchVitePress(siteDir, detailPath);
                        DA.SetData(1, "VitePress dev server launched via 'npx vitepress dev'.");
                        break;

                    case "Sphinx (Read the Docs)":
                        LaunchSphinx(siteDir, detailPath);
                        DA.SetData(1, "Sphinx build triggered and documentation index opened.");
                        break;

                    case "MkDocs":
                        LaunchMkDocs(siteDir);
                        DA.SetData(1, "MkDocs server launched via 'mkdocs serve'.");
                        break;

                    case "Vanilla HTML":
                        LaunchHtml(detailPath);
                        DA.SetData(1, "Vanilla HTML opened directly in default browser.");
                        break;

                    default:
                        DA.SetData(1, "Error: Unknown or unsupported documentation format.");
                        break;
                }
            }
            catch (Exception ex)
            {
                DA.SetData(1, $"Error launching preview: {ex.Message}");
            }
        }

        private string DetectFormat(string siteDir, out string detailPath)
        {
            detailPath = string.Empty;

            if (File.Exists(Path.Combine(siteDir, "docusaurus.config.js")) || File.Exists(Path.Combine(siteDir, "sidebars.js")))
            {
                return "Docusaurus";
            }

            string docsVp = Path.Combine(siteDir, "docs", ".vitepress");
            string rootVp = Path.Combine(siteDir, ".vitepress");
            if (Directory.Exists(docsVp))
            {
                detailPath = "docs";
                return "VitePress";
            }
            if (Directory.Exists(rootVp))
            {
                detailPath = "root";
                return "VitePress";
            }

            string docsSphinx = Path.Combine(siteDir, "docs", "conf.py");
            string rootSphinx = Path.Combine(siteDir, "conf.py");
            if (File.Exists(docsSphinx))
            {
                detailPath = Path.Combine(siteDir, "docs");
                return "Sphinx (Read the Docs)";
            }
            if (File.Exists(rootSphinx))
            {
                detailPath = siteDir;
                return "Sphinx (Read the Docs)";
            }

            if (File.Exists(Path.Combine(siteDir, "mkdocs.yml")))
            {
                return "MkDocs";
            }

            string indexPath = Path.Combine(siteDir, "index.html");
            if (File.Exists(indexPath))
            {
                detailPath = indexPath;
                return "Vanilla HTML";
            }

            return "Unknown";
        }

        private bool IsNpmInstalled()
        {
            if (CheckNpmInPath())
                return true;

            string nodePath = GetNodeJsInstallPath();
            if (!string.IsNullOrEmpty(nodePath))
            {
                EnsureNodeJsInPath(nodePath);
                return CheckNpmInPath();
            }

            return false;
        }

        private bool CheckNpmInPath()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c where npm",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit();
                    return p.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool IsSphinxInstalled()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c where sphinx-build",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit();
                    return p.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private bool IsMkDocsInstalled()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c where mkdocs",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using (Process p = Process.Start(psi))
                {
                    p.WaitForExit();
                    return p.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private void RunSphinxInstall()
        {
            ProcessStartInfo installPsi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c \"python -m pip install sphinx sphinx_rtd_theme & pause\"",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };
            Process p = Process.Start(installPsi);
            if (p != null)
            {
                p.WaitForExit();
            }
        }

        private void RunMkDocsInstall()
        {
            ProcessStartInfo installPsi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c \"python -m pip install mkdocs mkdocs-material & pause\"",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };
            Process p = Process.Start(installPsi);
            if (p != null)
            {
                p.WaitForExit();
            }
        }

        private string GetNodeJsInstallPath()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Node.js"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("InstallPath") as string;
                        if (!string.IsNullOrEmpty(val) && Directory.Exists(val))
                            return val;
                    }
                }
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Node.js"))
                {
                    if (key != null)
                    {
                        var val = key.GetValue("InstallPath") as string;
                        if (!string.IsNullOrEmpty(val) && Directory.Exists(val))
                            return val;
                    }
                }
            }
            catch
            {
            }

            string defaultPath = @"C:\Program Files\nodejs";
            if (Directory.Exists(defaultPath))
                return defaultPath;

            string defaultPathX86 = @"C:\Program Files (x86)\nodejs";
            if (Directory.Exists(defaultPathX86))
                return defaultPathX86;

            return null;
        }

        private void EnsureNodeJsInPath(string nodePath)
        {
            try
            {
                string currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
                if (!currentPath.Contains(nodePath))
                {
                    string separator = currentPath.EndsWith(";") || string.IsNullOrEmpty(currentPath) ? "" : ";";
                    Environment.SetEnvironmentVariable("PATH", currentPath + separator + nodePath);
                }
            }
            catch
            {
            }
        }

        private void RunNodeJsInstallation()
        {
            ProcessStartInfo installPsi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c \"winget install OpenJS.NodeJS.LTS --accept-source-agreements --accept-package-agreements || pause\"",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };
            Process installProc = Process.Start(installPsi);
            if (installProc != null)
            {
                installProc.WaitForExit();
            }
        }

        private void RunNpmInstall(string siteDir)
        {
            ProcessStartInfo installPsi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c \"npm install & pause\"",
                WorkingDirectory = siteDir,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };
            Process installProc = Process.Start(installPsi);
            if (installProc != null)
            {
                installProc.WaitForExit();
            }
        }

        private void LaunchDocusaurus(string siteDir)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c \"npx docusaurus start || pause\"",
                WorkingDirectory = siteDir,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized
            };
            Process.Start(psi);
        }

        private void LaunchVitePress(string siteDir, string mode)
        {
            string args = mode == "docs" ? "/c \"npx vitepress dev docs || pause\"" : "/c \"npx vitepress dev || pause\"";
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = args,
                WorkingDirectory = siteDir,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized
            };
            Process.Start(psi);

            System.Threading.Thread.Sleep(1500);
            Process.Start(new ProcessStartInfo("http://localhost:5173/") { UseShellExecute = true });
        }

        private void LaunchSphinx(string siteDir, string SphinxRootDir)
        {
            string makeBat = Path.Combine(SphinxRootDir, "make.bat");
            ProcessStartInfo buildInfo = new ProcessStartInfo();
            buildInfo.FileName = "cmd.exe";
            buildInfo.WorkingDirectory = SphinxRootDir;
            buildInfo.UseShellExecute = true;
            buildInfo.WindowStyle = ProcessWindowStyle.Normal;

            if (File.Exists(makeBat))
            {
                buildInfo.Arguments = "/c \"make.bat html || pause\"";
            }
            else
            {
                buildInfo.Arguments = "/c \"sphinx-build -b html . _build/html || pause\"";
            }

            Process buildProc = Process.Start(buildInfo);
            buildProc.WaitForExit(8000);

            string indexPath = Path.Combine(SphinxRootDir, "_build", "html", "index.html");
            if (File.Exists(indexPath))
            {
                Process.Start(new ProcessStartInfo(indexPath) { UseShellExecute = true });
            }
            else
            {
                throw new FileNotFoundException($"Could not find generated index.html at {indexPath}. Sphinx build may have failed.");
            }
        }

        private void LaunchMkDocs(string siteDir)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = "/c \"mkdocs serve || pause\"",
                WorkingDirectory = siteDir,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Minimized
            };
            Process.Start(psi);

            System.Threading.Thread.Sleep(1000);
            Process.Start(new ProcessStartInfo("http://127.0.0.1:8000/") { UseShellExecute = true });
        }

        private void LaunchHtml(string indexPath)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = indexPath,
                UseShellExecute = true
            };
            Process.Start(psi);
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
            get { return new Guid("8DCE85B2-DF89-4FA3-87CE-7B4F35C7A2E9"); }
        }
    }
}
