using System.Collections.Generic;
using System.Drawing;

namespace DocsHopper.Core
{
    public class DocParameter
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string TypeName { get; set; }
        public string Access { get; set; }
    }

    public class DocComponent
    {
        public string Name { get; set; }
        public string NickName { get; set; }
        public string Description { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }

        public Bitmap Icon { get; set; }

        public List<DocParameter> Inputs { get; set; } = new List<DocParameter>();
        public List<DocParameter> Outputs { get; set; } = new List<DocParameter>();
    }
}