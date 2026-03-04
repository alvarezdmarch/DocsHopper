using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace GHDocs
{
    public class GHDocsInfo : GH_AssemblyInfo
    {
        public override string Name => "GHDocs";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("4d12639f-7f82-41bd-b257-ee282823b4ce");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";

        //Return a string representing the version.  This returns the same version as the assembly.
        public override string AssemblyVersion => GetType().Assembly.GetName().Version.ToString();
    }
}