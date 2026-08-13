using Microsoft.Data.Entity.Design.Common;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration;
using Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration.Generators;

namespace Microsoft.VisualStudio.Data.Entity.Design.CodeGeneration
{
    internal interface ICodeGeneratorFactory
    {
        IContextGenerator GetContextGenerator(LangEnum language, bool isEmptyModel);
        IEntityTypeGenerator GetEntityTypeGenerator(LangEnum language);
    }
}
