using System;
using System.Collections.Generic;
using System.Text;
using DocsHopper.Properties;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DocsHopper.Components.Customize
{
    public class CustomMKDocsComponent : GH_Component
    {
        public CustomMKDocsComponent()
          : base("Custom MKDocs", "CustomMkDocs", "Generates custom YAML configuration settings for the MkDocs Material theme.", "DocsHopper", "Customize")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Theme Scheme", "SCH", "Use 'default' for Light mode, or 'slate' for Dark mode", GH_ParamAccess.item, "slate");
            pManager.AddTextParameter("Primary Color", "PC", "Primary theme color (e.g., indigo, blue, teal, deep orange)", GH_ParamAccess.item, "indigo");
            pManager.AddTextParameter("Accent Color", "AC", "Accent theme color (e.g., light blue, amber, pink)", GH_ParamAccess.item, "deep orange");
            pManager.AddTextParameter("Repo URL", "URL", "URL to your GitHub repository (adds a top-right GitHub icon)", GH_ParamAccess.item, "");
            pManager.AddTextParameter("Site Author", "AUT", "Name of the plugin author", GH_ParamAccess.item, "");
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("YAML Config", "YAML", "The generated YAML string. Plug this into the MkDocs Exporter.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string scheme = "slate";
            string primary = "indigo";
            string accent = "deep orange";
            string repoUrl = string.Empty;
            string author = string.Empty;

            DA.GetData(0, ref scheme);
            DA.GetData(1, ref primary);
            DA.GetData(2, ref accent);
            DA.GetData(3, ref repoUrl);
            DA.GetData(4, ref author);

            StringBuilder yaml = new StringBuilder();

            yaml.AppendLine("theme:");
            yaml.AppendLine("  name: material");
            yaml.AppendLine("  palette:");
            yaml.AppendLine($"    scheme: {scheme.ToLower()}");
            yaml.AppendLine($"    primary: {primary.ToLower()}");
            yaml.AppendLine($"    accent: {accent.ToLower()}");

            yaml.AppendLine("  features:");
            yaml.AppendLine("    - navigation.instant");
            yaml.AppendLine("    - navigation.tracking");
            yaml.AppendLine("    - navigation.sections");
            yaml.AppendLine("    - toc.follow");

            if (!string.IsNullOrWhiteSpace(repoUrl))
            {
                yaml.AppendLine($"repo_url: {repoUrl}");
                yaml.AppendLine("repo_name: GitHub");
            }

            if (!string.IsNullOrWhiteSpace(author))
            {
                yaml.AppendLine($"site_author: {author}");
            }

            DA.SetData(0, yaml.ToString());
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.customMKDocs;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("0E15A237-7DA1-4A90-BCF5-9C08B02E1899"); }
        }
    }
}