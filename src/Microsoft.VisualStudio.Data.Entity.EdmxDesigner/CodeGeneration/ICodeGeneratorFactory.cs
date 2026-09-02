using Microsoft.Data.Entity.Design.XmlEngine.Common;
using Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration.Generators;

namespace Microsoft.VisualStudio.Data.Entity.EdmxDesigner.CodeGeneration
{
    internal interface ICodeGeneratorFactory
    {
        IContextGenerator GetContextGenerator(LangEnum language, bool isEmptyModel);
        IEntityTypeGenerator GetEntityTypeGenerator(LangEnum language);
    }
}
