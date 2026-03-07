using System;
using System.Collections.Generic;
using System.Drawing;
using DocsHopper.Properties;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace DocsHopper.Components.Customize
{
    public class CustomCssComponent : GH_Component
    {
        public CustomCssComponent()
          : base("Custom CSS", "CustomCSS", "Creates a custom CSS style string for the HTML documentation.", "DocsHopper", "Customize")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddColourParameter("Primary Color", "PC", "Main accent color (links, headers). Default: RBG(217, 76, 26).", GH_ParamAccess.item, Color.FromArgb(217, 76, 26));
            pManager.AddColourParameter("Background Color", "BG", "Site background color. Default: RBG(248, 250, 252).", GH_ParamAccess.item, Color.FromArgb(248, 250, 252));
            pManager.AddColourParameter("Text Color", "TXT", "Main text color. Default: RBG(30, 41, 59).", GH_ParamAccess.item, Color.FromArgb(30, 41, 59));
            pManager.AddColourParameter("Border Color", "BRD", "Table and divider border color. Default: RBG(226, 232, 240).", GH_ParamAccess.item, Color.FromArgb(226, 232, 240));
            pManager.AddTextParameter("Font Family", "FNT", "CSS Font family stack", GH_ParamAccess.item, "'Segoe UI', system-ui, sans-serif");
            pManager.AddIntegerParameter("Max Width", "W", "Maximum width of the container in pixels", GH_ParamAccess.item, 900);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("CSS String", "CSS", "The generated CSS string. Plug this into the HTML Exporter.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Color primary = Color.Empty;
            Color bg = Color.Empty;
            Color text = Color.Empty;
            Color border = Color.Empty;
            string font = string.Empty;
            int width = 900;

            DA.GetData(0, ref primary);
            DA.GetData(1, ref bg);
            DA.GetData(2, ref text);
            DA.GetData(3, ref border);
            DA.GetData(4, ref font);
            DA.GetData(5, ref width);

            string pHex = ColorToHex(primary);
            string bgHex = ColorToHex(bg);
            string tHex = ColorToHex(text);
            string bHex = ColorToHex(border);

            string customCss = $@"
:root {{ --primary: {pHex}; --bg: {bgHex}; --text: {tHex}; --border: {bHex}; }}
body {{ font-family: {font}; margin: 0; padding: 0; background: var(--bg); color: var(--text); line-height: 1.6; }}
.container {{ max-width: {width}px; margin: 0 auto; padding: 2rem; background: white; min-height: 100vh; box-shadow: 0 4px 6px -1px rgba(0,0,0,0.1); }}
h1, h2, h3 {{ color: var(--text); border-bottom: 1px solid var(--border); padding-bottom: 0.5rem; }}
h1 {{ color: var(--primary); border-bottom: none; }}
table {{ width: 100%; border-collapse: collapse; margin: 1.5rem 0; font-size: 0.95rem; }}
th, td {{ padding: 12px; text-align: left; border-bottom: 1px solid var(--border); }}
th {{ background-color: #f1f5f9; font-weight: 600; color: #475569; }}
tr:hover {{ background-color: #f8fafc; }}
a {{ color: var(--primary); text-decoration: none; font-weight: 500; }}
a:hover {{ text-decoration: underline; }}
.nav {{ margin-bottom: 2rem; padding-bottom: 1rem; border-bottom: 1px solid var(--border); font-size: 0.9rem; }}
.meta {{ color: #64748b; font-size: 0.9rem; margin-bottom: 2rem; }}
.icon {{ vertical-align: middle; margin-right: 10px; border-radius: 4px; }}
code {{ background: #f1f5f9; padding: 2px 6px; border-radius: 4px; font-family: 'Courier New', monospace; font-size: 0.9em; }}";

            DA.SetData(0, customCss);
        }

        private string ColorToHex(System.Drawing.Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.customCSS;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("95ADF79A-0DEF-48BC-A853-ABBF34ABF8E5"); }
        }
    }
}