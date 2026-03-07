using System;
using System.Collections.Generic;
using System.IO;
using DocsHopper.Core;
using DocsHopper.Properties;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DocsHopper.Components.Deploy
{
    public class DeployMKDocsComponent : GH_Component
    {
        public DeployMKDocsComponent()
          : base("Deploy MkDocs", "DeployMkD", "Creates a .yml file and pushes the repo to GitHub to trigger GitHub Pages.", "DocsHopper", "Deploy")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Documentation Directory", "DD", "Generated documentation folder path (Git repository root).", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run Deploy", "Run", "Set to true to generate the action and push to GitHub", GH_ParamAccess.item, false);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Status", "S", "Deployment status message", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string documentationDirectory = string.Empty;
            bool runDeploy = false;

            if (!DA.GetData(0, ref documentationDirectory)) return;
            if (!DA.GetData(1, ref runDeploy)) return;

            if (!runDeploy)
            {
                DA.SetData(0, "Waiting for Run toggle...");
                return;
            }

            if (!Directory.Exists(documentationDirectory))
            {
                DA.SetData(0, "Error: The specified directory does not exist.");
                return;
            }

            DocsHopperDeployer deployer = new DocsHopperDeployer();

            if(!Directory.Exists(Path.Combine(documentationDirectory, ".git")))
            {
                DA.SetData(0, "Error: The specified directory is not a Git repository.");
                return;
            }

            string workflowStatus = deployer.GenerateGitHubWorkflow(documentationDirectory);

            string defaultCommitMessage = "Auto-deployed MkDocs documentation via DocsHopper";
            string gitStatus = deployer.CommitAndPush(documentationDirectory, defaultCommitMessage);

            DA.SetData(0, $"{workflowStatus}\n{gitStatus}");
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.deployGitHubSites;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("0C0975B9-BD5F-4E31-93A7-E892A3B3EABB"); }
        }
    }
}