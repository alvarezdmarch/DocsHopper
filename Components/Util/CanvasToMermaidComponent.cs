using System;
using System.Collections.Generic;
using System.Text;
using DocsHopper.Properties;
using Grasshopper;
using Grasshopper.Kernel;

namespace DocsHopper.Components.Util
{
    public class CanvasToMermaidComponent : GH_Component
    {
        public CanvasToMermaidComponent()
          : base("Canvas to Mermaid", "GH2Mermaid", "Generates Mermaid.js flowchart syntax representing the active Grasshopper canvas layout and connections.", "DocsHopper", "Util")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Include Parameters", "IP", "If true, renders detailed parameter-level connections. If false, renders component-to-component connections.", GH_ParamAccess.item, false);
            pManager[0].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("Mermaid Code", "M", "The generated Mermaid.js flowchart code", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool includeParams = false;
            DA.GetData(0, ref includeParams);

            GH_Document doc = Instances.ActiveCanvas?.Document;
            if (doc == null)
            {
                DA.SetData(0, "Error: No active Grasshopper document found.");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("flowchart TD");

            HashSet<string> declaredNodes = new HashSet<string>();
            HashSet<string> edges = new HashSet<string>();

            if (!includeParams)
            {
                foreach (var obj in doc.Objects)
                {
                    if (obj is IGH_Component comp)
                    {
                        string id = "c_" + comp.InstanceGuid.ToString("N");
                        string name = EscapeMermaidLabel(comp.Name);
                        sb.AppendLine($"    {id}[\"{name} ({comp.NickName})\"]");

                        foreach (var input in comp.Params.Input)
                        {
                            foreach (var source in input.Sources)
                            {
                                var sourceObj = source.Attributes?.GetTopLevel?.DocObject;
                                if (sourceObj != null && sourceObj != comp)
                                {
                                    string sourceId = "c_" + sourceObj.InstanceGuid.ToString("N");
                                    if (sourceObj is IGH_Param && sourceObj.Attributes?.Parent == null)
                                    {
                                        sourceId = "p_" + sourceObj.InstanceGuid.ToString("N");
                                    }
                                    edges.Add($"    {sourceId} --> {id}");
                                }
                            }
                        }
                    }
                    else if (obj is IGH_Param param && param.Attributes?.Parent == null)
                    {
                        string id = "p_" + param.InstanceGuid.ToString("N");
                        string name = EscapeMermaidLabel(param.Name);
                        sb.AppendLine($"    {id}(\"{name}\")");

                        foreach (var source in param.Sources)
                        {
                            var sourceObj = source.Attributes?.GetTopLevel?.DocObject;
                            if (sourceObj != null && sourceObj != param)
                            {
                                string sourceId = "c_" + sourceObj.InstanceGuid.ToString("N");
                                if (sourceObj is IGH_Param && sourceObj.Attributes?.Parent == null)
                                {
                                    sourceId = "p_" + sourceObj.InstanceGuid.ToString("N");
                                }
                                edges.Add($"    {sourceId} --> {id}");
                            }
                        }
                    }
                }
            }
            else
            {
                foreach (var obj in doc.Objects)
                {
                    if (obj is IGH_Component comp)
                    {
                        string compId = "sub_" + comp.InstanceGuid.ToString("N");
                        string compName = EscapeMermaidLabel(comp.Name);

                        sb.AppendLine($"    subgraph {compId} [\"{compName} ({comp.NickName})\"]");

                        foreach (var input in comp.Params.Input)
                        {
                            string paramId = "param_" + input.InstanceGuid.ToString("N");
                            string name = EscapeMermaidLabel(input.Name);
                            sb.AppendLine($"        {paramId}[\"{name} (In)\"]");
                        }

                        foreach (var output in comp.Params.Output)
                        {
                            string paramId = "param_" + output.InstanceGuid.ToString("N");
                            string name = EscapeMermaidLabel(output.Name);
                            sb.AppendLine($"        {paramId}[\"{name} (Out)\"]");
                        }

                        sb.AppendLine("    end");

                        foreach (var input in comp.Params.Input)
                        {
                            string inputParamId = "param_" + input.InstanceGuid.ToString("N");
                            foreach (var source in input.Sources)
                            {
                                string sourceParamId = "param_" + source.InstanceGuid.ToString("N");
                                edges.Add($"    {sourceParamId} --> {inputParamId}");
                            }
                        }
                    }
                    else if (obj is IGH_Param param && param.Attributes?.Parent == null)
                    {
                        string paramId = "param_" + param.InstanceGuid.ToString("N");
                        string name = EscapeMermaidLabel(param.Name);
                        sb.AppendLine($"    {paramId}((\"{name}\"))");

                        foreach (var source in param.Sources)
                        {
                            string sourceParamId = "param_" + source.InstanceGuid.ToString("N");
                            edges.Add($"    {sourceParamId} --> {paramId}");
                        }
                    }
                }
            }

            foreach (var edge in edges)
            {
                sb.AppendLine(edge);
            }

            DA.SetData(0, sb.ToString());
        }

        private string EscapeMermaidLabel(string label)
        {
            if (string.IsNullOrEmpty(label)) return string.Empty;
            return label.Replace("\"", "\\\"").Replace("[", "(").Replace("]", ")").Replace("{", "(").Replace("}", ")");
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return Resources.visualizeMkDocs;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("F5E6437B-7360-4965-A2B0-F3E745DF7A31"); }
        }
    }
}
