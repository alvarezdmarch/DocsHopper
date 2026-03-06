using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DocsHopper.Core
{
    public class DocsHopperDeployer
    {
        public string GenerateGitHubWorkflow(string repoDir)
        {
            try
            {
                string workflowsDir = Path.Combine(repoDir, ".github", "workflows");
                if (!Directory.Exists(workflowsDir))
                {
                    Directory.CreateDirectory(workflowsDir);
                }

                StringBuilder yaml = new StringBuilder();
                yaml.AppendLine("name: Deploy MkDocs");
                yaml.AppendLine("on:");
                yaml.AppendLine("  push:");
                yaml.AppendLine("    branches:");
                yaml.AppendLine("      - main");
                yaml.AppendLine("      - master");
                yaml.AppendLine("permissions:");
                yaml.AppendLine("  contents: write");
                yaml.AppendLine("jobs:");
                yaml.AppendLine("  deploy:");
                yaml.AppendLine("    runs-on: ubuntu-latest");
                yaml.AppendLine("    steps:");
                yaml.AppendLine("      - uses: actions/checkout@v4");
                yaml.AppendLine("      - name: Set up Python");
                yaml.AppendLine("        uses: actions/setup-python@v5");
                yaml.AppendLine("        with:");
                yaml.AppendLine("          python-version: '3.x'");
                yaml.AppendLine("      - name: Install dependencies");
                yaml.AppendLine("        run: pip install mkdocs-material");
                yaml.AppendLine("      - name: Deploy to GitHub Pages");
                yaml.AppendLine("        run: mkdocs gh-deploy --force");

                string yamlPath = Path.Combine(workflowsDir, "deploy-docs.yml");
                File.WriteAllText(yamlPath, yaml.ToString());

                return $"Workflow generated at: {yamlPath}";
            }
            catch (Exception ex)
            {
                return $"Error generating workflow: {ex.Message}";
            }
        }

        public string CommitAndPush(string repoDir, string commitMsg)
        {
            try
            {
                string command = $"git add . & git commit -m \"{commitMsg}\" & git push & pause";

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    WorkingDirectory = repoDir,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Normal
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    process.WaitForExit();
                }

                return "Git deployment process finished.";
            }
            catch (Exception ex)
            {
                return $"Git Error: {ex.Message}";
            }
        }
    }
}