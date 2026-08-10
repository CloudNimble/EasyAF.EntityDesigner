// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using Microsoft.Data.Entity.Design.Base.Context;
using Microsoft.Data.Tools.Dsl.ModelTranslator;

namespace Microsoft.Data.Entity.Design.EntityDesigner.CustomSerializer
{
    internal class ModelTranslatorContextItem : ContextItem
    {
        internal static ModelTranslator<BaseTranslatorStrategy> GetEntityModelTranslator(EditingContext context)
        {
            var translatorContextItem = context.Items.GetValue<ModelTranslatorContextItem>();
            translatorContextItem.Translator ??=
                    new ModelTranslator<BaseTranslatorStrategy>(new EntityModelToDslModelTranslatorStrategy(context));
            return translatorContextItem.Translator;
        }

        internal override Type ItemType
        {
            get { return typeof(ModelTranslatorContextItem); }
        }

        private ModelTranslator<BaseTranslatorStrategy> Translator { get; set; }
    }
}
