using Microsoft.Data.Entity.Design.Common;

namespace Microsoft.Data.Entity.Design.CodeGeneration
{
    internal interface ICodeGeneratorFactory
    {
        IContextGenerator GetContextGenerator(LangEnum language, bool isEmptyModel);
        IEntityTypeGenerator GetEntityTypeGenerator(LangEnum language);
    }
}
