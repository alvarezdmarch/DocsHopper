using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Grasshopper.Kernel;
using System.Text.Json;
using System.Runtime.CompilerServices;

namespace DocsHopper.Core
{
    public class DocsHopperExporter
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

        public string ExportMkDocsSite(string baseDirectory, string pluginName, string mainDescription, List<DocComponent> components, string customConfig = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseDirectory))
                    return "Error: Base directory path is empty.";

                //string siteDir = Path.Combine(baseDirectory, pluginName.Replace(" ", "_") + "_MkDocs");
                string siteDir = baseDirectory;
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

                GenerateMkDocsYaml(siteDir, pluginName, mainDescription, components, customConfig);

                return $"Success: Exported MkDocs site ({successCount}/{components.Count} components) to {siteDir}";
            }
            catch (Exception ex)
            {
                return $"Error during MkDocs export: {ex.Message}";
            }
        }

        public string ExportAdvancedMkDocsSite(string baseDirectory, string pluginName, string mainDescription, List<DocComponent> components, List<DocSection> customSections, string customConfig = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseDirectory))
                    return "Error: Base directory path is empty.";

                string siteDir = baseDirectory;
                string docsDir = Path.Combine(siteDir, "docs");
                string componentsDir = Path.Combine(docsDir, "components");
                string imagesDir = Path.Combine(docsDir, "images");

                CreateDirectories(siteDir, docsDir, componentsDir, imagesDir);

                string advancedDescription = mainDescription;

                if (customSections != null && customSections.Count > 0)
                {
                    advancedDescription += "\n\n";

                    foreach (var section in customSections)
                    {
                        advancedDescription += $"## {section.Title}\n\n{section.Content}\n\n";

                        if (!string.IsNullOrWhiteSpace(section.ImagePath) && File.Exists(section.ImagePath))
                        {
                            string fileName = Path.GetFileName(section.ImagePath);
                            string destPath = Path.Combine(imagesDir, fileName);

                            File.Copy(section.ImagePath, destPath, true);

                            advancedDescription += $"![{section.Title}](images/{fileName})\n\n";
                        }
                    }
                }

                string indexPath = Path.Combine(docsDir, "index.md");

                GenerateIndexFile(indexPath, pluginName, advancedDescription, components);

                int successCount = 0;
                foreach (var comp in components)
                {
                    if (GenerateComponentFile(componentsDir, imagesDir, comp)) successCount++;
                }

                GenerateMkDocsYaml(siteDir, pluginName, mainDescription, components, customConfig);

                return $"Success: Exported Advanced MkDocs site ({successCount}/{components.Count} components) to {siteDir}";
            }
            catch (Exception ex)
            {
                return $"Error during Advanced MkDocs export: {ex.Message}";
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

        private void GenerateMkDocsYaml(string siteDir, string pluginName, string mainDescription, List<DocComponent> components, string customConfig = null)
        {
            string yamlPath = Path.Combine(siteDir, "mkdocs.yml");
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"site_name: {pluginName} Documentation");
            sb.AppendLine($"site_description: \"{mainDescription}\"");

            if (!string.IsNullOrWhiteSpace(customConfig))
            {
                sb.AppendLine(customConfig);
            }
            else
            {
                sb.AppendLine("theme:");
                sb.AppendLine("  name: material");
            }

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

        public string ExportJson(string baseDirectory, string pluginName, string mainDescription, List<DocComponent> components)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseDirectory))
                    return "Error: Base directory path is empty.";

                string pluginDir = Path.Combine(baseDirectory, pluginName.Replace(" ", "_"));
                CreateDirectories(pluginDir);

                var exportData = new
                {
                    PluginMetadata = new
                    {
                        Name = pluginName,
                        Description = mainDescription,
                        ExportedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        ComponentCount = components.Count
                    },
                    Components = components.Select(c => new
                    {
                        c.Name,
                        c.NickName,
                        c.Description,
                        c.Category,
                        c.SubCategory,
                        IconBase = c.Icon != null ? BitmapToBase64(c.Icon) : null,
                        Inputs = c.Inputs,
                        Outputs = c.Outputs
                    }).ToList()
                };

                var options = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(exportData, options);

                string jsonPath = Path.Combine(pluginDir, "documentation.json");
                File.WriteAllText(jsonPath, jsonString);

                return $"Success: Exported {components.Count} components to documentation.json in {pluginDir}";
            }
            catch (Exception ex)
            {
                return $"Error during JSON export: {ex.Message}";
            }
        }

        public string ExportHtmlSite(string baseDirectory, string pluginName, string mainDescription, List<DocComponent> components, string customCss = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseDirectory))
                    return "Error: Base directory path is empty.";

                string siteDir = baseDirectory;
                string componentsDir = Path.Combine(siteDir, "components");
                string imagesDir = Path.Combine(siteDir, "images");

                CreateDirectories(siteDir, componentsDir, imagesDir);

                string finalCss = string.IsNullOrWhiteSpace(customCss) ? GetDefaultCss() : customCss;
                File.WriteAllText(Path.Combine(siteDir, "style.css"), finalCss);

                GenerateHtmlIndex(Path.Combine(siteDir, "index.html"), pluginName, mainDescription, components);

                int successCount = 0;
                foreach (var comp in components)
                {
                    if (GenerateHtmlComponentPage(componentsDir, comp)) successCount++;
                }

                return $"Success: Exported HTML site ({successCount}/{components.Count} components) to {siteDir}";
            }
            catch (Exception ex)
            {
                return $"Error during HTML export: {ex.Message}";
            }
        }

        public string ExportAdvancedHtmlSite(string baseDirectory, string pluginName, string mainDescription, List<DocComponent> components, List<DocSection> customSections, string customCss = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseDirectory))
                    return "Error: Base directory path is empty.";

                string siteDir = Path.Combine(baseDirectory, pluginName.Replace(" ", "_") + "_HTML");
                string componentsDir = Path.Combine(siteDir, "components");
                string imagesDir = Path.Combine(siteDir, "images");

                CreateDirectories(siteDir, componentsDir, imagesDir);

                string finalCss = string.IsNullOrWhiteSpace(customCss) ? GetDefaultCss() : customCss;
                File.WriteAllText(Path.Combine(siteDir, "style.css"), finalCss);

                string advancedHtmlContent = $"<p class=\"description\">{mainDescription}</p>\n";

                if (customSections != null && customSections.Count > 0)
                {
                    advancedHtmlContent += "<div class=\"advanced-sections\">\n";
                    foreach (var section in customSections)
                    {
                        advancedHtmlContent += $"  <div class=\"section\">\n";
                        advancedHtmlContent += $"    <h2>{section.Title}</h2>\n";
                        advancedHtmlContent += $"    <p>{section.Content}</p>\n";

                        if (!string.IsNullOrWhiteSpace(section.ImagePath) && File.Exists(section.ImagePath))
                        {
                            string fileName = Path.GetFileName(section.ImagePath);
                            string destPath = Path.Combine(imagesDir, fileName);
                            File.Copy(section.ImagePath, destPath, true);

                            advancedHtmlContent += $"    <img src=\"images/{fileName}\" alt=\"{section.Title}\" style=\"max-width: 100%; height: auto; border-radius: 8px; margin: 1rem 0;\" />\n";
                        }
                        advancedHtmlContent += $"  </div>\n";
                    }
                    advancedHtmlContent += "</div>\n";
                }

                GenerateAdvancedHtmlIndex(Path.Combine(siteDir, "index.html"), pluginName, advancedHtmlContent, components);

                int successCount = 0;
                foreach (var comp in components)
                {
                    if (GenerateHtmlComponentPage(componentsDir, comp)) successCount++;
                }

                return $"Success: Exported Advanced HTML site ({successCount}/{components.Count} components) to {siteDir}";
            }
            catch (Exception ex)
            {
                return $"Error during Advanced HTML export: {ex.Message}";
            }
        }

        private void GenerateAdvancedHtmlIndex(string filePath, string pluginName, string customHtmlBlock, List<DocComponent> components)
        {
            System.Text.StringBuilder html = new System.Text.StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang=\"en\">");
            html.AppendLine("<head>");
            html.AppendLine("  <meta charset=\"UTF-8\">");
            html.AppendLine($"  <title>{pluginName} Documentation</title>");
            html.AppendLine("  <link rel=\"stylesheet\" href=\"style.css\">");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("  <div class=\"container\">");
            html.AppendLine($"    <h1>{pluginName}</h1>");

            html.AppendLine(customHtmlBlock);

            html.AppendLine("    <h2>Components</h2>");
            html.AppendLine("    <table>");
            html.AppendLine("      <tr><th>Name</th><th>Category</th><th>Description</th></tr>");

            foreach (var comp in components)
            {
                string safeName = MakeSafeFilename(comp.Name);
                html.AppendLine($"      <tr>");
                html.AppendLine($"        <td><a href=\"components/{safeName}.html\">{comp.Name}</a></td>");
                html.AppendLine($"        <td>{comp.Category} > {comp.SubCategory}</td>");
                html.AppendLine($"        <td>{comp.Description}</td>");
                html.AppendLine($"      </tr>");
            }

            html.AppendLine("    </table>");
            html.AppendLine("  </div>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            File.WriteAllText(filePath, html.ToString());
        }

        private string GetDefaultCss()
        {
            return @"
:root { --primary: #d94c1a; --bg: #f8fafc; --text: #1e293b; --border: #e2e8f0; }
body { font-family: 'Segoe UI', system-ui, sans-serif; margin: 0; padding: 0; background: var(--bg); color: var(--text); line-height: 1.6; }
.container { max-width: 900px; margin: 0 auto; padding: 2rem; background: white; min-height: 100vh; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); }
h1, h2, h3 { color: var(--text); border-bottom: 1px solid var(--border); padding-bottom: 0.5rem; }
h1 { color: var(--primary); border-bottom: none; }
table { width: 100%; border-collapse: collapse; margin: 1.5rem 0; font-size: 0.95rem; }
th, td { padding: 12px; text-align: left; border-bottom: 1px solid var(--border); }
th { background-color: #f1f5f9; font-weight: 600; color: #475569; }
tr:hover { background-color: #f8fafc; }
a { color: var(--primary); text-decoration: none; font-weight: 500; }
a:hover { text-decoration: underline; }
.nav { margin-bottom: 2rem; padding-bottom: 1rem; border-bottom: 1px solid var(--border); font-size: 0.9rem; }
.meta { color: #64748b; font-size: 0.9rem; margin-bottom: 2rem; }
.icon { vertical-align: middle; margin-right: 10px; border-radius: 4px; }
code { background: #f1f5f9; padding: 2px 6px; border-radius: 4px; font-family: 'Courier New', monospace; font-size: 0.9em; }";
        }

        private void GenerateHtmlIndex(string filePath, string pluginName, string mainDescription, List<DocComponent> components)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\">");
            sb.AppendLine($"<title>{pluginName} Documentation</title>");
            sb.AppendLine("<link rel=\"stylesheet\" href=\"style.css\"></head><body>");
            sb.AppendLine($"<div class=\"container\">");

            sb.AppendLine($"<h1>{pluginName} Documentation</h1>");
            sb.AppendLine($"<p>{mainDescription}</p>");

            var groupedComponents = components.GroupBy(c => string.IsNullOrWhiteSpace(c.SubCategory) ? "Uncategorized" : c.SubCategory).OrderBy(g => g.Key);

            foreach (var group in groupedComponents)
            {
                sb.AppendLine($"<h2>{group.Key}</h2>");
                sb.AppendLine("<table><thead><tr><th>Name</th><th>Description</th></tr></thead><tbody>");

                foreach (var comp in group)
                {
                    string safeName = MakeSafeFilename(comp.Name);
                    string cleanDesc = comp.Description?.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ") ?? "";

                    string iconImg = "";
                    if (comp.Icon != null)
                    {
                        string base64Icon = BitmapToBase64(comp.Icon);
                        iconImg = $"<img src=\"data:image/png;base64,{base64Icon}\" class=\"icon\" width=\"24\" height=\"24\" alt=\"icon\">";
                    }

                    sb.AppendLine($"<tr><td>{iconImg}<a href=\"components/{safeName}.html\">{comp.Name}</a></td><td>{cleanDesc}</td></tr>");
                }
                sb.AppendLine("</tbody></table>");
            }

            sb.AppendLine("</div></body></html>");
            File.WriteAllText(filePath, sb.ToString());
        }

        private bool GenerateHtmlComponentPage(string componentsDir, DocComponent comp)
        {
            try
            {
                string safeName = MakeSafeFilename(comp.Name);
                string htmlPath = Path.Combine(componentsDir, $"{safeName}.html");

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<!DOCTYPE html><html lang=\"en\"><head><meta charset=\"UTF-8\">");
                sb.AppendLine($"<title>{comp.Name} - Component</title>");

                sb.AppendLine("<link rel=\"stylesheet\" href=\"../style.css\"></head><body>");
                sb.AppendLine("<div class=\"container\">");

                sb.AppendLine("<div class=\"nav\"><a href=\"../index.html\">&larr; Back to Index</a></div>");

                string iconImg = "";
                if (comp.Icon != null)
                {
                    string base64Icon = BitmapToBase64(comp.Icon);
                    iconImg = $"<img src=\"data:image/png;base64,{base64Icon}\" class=\"icon\" width=\"32\" height=\"32\" alt=\"icon\">";
                }

                sb.AppendLine($"<h1>{iconImg}{comp.Name}</h1>");
                sb.AppendLine($"<div class=\"meta\"><strong>Nickname:</strong> {comp.NickName} | <strong>Location:</strong> {comp.Category} > {comp.SubCategory}</div>");

                sb.AppendLine($"<h2>Description</h2><p>{comp.Description}</p>");

                sb.AppendLine("<h2>Inputs</h2>");
                if (comp.Inputs.Count == 0) sb.AppendLine("<p><em>None</em></p>");
                else
                {
                    sb.AppendLine("<table><thead><tr><th>Name</th><th>Type</th><th>Access</th><th>Description</th></tr></thead><tbody>");
                    foreach (var input in comp.Inputs)
                    {
                        sb.AppendLine($"<tr><td><strong>{input.Name}</strong></td><td><code>{input.TypeName}</code></td><td>{input.Access}</td><td>{input.Description}</td></tr>");
                    }
                    sb.AppendLine("</tbody></table>");
                }

                sb.AppendLine("<h2>Outputs</h2>");
                if (comp.Outputs.Count == 0) sb.AppendLine("<p><em>None</em></p>");
                else
                {
                    sb.AppendLine("<table><thead><tr><th>Name</th><th>Type</th><th>Access</th><th>Description</th></tr></thead><tbody>");
                    foreach (var output in comp.Outputs)
                    {
                        sb.AppendLine($"<tr><td><strong>{output.Name}</strong></td><td><code>{output.TypeName}</code></td><td>{output.Access}</td><td>{output.Description}</td></tr>");
                    }
                    sb.AppendLine("</tbody></table>");
                }

                sb.AppendLine("</div></body></html>");
                File.WriteAllText(htmlPath, sb.ToString());
                return true;
            }
            catch { return false; }
        }
    }

}