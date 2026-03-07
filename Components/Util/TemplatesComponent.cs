using DocsHopper.Properties;
using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace DocsHopper.Components.Util
{
    public class TemplateComponent : GH_Component
    {
        private string _statusMessage = "Right-click to insert a template.";

        public TemplateComponent()
          : base("DocsHopper Templates", "Templates", "Right-click this component to insert pre-made documentation templates onto your canvas.", "DocsHopper", "Util")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "Status", "Status of the template insertion", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.SetData(0, _statusMessage);
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();

            bool foundTemplates = false;

            foreach (var name in resourceNames)
            {
                if (name.Contains(".Templates.") && name.EndsWith(".gh"))
                {
                    foundTemplates = true;
                    string cleanName = name.Substring(name.IndexOf(".Templates.") + 11);

                    ToolStripMenuItem item = new ToolStripMenuItem(cleanName);
                    item.Tag = name;
                    item.Click += AddTemplate_Click;
                    menu.Items.Add(item);
                }
            }

            if (!foundTemplates)
            {
                menu.Items.Add(new ToolStripMenuItem("No templates found in assembly."));
            }
        }

        private void AddTemplate_Click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem && menuItem.Tag is string fullResourceName)
            {
                var assembly = Assembly.GetExecutingAssembly();

                try
                {
                    using (Stream stream = assembly.GetManifestResourceStream(fullResourceName))
                    {
                        if (stream == null) throw new Exception("Could not read embedded resource.");

                        byte[] templateBytes;
                        using (MemoryStream ms = new MemoryStream())
                        {
                            stream.CopyTo(ms);
                            templateBytes = ms.ToArray();
                        }

                        string tempPath = Path.Combine(Path.GetTempPath(), "ghdocs_temp_template.gh");
                        File.WriteAllBytes(tempPath, templateBytes);

                        GH_DocumentIO io = new GH_DocumentIO();
                        if (io.Open(tempPath))
                        {
                            var templateDoc = io.Document;
                            templateDoc.SelectAll();
                            templateDoc.MutateAllIds();

                            Instances.ActiveCanvas.Document.DeselectAll();
                            Instances.ActiveCanvas.Document.MergeDocument(templateDoc);

                            File.Delete(tempPath);

                            Instances.ActiveCanvas.Refresh();

                            _statusMessage = $"Successfully inserted {menuItem.Text}!";
                            this.ExpireSolution(true);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _statusMessage = $"Error: {ex.Message}";
                    this.ExpireSolution(true);

                    MessageBox.Show($"Failed to load template: {ex.Message}", "DocsHopper Error");
                }
            }
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.templates;
            }
        }


        public override Guid ComponentGuid
        {
            get { return new Guid("143DBA33-CCBA-4517-9E79-41687F56EE72"); }
        }
    }
}