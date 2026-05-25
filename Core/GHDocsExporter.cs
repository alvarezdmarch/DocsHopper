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
        public string ExportStandardMarkdown(string baseDirectory, string pluginName, string mainDescription, List<DocComponent> components, string examplesDir = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseDirectory))
                    return "Error: Base directory path is empty.";

                string pluginDir = Path.Combine(baseDirectory, pluginName.Replace(" ", "_"));
                string componentsDir = Path.Combine(pluginDir, "components");
                string imagesDir = Path.Combine(pluginDir, "images");
                string destExamplesDir = Path.Combine(pluginDir, "examples");

                CreateDirectories(pluginDir, componentsDir, imagesDir);

                string indexPath = Path.Combine(pluginDir, "README.md");
                GenerateIndexFile(indexPath, pluginName, mainDescription, components);

                int successCount = 0;
                foreach (var comp in components)
                {
                    if (GenerateComponentFile(componentsDir, imagesDir, comp, examplesDir, destExamplesDir)) successCount++;
                }

                return $"Success: Exported {successCount}/{components.Count} components to standard Markdown in {pluginDir}";
            }
            catch (Exception ex)
            {
                return $"Error during standard export: {ex.Message}";
            }
        }

        public string ExportMkDocsSite(string baseDirectory, string pluginName, string mainDescription, List<DocComponent> components, string customConfig = null, string examplesDir = null)
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
                string destExamplesDir = Path.Combine(docsDir, "examples");

                CreateDirectories(siteDir, docsDir, componentsDir, imagesDir);

                string indexPath = Path.Combine(docsDir, "index.md");
                GenerateIndexFile(indexPath, pluginName, mainDescription, components);

                int successCount = 0;
                foreach (var comp in components)
                {
                    if (GenerateComponentFile(componentsDir, imagesDir, comp, examplesDir, destExamplesDir)) successCount++;
                }

                GenerateMkDocsYaml(siteDir, pluginName, mainDescription, components, customConfig);

                return $"Success: Exported MkDocs site ({successCount}/{components.Count} components) to {siteDir}";
            }
            catch (Exception ex)
            {
                return $"Error during MkDocs export: {ex.Message}";
            }
        }

        public string ExportAdvancedMkDocsSite(string baseDirectory, string pluginName, string mainDescription, List<DocComponent> components, List<DocSection> customSections, string customConfig = null, string examplesDir = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseDirectory))
                    return "Error: Base directory path is empty.";

                string siteDir = baseDirectory;
                string docsDir = Path.Combine(siteDir, "docs");
                string componentsDir = Path.Combine(docsDir, "components");
                string imagesDir = Path.Combine(docsDir, "images");
                string destExamplesDir = Path.Combine(docsDir, "examples");

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
                    if (GenerateComponentFile(componentsDir, imagesDir, comp, examplesDir, destExamplesDir)) successCount++;
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
                        iconMarkdown = $"![{comp.Name}](./images/{iconFileName}) ";
                    }

                    sb.AppendLine($"| {iconMarkdown}[{comp.Name}](components/{safeName}.md) | {cleanDesc} |");
                }
                sb.AppendLine();
            }

            File.WriteAllText(filePath, sb.ToString());
        }

        private bool GenerateComponentFile(string componentsDir, string imagesDir, DocComponent comp, string examplesDir = null, string destExamplesDir = null)
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

                if (!string.IsNullOrWhiteSpace(examplesDir) && !string.IsNullOrWhiteSpace(destExamplesDir))
                {
                    string exampleLink = CopyAndLinkExampleFile(comp, examplesDir, destExamplesDir, "markdown");
                    if (!string.IsNullOrEmpty(exampleLink))
                    {
                        sb.AppendLine();
                        sb.AppendLine(exampleLink);
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

        public string ExportHtmlSite(string baseDirectory, string pluginName, string mainDescription, List<DocComponent> components, string customCss = null, string examplesDir = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseDirectory))
                    return "Error: Base directory path is empty.";

                string siteDir = baseDirectory;
                string componentsDir = Path.Combine(siteDir, "components");
                string imagesDir = Path.Combine(siteDir, "images");
                string destExamplesDir = Path.Combine(siteDir, "examples");

                CreateDirectories(siteDir, componentsDir, imagesDir);

                string finalCss = string.IsNullOrWhiteSpace(customCss) ? GetDefaultCss() : customCss;
                File.WriteAllText(Path.Combine(siteDir, "style.css"), finalCss);

                GenerateHtmlIndex(Path.Combine(siteDir, "index.html"), pluginName, mainDescription, components);

                int successCount = 0;
                foreach (var comp in components)
                {
                    if (GenerateHtmlComponentPage(componentsDir, comp, examplesDir, destExamplesDir)) successCount++;
                }

                return $"Success: Exported HTML site ({successCount}/{components.Count} components) to {siteDir}";
            }
            catch (Exception ex)
            {
                return $"Error during HTML export: {ex.Message}";
            }
        }

        public string ExportAdvancedHtmlSite(string baseDirectory, string pluginName, string mainDescription, List<DocComponent> components, List<DocSection> customSections, string customCss = null, string examplesDir = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseDirectory))
                    return "Error: Base directory path is empty.";

                string siteDir = Path.Combine(baseDirectory, pluginName.Replace(" ", "_") + "_HTML");
                string componentsDir = Path.Combine(siteDir, "components");
                string imagesDir = Path.Combine(siteDir, "images");
                string destExamplesDir = Path.Combine(siteDir, "examples");

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
                    if (GenerateHtmlComponentPage(componentsDir, comp, examplesDir, destExamplesDir)) successCount++;
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

        private bool GenerateHtmlComponentPage(string componentsDir, DocComponent comp, string examplesDir = null, string destExamplesDir = null)
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

                if (!string.IsNullOrWhiteSpace(examplesDir) && !string.IsNullOrWhiteSpace(destExamplesDir))
                {
                    string exampleLink = CopyAndLinkExampleFile(comp, examplesDir, destExamplesDir, "html");
                    if (!string.IsNullOrEmpty(exampleLink))
                    {
                        sb.AppendLine(exampleLink);
                    }
                }

                sb.AppendLine("</div></body></html>");
                File.WriteAllText(htmlPath, sb.ToString());
                return true;
            }
            catch { return false; }
        }

        public string ExportDocusaurusSite(string baseDirectory, string pluginName, string mainDescription, List<DocComponent> components, string examplesDir = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseDirectory))
                    return "Error: Base directory path is empty.";

                string siteDir = baseDirectory;
                string docsDir = Path.Combine(siteDir, "docs");
                string componentsDir = Path.Combine(docsDir, "components");
                string imagesDir = Path.Combine(docsDir, "images");
                string destExamplesDir = Path.Combine(docsDir, "examples");

                CreateDirectories(siteDir, docsDir, componentsDir, imagesDir);

                // 0. Generate package.json
                string pkgJsonPath = Path.Combine(siteDir, "package.json");
                StringBuilder pkgSb = new StringBuilder();
                pkgSb.AppendLine("{");
                pkgSb.AppendLine("  \"name\": \"" + pluginName.Replace(" ", "-").ToLower() + "-docs\",");
                pkgSb.AppendLine("  \"version\": \"1.0.0\",");
                pkgSb.AppendLine("  \"private\": true,");
                pkgSb.AppendLine("  \"scripts\": {");
                pkgSb.AppendLine("    \"start\": \"docusaurus start\",");
                pkgSb.AppendLine("    \"build\": \"docusaurus build\"");
                pkgSb.AppendLine("  },");
                pkgSb.AppendLine("  \"dependencies\": {");
                pkgSb.AppendLine("    \"@docusaurus/core\": \"^3.5.2\",");
                pkgSb.AppendLine("    \"@docusaurus/preset-classic\": \"^3.5.2\",");
                pkgSb.AppendLine("    \"react\": \"^18.0.0\",");
                pkgSb.AppendLine("    \"react-dom\": \"^18.0.0\"");
                pkgSb.AppendLine("  },");
                pkgSb.AppendLine("  \"overrides\": {");
                pkgSb.AppendLine("    \"serialize-javascript\": \"^7.0.5\",");
                pkgSb.AppendLine("    \"uuid\": \"^11.1.1\"");
                pkgSb.AppendLine("  }");
                pkgSb.AppendLine("}");
                File.WriteAllText(pkgJsonPath, pkgSb.ToString());

                string indexPath = Path.Combine(docsDir, "index.md");
                StringBuilder indexSb = new StringBuilder();
                indexSb.AppendLine("---");
                indexSb.AppendLine("slug: /");
                indexSb.AppendLine("sidebar_position: 1");
                indexSb.AppendLine($"title: {pluginName} Documentation");
                indexSb.AppendLine("---");
                indexSb.AppendLine();
                indexSb.AppendLine($"# {pluginName} Documentation");
                indexSb.AppendLine();
                indexSb.AppendLine(mainDescription);
                indexSb.AppendLine();
                indexSb.AppendLine("## Components by Subcategory");

                var groupedComponents = components
                    .GroupBy(c => string.IsNullOrWhiteSpace(c.SubCategory) ? "Uncategorized" : c.SubCategory)
                    .OrderBy(g => g.Key);

                foreach (var group in groupedComponents)
                {
                    indexSb.AppendLine($"### {group.Key}");
                    indexSb.AppendLine("| Name | Description |");
                    indexSb.AppendLine("|---|---|");

                    foreach (var comp in group)
                    {
                        string safeName = MakeSafeFilename(comp.Name);
                        string cleanDesc = comp.Description ?? "";
                        cleanDesc = cleanDesc.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

                        string iconMarkdown = "";
                        if (comp.Icon != null)
                        {
                            string iconFileName = $"{safeName}_icon.png";
                            iconMarkdown = $"![{comp.Name}](./images/{iconFileName}) ";
                        }

                        indexSb.AppendLine($"| {iconMarkdown}[{comp.Name}](components/{safeName}.md) | {cleanDesc} |");
                    }
                    indexSb.AppendLine();
                }
                File.WriteAllText(indexPath, indexSb.ToString());

                int successCount = 0;
                foreach (var comp in components)
                {
                    if (GenerateDocusaurusComponentFile(componentsDir, imagesDir, comp, examplesDir, destExamplesDir)) successCount++;
                }

                GenerateDocusaurusSidebars(siteDir, components);

                GenerateDocusaurusConfig(siteDir, pluginName, mainDescription);
                File.WriteAllText(Path.Combine(docsDir, "custom.css"), "/* Custom styles for Docusaurus */\n");

                return $"Success: Exported Docusaurus site ({successCount}/{components.Count} components) to {siteDir}";
            }
            catch (Exception ex)
            {
                return $"Error during Docusaurus export: {ex.Message}";
            }
        }

        private bool GenerateDocusaurusComponentFile(string componentsDir, string imagesDir, DocComponent comp, string examplesDir = null, string destExamplesDir = null)
        {
            try
            {
                string safeName = MakeSafeFilename(comp.Name);
                string mdPath = Path.Combine(componentsDir, $"{safeName}.md");

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("---");
                sb.AppendLine($"title: {comp.Name}");
                sb.AppendLine($"sidebar_label: {comp.Name}");
                sb.AppendLine("---");
                sb.AppendLine();
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
                    sb.AppendLine($"<img src=\"data:image/png;base64,{base64Icon}\" alt=\"{comp.Name} Icon\" width=\"24\" height=\"24\" />");
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
                        string desc = string.IsNullOrWhiteSpace(input.Description) ? "-" : input.Description.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
                        sb.AppendLine($"| **{input.Name}** | `{input.TypeName}` | {input.Access} | {desc} |");
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
                        string desc = string.IsNullOrWhiteSpace(output.Description) ? "-" : output.Description.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
                        sb.AppendLine($"| **{output.Name}** | `{output.TypeName}` | {output.Access} | {desc} |");
                    }
                }

                if (!string.IsNullOrWhiteSpace(examplesDir) && !string.IsNullOrWhiteSpace(destExamplesDir))
                {
                    string exampleLink = CopyAndLinkExampleFile(comp, examplesDir, destExamplesDir, "markdown");
                    if (!string.IsNullOrEmpty(exampleLink))
                    {
                        sb.AppendLine();
                        sb.AppendLine(exampleLink);
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

        private void GenerateDocusaurusSidebars(string siteDir, List<DocComponent> components)
        {
            string sidebarPath = Path.Combine(siteDir, "sidebars.js");
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("const sidebars = {");
            sb.AppendLine("  mySidebar: [");
            sb.AppendLine("    'index',");
            sb.AppendLine("    {");
            sb.AppendLine("      type: 'category',");
            sb.AppendLine("      label: 'Components',");
            sb.AppendLine("      items: [");

            var groupedComponents = components
                .GroupBy(c => string.IsNullOrWhiteSpace(c.SubCategory) ? "Uncategorized" : c.SubCategory)
                .OrderBy(g => g.Key);

            int groupCount = groupedComponents.Count();
            int currentGroup = 0;
            foreach (var group in groupedComponents)
            {
                currentGroup++;
                sb.AppendLine("        {");
                sb.AppendLine("          type: 'category',");
                sb.AppendLine($"          label: '{group.Key}',");
                sb.AppendLine("          items: [");

                int compCount = group.Count();
                int currentComp = 0;
                foreach (var comp in group)
                {
                    currentComp++;
                    string safeName = MakeSafeFilename(comp.Name);
                    string trailingComma = (currentComp < compCount) ? "," : "";
                    sb.AppendLine($"            'components/{safeName}'{trailingComma}");
                }

                sb.AppendLine("          ]");
                string groupTrailingComma = (currentGroup < groupCount) ? "," : "";
                sb.AppendLine($"        }}{groupTrailingComma}");
            }

            sb.AppendLine("      ]");
            sb.AppendLine("    }");
            sb.AppendLine("  ]");
            sb.AppendLine("};");
            sb.AppendLine("module.exports = sidebars;");

            File.WriteAllText(sidebarPath, sb.ToString());
        }

        private void GenerateDocusaurusConfig(string siteDir, string pluginName, string mainDescription)
        {
            string configPath = Path.Combine(siteDir, "docusaurus.config.js");
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("const config = {");
            sb.AppendLine($"  title: '{pluginName} Documentation',");
            sb.AppendLine($"  tagline: '{mainDescription.Replace("'", "\\'").Replace("\r\n", " ").Replace("\n", " ")}',");
            sb.AppendLine("  url: 'https://your-website.com',");
            sb.AppendLine("  baseUrl: '/',");
            sb.AppendLine("  onBrokenLinks: 'warn',");
            sb.AppendLine("  onBrokenMarkdownLinks: 'warn',");
            sb.AppendLine("  presets: [");
            sb.AppendLine("    [");
            sb.AppendLine("      'classic',");
            sb.AppendLine("      {");
            sb.AppendLine("        docs: {");
            sb.AppendLine("          sidebarPath: require.resolve('./sidebars.js'),");
            sb.AppendLine("          routeBasePath: '/',");
            sb.AppendLine("        },");
            sb.AppendLine("        blog: false,");
            sb.AppendLine("        theme: {");
            sb.AppendLine("          customCss: require.resolve('./docs/custom.css'),");
            sb.AppendLine("        },");
            sb.AppendLine("      },");
            sb.AppendLine("    ],");
            sb.AppendLine("  ],");
            sb.AppendLine("  themeConfig: {");
            sb.AppendLine("    navbar: {");
            sb.AppendLine($"      title: '{pluginName}',");
            sb.AppendLine("      items: [],");
            sb.AppendLine("    },");
            sb.AppendLine("    footer: {");
            sb.AppendLine("      style: 'dark',");
            sb.AppendLine($"      copyright: `Copyright © ${System.DateTime.Now.Year} ${pluginName}. Built with DocsHopper.`,");
            sb.AppendLine("    },");
            sb.AppendLine("  },");
            sb.AppendLine("};");
            sb.AppendLine("module.exports = config;");

            File.WriteAllText(configPath, sb.ToString());
        }

        public string ExportVitePressSite(string baseDirectory, string pluginName, string mainDescription, List<DocComponent> components, string examplesDir = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseDirectory))
                    return "Error: Base directory path is empty.";

                string siteDir = baseDirectory;
                string docsDir = Path.Combine(siteDir, "docs");
                string componentsDir = Path.Combine(docsDir, "components");
                string imagesDir = Path.Combine(docsDir, "images");
                string vpDir = Path.Combine(docsDir, ".vitepress");
                string destExamplesDir = Path.Combine(docsDir, "examples");

                CreateDirectories(siteDir, docsDir, componentsDir, imagesDir, vpDir);

                // 0. Generate package.json
                string pkgJsonPath = Path.Combine(siteDir, "package.json");
                StringBuilder pkgSb = new StringBuilder();
                pkgSb.AppendLine("{");
                pkgSb.AppendLine("  \"name\": \"" + pluginName.Replace(" ", "-").ToLower() + "-docs\",");
                pkgSb.AppendLine("  \"version\": \"1.0.0\",");
                pkgSb.AppendLine("  \"private\": true,");
                pkgSb.AppendLine("  \"scripts\": {");
                pkgSb.AppendLine("    \"dev\": \"vitepress dev docs\",");
                pkgSb.AppendLine("    \"build\": \"vitepress build docs\"");
                pkgSb.AppendLine("  },");
                pkgSb.AppendLine("  \"dependencies\": {");
                pkgSb.AppendLine("    \"vitepress\": \"^1.6.4\",");
                pkgSb.AppendLine("    \"vue\": \"^3.4.0\"");
                pkgSb.AppendLine("  },");
                pkgSb.AppendLine("  \"overrides\": {");
                pkgSb.AppendLine("    \"vite\": \"^6.4.2\",");
                pkgSb.AppendLine("    \"esbuild\": \"^0.25.0\"");
                pkgSb.AppendLine("  }");
                pkgSb.AppendLine("}");
                File.WriteAllText(pkgJsonPath, pkgSb.ToString());

                string indexPath = Path.Combine(docsDir, "index.md");
                StringBuilder indexSb = new StringBuilder();
                indexSb.AppendLine("---");
                indexSb.AppendLine($"title: {pluginName} Documentation");
                indexSb.AppendLine("---");
                indexSb.AppendLine();
                indexSb.AppendLine($"# {pluginName} Documentation");
                indexSb.AppendLine();
                indexSb.AppendLine(mainDescription);
                indexSb.AppendLine();
                indexSb.AppendLine("## Components by Subcategory");

                var groupedComponents = components
                    .GroupBy(c => string.IsNullOrWhiteSpace(c.SubCategory) ? "Uncategorized" : c.SubCategory)
                    .OrderBy(g => g.Key);

                foreach (var group in groupedComponents)
                {
                    indexSb.AppendLine($"### {group.Key}");
                    indexSb.AppendLine("| Name | Description |");
                    indexSb.AppendLine("|---|---|");

                    foreach (var comp in group)
                    {
                        string safeName = MakeSafeFilename(comp.Name);
                        string cleanDesc = comp.Description ?? "";
                        cleanDesc = cleanDesc.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

                        string iconMarkdown = "";
                        if (comp.Icon != null)
                        {
                            string iconFileName = $"{safeName}_icon.png";
                            iconMarkdown = $"![{comp.Name}](./images/{iconFileName}) ";
                        }

                        indexSb.AppendLine($"| {iconMarkdown}[{comp.Name}](components/{safeName}.md) | {cleanDesc} |");
                    }
                    indexSb.AppendLine();
                }
                File.WriteAllText(indexPath, indexSb.ToString());

                int successCount = 0;
                foreach (var comp in components)
                {
                    if (GenerateVitePressComponentFile(componentsDir, imagesDir, comp, examplesDir, destExamplesDir)) successCount++;
                }

                GenerateVitePressConfig(vpDir, pluginName, mainDescription, components);

                return $"Success: Exported VitePress site ({successCount}/{components.Count} components) to {siteDir}";
            }
            catch (Exception ex)
            {
                return $"Error during VitePress export: {ex.Message}";
            }
        }

        private bool GenerateVitePressComponentFile(string componentsDir, string imagesDir, DocComponent comp, string examplesDir = null, string destExamplesDir = null)
        {
            return GenerateDocusaurusComponentFile(componentsDir, imagesDir, comp, examplesDir, destExamplesDir);
        }

        private void GenerateVitePressConfig(string vpDir, string pluginName, string mainDescription, List<DocComponent> components)
        {
            string configPath = Path.Combine(vpDir, "config.js");
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("export default {");
            sb.AppendLine($"  title: '{pluginName} Documentation',");
            sb.AppendLine($"  description: '{mainDescription.Replace("'", "\\'").Replace("\r\n", " ").Replace("\n", " ")}',");
            sb.AppendLine("  themeConfig: {");
            sb.AppendLine("    nav: [");
            sb.AppendLine("      { text: 'Home', link: '/' }");
            sb.AppendLine("    ],");
            sb.AppendLine("    sidebar: [");
            sb.AppendLine("      {");
            sb.AppendLine("        text: 'Overview',");
            sb.AppendLine("        items: [");
            sb.AppendLine("          { text: 'Getting Started', link: '/' }");
            sb.AppendLine("        ]");
            sb.AppendLine("      },");
            sb.AppendLine("      {");
            sb.AppendLine("        text: 'Components',");
            sb.AppendLine("        items: [");

            var groupedComponents = components
                .GroupBy(c => string.IsNullOrWhiteSpace(c.SubCategory) ? "Uncategorized" : c.SubCategory)
                .OrderBy(g => g.Key);

            int groupCount = groupedComponents.Count();
            int currentGroup = 0;
            foreach (var group in groupedComponents)
            {
                currentGroup++;
                sb.AppendLine("          {");
                sb.AppendLine($"            text: '{group.Key}',");
                sb.AppendLine("            items: [");

                int compCount = group.Count();
                int currentComp = 0;
                foreach (var comp in group)
                {
                    currentComp++;
                    string safeName = MakeSafeFilename(comp.Name);
                    string trailingComma = (currentComp < compCount) ? "," : "";
                    sb.AppendLine($"              {{ text: '{comp.Name}', link: '/components/{safeName}' }}{trailingComma}");
                }

                sb.AppendLine("            ]");
                string groupTrailingComma = (currentGroup < groupCount) ? "," : "";
                sb.AppendLine($"          }}{groupTrailingComma}");
            }

            sb.AppendLine("        ]");
            sb.AppendLine("      }");
            sb.AppendLine("    ]");
            sb.AppendLine("  }");
            sb.AppendLine("}");

            File.WriteAllText(configPath, sb.ToString());
        }

        public string ExportSphinxSite(string baseDirectory, string pluginName, string mainDescription, List<DocComponent> components, string examplesDir = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(baseDirectory))
                    return "Error: Base directory path is empty.";

                string siteDir = baseDirectory;
                string docsDir = Path.Combine(siteDir, "docs");
                string componentsDir = Path.Combine(docsDir, "components");
                string imagesDir = Path.Combine(docsDir, "images");
                string destExamplesDir = Path.Combine(docsDir, "examples");

                CreateDirectories(siteDir, docsDir, componentsDir, imagesDir);

                GenerateSphinxConf(docsDir, pluginName);

                GenerateSphinxIndex(docsDir, pluginName, mainDescription, components);
                int successCount = 0;
                foreach (var comp in components)
                {
                    if (GenerateSphinxComponentFile(componentsDir, imagesDir, comp, examplesDir, destExamplesDir)) successCount++;
                }

                return $"Success: Exported Sphinx/ReadTheDocs site ({successCount}/{components.Count} components) to {siteDir}";
            }
            catch (Exception ex)
            {
                return $"Error during Sphinx export: {ex.Message}";
            }
        }

        private void GenerateSphinxConf(string docsDir, string pluginName)
        {
            string confPath = Path.Combine(docsDir, "conf.py");
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"project = '{pluginName}'");
            sb.AppendLine($"copyright = '{System.DateTime.Now.Year}, Authors'");
            sb.AppendLine("author = 'Authors'");
            sb.AppendLine("release = '1.0'");
            sb.AppendLine();
            sb.AppendLine("extensions = [");
            sb.AppendLine("    'sphinx_rtd_theme',");
            sb.AppendLine("]");
            sb.AppendLine();
            sb.AppendLine("templates_path = ['_templates']");
            sb.AppendLine("exclude_patterns = ['_build', 'Thumbs.db', '.DS_Store']");
            sb.AppendLine();
            sb.AppendLine("html_theme = 'sphinx_rtd_theme'");
            sb.AppendLine("html_static_path = ['_static']");

            File.WriteAllText(confPath, sb.ToString());
        }

        private void GenerateSphinxIndex(string docsDir, string pluginName, string mainDescription, List<DocComponent> components)
        {
            string indexPath = Path.Combine(docsDir, "index.rst");
            StringBuilder sb = new StringBuilder();

            string title = $"{pluginName} Documentation";
            sb.AppendLine(title);
            sb.AppendLine(new string('=', title.Length));
            sb.AppendLine();
            sb.AppendLine(mainDescription);
            sb.AppendLine();

            sb.AppendLine(".. toctree::");
            sb.AppendLine("   :maxdepth: 2");
            sb.AppendLine("   :caption: Components by Subcategory:");
            sb.AppendLine();

            var groupedComponents = components
                .GroupBy(c => string.IsNullOrWhiteSpace(c.SubCategory) ? "Uncategorized" : c.SubCategory)
                .OrderBy(g => g.Key);

            foreach (var group in groupedComponents)
            {
                foreach (var comp in group)
                {
                    string safeName = MakeSafeFilename(comp.Name);
                    sb.AppendLine($"   components/{safeName}");
                }
            }

            File.WriteAllText(indexPath, sb.ToString());
        }

        private bool GenerateSphinxComponentFile(string componentsDir, string imagesDir, DocComponent comp, string examplesDir = null, string destExamplesDir = null)
        {
            try
            {
                string safeName = MakeSafeFilename(comp.Name);
                string rstPath = Path.Combine(componentsDir, $"{safeName}.rst");

                StringBuilder sb = new StringBuilder();

                string title = comp.Name;
                sb.AppendLine(title);
                sb.AppendLine(new string('=', title.Length));
                sb.AppendLine();

                sb.AppendLine($"**Nickname:** {comp.NickName}  ");
                sb.AppendLine($"**Location:** {comp.Category} > {comp.SubCategory}  ");
                sb.AppendLine();

                if (comp.Icon != null)
                {
                    string iconFileName = $"{safeName}_icon.png";
                    string iconFilePath = Path.Combine(imagesDir, iconFileName);
                    comp.Icon.Save(iconFilePath, System.Drawing.Imaging.ImageFormat.Png);

                    sb.AppendLine($".. image:: ../images/{iconFileName}");
                    sb.AppendLine("   :width: 24 px");
                    sb.AppendLine("   :height: 24 px");
                    sb.AppendLine("   :align: middle");
                    sb.AppendLine();
                }

                sb.AppendLine("Description");
                sb.AppendLine("-----------");
                sb.AppendLine(comp.Description);
                sb.AppendLine();

                sb.AppendLine("Inputs");
                sb.AppendLine("------");
                if (comp.Inputs.Count == 0)
                {
                    sb.AppendLine("*None*");
                }
                else
                {
                    sb.AppendLine(".. list-table::");
                    sb.AppendLine("   :widths: 20 20 20 40");
                    sb.AppendLine("   :header-rows: 1");
                    sb.AppendLine();
                    sb.AppendLine("   * - Name");
                    sb.AppendLine("     - Type");
                    sb.AppendLine("     - Access");
                    sb.AppendLine("     - Description");

                    foreach (var input in comp.Inputs)
                    {
                        string desc = string.IsNullOrWhiteSpace(input.Description) ? "-" : input.Description.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
                        sb.AppendLine($"   * - **{input.Name}**");
                        sb.AppendLine($"     - ``{input.TypeName}``");
                        sb.AppendLine($"     - {input.Access}");
                        sb.AppendLine($"     - {desc}");
                    }
                }
                sb.AppendLine();

                sb.AppendLine("Outputs");
                sb.AppendLine("-------");
                if (comp.Outputs.Count == 0)
                {
                    sb.AppendLine("*None*");
                }
                else
                {
                    sb.AppendLine(".. list-table::");
                    sb.AppendLine("   :widths: 20 20 20 40");
                    sb.AppendLine("   :header-rows: 1");
                    sb.AppendLine();
                    sb.AppendLine("   * - Name");
                    sb.AppendLine("     - Type");
                    sb.AppendLine("     - Access");
                    sb.AppendLine("     - Description");

                    foreach (var output in comp.Outputs)
                    {
                        string desc = string.IsNullOrWhiteSpace(output.Description) ? "-" : output.Description.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
                        sb.AppendLine($"   * - **{output.Name}**");
                        sb.AppendLine($"     - ``{output.TypeName}``");
                        sb.AppendLine($"     - {output.Access}");
                        sb.AppendLine($"     - {desc}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(examplesDir) && !string.IsNullOrWhiteSpace(destExamplesDir))
                {
                    string exampleLink = CopyAndLinkExampleFile(comp, examplesDir, destExamplesDir, "rst");
                    if (!string.IsNullOrEmpty(exampleLink))
                    {
                        sb.AppendLine();
                        sb.AppendLine(exampleLink);
                    }
                }

                File.WriteAllText(rstPath, sb.ToString());
                return true;
            }
            catch
            {
                return false;
            }
        }
        private string CopyAndLinkExampleFile(DocComponent comp, string examplesDir, string destExamplesDir, string format)
        {
            if (string.IsNullOrWhiteSpace(examplesDir) || !Directory.Exists(examplesDir))
                return string.Empty;

            try
            {
                string[] files = Directory.GetFiles(examplesDir, "*.gh");
                string matchedFile = null;

                foreach (var file in files)
                {
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(file).Trim();
                    if (nameWithoutExt.Equals(comp.Name, StringComparison.OrdinalIgnoreCase) ||
                        nameWithoutExt.Equals(comp.NickName, StringComparison.OrdinalIgnoreCase) ||
                        nameWithoutExt.Equals(MakeSafeFilename(comp.Name), StringComparison.OrdinalIgnoreCase))
                    {
                        matchedFile = file;
                        break;
                    }
                }

                if (matchedFile != null)
                {
                    if (!Directory.Exists(destExamplesDir))
                    {
                        Directory.CreateDirectory(destExamplesDir);
                    }

                    string fileName = Path.GetFileName(matchedFile);
                    string destPath = Path.Combine(destExamplesDir, fileName);
                    File.Copy(matchedFile, destPath, true);

                    if (format == "rst")
                    {
                        return $"\nExample File\n------------\n📥 :download:`Download Example File ({fileName}) <../examples/{fileName}>`\n";
                    }
                    else if (format == "html")
                    {
                        return $"\n<h2>Example File</h2><p>📥 <a href=\"../examples/{fileName}\">Download Example File ({fileName})</a></p>\n";
                    }
                    else
                    {
                        return $"\n## Example File\n📥 [Download Example File ({fileName})](../examples/{fileName})\n";
                    }
                }
            }
            catch
            {
                // Fail silently
            }

            return string.Empty;
        }
    }
}
