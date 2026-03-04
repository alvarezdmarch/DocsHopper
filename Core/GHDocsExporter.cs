using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;

namespace GHDocs.Core
{
    public class GHDocsExporter
    {
        public string ExportStandardMarkdown(string baseDirectory, string pluginName, string mainDescription, List<DocComponent> components)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseDirectory))
                    return "Error: Base directory path is empty.";

                string pluginDir = Path.Combine(baseDirectory, pluginName.Replace(" ", "_"));
                string componentsDir = Path.Combine(pluginDir, "components");
                string imagesDir = Path.Combine(pluginDir, "images");

                CreateDirectories(pluginDir, componentsDir, imagesDir);

                string indexPath = Path.Combine(pluginDir, "README.md");
                GenerateIndexFile(indexPath, pluginName, mainDescription, components);

                int successCount = 0;
                foreach (var comp in components)
                {
                    if (GenerateComponentFile(componentsDir, imagesDir, comp)) successCount++;
                }

                return $"Success: Exported {successCount}/{components.Count} components to standard Markdown in {pluginDir}";
            }
            catch (Exception ex)
            {
                return $"Error during standard export: {ex.Message}";
            }
        }

        public string ExportMkDocsSite(string baseDirectory, string pluginName, string mainDescription, List<DocComponent> components)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseDirectory))
                    return "Error: Base directory path is empty.";

                string siteDir = Path.Combine(baseDirectory, pluginName.Replace(" ", "_") + "_MkDocs");
                string docsDir = Path.Combine(siteDir, "docs");
                string componentsDir = Path.Combine(docsDir, "components");
                string imagesDir = Path.Combine(docsDir, "images");

                CreateDirectories(siteDir, docsDir, componentsDir, imagesDir);

                string indexPath = Path.Combine(docsDir, "index.md");
                GenerateIndexFile(indexPath, pluginName, mainDescription, components);

                int successCount = 0;
                foreach (var comp in components)
                {
                    if (GenerateComponentFile(componentsDir, imagesDir, comp)) successCount++;
                }

                GenerateMkDocsYaml(siteDir, pluginName, mainDescription, components);

                return $"Success: Exported MkDocs site ({successCount}/{components.Count} components) to {siteDir}";
            }
            catch (Exception ex)
            {
                return $"Error during MkDocs export: {ex.Message}";
            }
        }

        private void CreateDirectories(params string[] paths)
        {
            foreach (var path in paths)
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            }
        }

        private void GenerateIndexFile(string filePath, string pluginName, string mainDescription, List<DocComponent> components)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"# {pluginName} Documentation");
            sb.AppendLine();
            sb.AppendLine(mainDescription);
            sb.AppendLine();
            sb.AppendLine("## Components by Subcategory");

            var groupedComponents = components
                .GroupBy(c => string.IsNullOrWhiteSpace(c.SubCategory) ? "Uncategorized" : c.SubCategory)
                .OrderBy(g => g.Key);

            foreach (var group in groupedComponents)
            {
                sb.AppendLine($"### {group.Key}");

                sb.AppendLine("| Name | Description |");
                sb.AppendLine("|---|---|");

                foreach (var comp in group)
                {
                    string safeName = MakeSafeFilename(comp.Name);

                    string cleanDesc = comp.Description ?? "";
                    cleanDesc = cleanDesc.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

                    string iconMarkdown = "";
                    if (comp.Icon != null)
                    {
                        string iconFileName = $"{safeName}_icon.png";
                        iconMarkdown = $"<img src=\"images/{iconFileName}\" width=\"24\" height=\"24\" align=\"absmiddle\"> ";
                    }

                    sb.AppendLine($"| {iconMarkdown}[{comp.Name}](components/{safeName}.md) | {cleanDesc} |");
                }
                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString());
        }

        private bool GenerateComponentFile(string componentsDir, string imagesDir, DocComponent comp)
        {
            try
            {
                string safeName = MakeSafeFilename(comp.Name);
                string mdPath = Path.Combine(componentsDir, $"{safeName}.md");

                StringBuilder sb = new StringBuilder();

                sb.AppendLine($"# {comp.Name}");
                sb.AppendLine($"**Nickname:** {comp.NickName}  ");
                sb.AppendLine($"**Location:** {comp.Category} > {comp.SubCategory}  ");
                sb.AppendLine();

                if (comp.Icon != null)
                {
                    string iconFileName = $"{safeName}_icon.png";
                    string iconFilePath = Path.Combine(imagesDir, iconFileName);
                    comp.Icon.Save(iconFilePath, System.Drawing.Imaging.ImageFormat.Png);

                    string base64Icon = BitmapToBase64(comp.Icon);
                    sb.AppendLine($"<img src=\"data:image/png;base64,{base64Icon}\" alt=\"{comp.Name} Icon\" width=\"24\" height=\"24\">");
                    sb.AppendLine();
                }

                sb.AppendLine("---");
                sb.AppendLine();
                sb.AppendLine($"## Description");
                sb.AppendLine(comp.Description);
                sb.AppendLine();

                sb.AppendLine("## Inputs");
                if (comp.Inputs.Count == 0) sb.AppendLine("*None*");
                else
                {
                    sb.AppendLine("| Name | Type | Access | Description |");
                    sb.AppendLine("|---|---|---|---|");
                    foreach (var input in comp.Inputs)
                    {
                        sb.AppendLine($"| **{input.Name}** | `{input.TypeName}` | {input.Access} | {input.Description} |");
                    }
                }
                sb.AppendLine();

                sb.AppendLine("## Outputs");
                if (comp.Outputs.Count == 0) sb.AppendLine("*None*");
                else
                {
                    sb.AppendLine("| Name | Type | Access | Description |");
                    sb.AppendLine("|---|---|---|---|");
                    foreach (var output in comp.Outputs)
                    {
                        sb.AppendLine($"| **{output.Name}** | `{output.TypeName}` | {output.Access} | {output.Description} |");
                    }
                }

                File.WriteAllText(mdPath, sb.ToString());
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void GenerateMkDocsYaml(string siteDir, string pluginName, string mainDescription, List<DocComponent> components)
        {
            string yamlPath = Path.Combine(siteDir, "mkdocs.yml");
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"site_name: {pluginName} Documentation");
            sb.AppendLine($"site_description: \"{mainDescription}\"");
            sb.AppendLine("theme:");
            sb.AppendLine("  name: material");

            sb.AppendLine("docs_dir: 'docs'");
            sb.AppendLine();

            sb.AppendLine("nav:");
            sb.AppendLine("  - Home: index.md");
            sb.AppendLine("  - Components:");

            var groupedComponents = components
                .GroupBy(c => string.IsNullOrWhiteSpace(c.SubCategory) ? "Uncategorized" : c.SubCategory)
                .OrderBy(g => g.Key);

            foreach (var group in groupedComponents)
            {
                sb.AppendLine($"      - {group.Key}:");
                foreach (var comp in group)
                {
                    string safeName = MakeSafeFilename(comp.Name);
                    sb.AppendLine($"          - {comp.Name}: components/{safeName}.md");
                }
            }

            File.WriteAllText(yamlPath, sb.ToString());
        }

        private string MakeSafeFilename(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "Unknown_Component";

            char[] arr = name.Where(c => char.IsLetterOrDigit(c)).ToArray();
            string safeName = new string(arr);
            if (string.IsNullOrWhiteSpace(safeName)) safeName = "Symbol_Component_" + Guid.NewGuid().ToString().Substring(0, 4);

            return safeName;
        }

        private string BitmapToBase64(System.Drawing.Bitmap bitmap)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                byte[] byteImage = ms.ToArray();
                return Convert.ToBase64String(byteImage);
            }
        }
    }
}