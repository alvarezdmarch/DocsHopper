using System;
using System.Collections.Generic;
using DocsHopper.Core;
using DocsHopper.Properties;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DocsHopper.Components.Customize
{
    public class CreateSectionComponent : GH_Component
    {
        public CreateSectionComponent()
          : base("Create Homepage Section", "HSection", "Creates a custom text/image section for the Advanced MkDocs homepage.", "DocsHopper", "Customize")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Section Title", "ST", "The header for this section.", GH_ParamAccess.item);
            pManager.AddTextParameter("Content Text", "CT", "The paragraph explaining the template or workflow.", GH_ParamAccess.item);
            pManager.AddTextParameter("Image Filepath", "IMG", "Optional: Path to a local screenshot (.png, .jpg).", GH_ParamAccess.item);
            pManager[2].Optional = true;
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Section Data", "SEC", "Section data to plug into the Advanced Exporter component.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string title = string.Empty;
            string content = string.Empty;
            string imagePath = null;

            if (!DA.GetData(0, ref title)) return;
            if (!DA.GetData(1, ref content)) return;
            DA.GetData(2, ref imagePath);

            DocSection section = new DocSection
            {
                Title = title,
                Content = content,
                ImagePath = imagePath
            };

            DA.SetData(0, section);
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.createHomepageSection;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("168E550E-2856-486D-A7B2-D23911739C41"); }
        }
    }
}