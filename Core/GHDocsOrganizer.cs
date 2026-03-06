using System;
using System.Collections.Generic;
using Grasshopper.Kernel;

namespace DocsHopper.Core
{
    public class DocsHopperOrganizer
    {
        private readonly DocsHopperReader _reader;

        public DocsHopperOrganizer(DocsHopperReader reader)
        {
            _reader = reader;
        }

        public List<DocComponent> OrganizeComponents(List<IGH_ObjectProxy> proxies)
        {
            List<DocComponent> structuredDocs = new List<DocComponent>();

            foreach (var proxy in proxies)
            {
                IGH_Component instance = _reader.InstantiateComponent(proxy);

                if (instance == null) continue;

                DocComponent docComp = new DocComponent
                {
                    Name = instance.Name,
                    NickName = instance.NickName,
                    Description = instance.Description,
                    Category = instance.Category,
                    SubCategory = instance.SubCategory,
                    Icon = instance.Icon_24x24
                };

                foreach (var input in instance.Params.Input)
                {
                    docComp.Inputs.Add(new DocParameter
                    {
                        Name = input.Name,
                        Description = input.Description,
                        TypeName = input.TypeName,
                        Access = input.Access.ToString()
                    });
                }

                foreach (var output in instance.Params.Output)
                {
                    docComp.Outputs.Add(new DocParameter
                    {
                        Name = output.Name,
                        Description = output.Description,
                        TypeName = output.TypeName,
                        Access = output.Access.ToString()
                    });
                }

                structuredDocs.Add(docComp);
            }

            return structuredDocs;
        }
    }
}