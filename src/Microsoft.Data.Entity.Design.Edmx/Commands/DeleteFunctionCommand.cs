// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Entity;
using Microsoft.Data.Entity.Design.Edmx.Mapping;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using System;
using System.Diagnostics;
using System.Linq;

namespace Microsoft.Data.Entity.Design.Edmx.Commands
{
    internal class DeleteFunctionCommand : DeleteEFElementCommand
    {
        internal string DeletedFunctionName { get; private set; }
        internal string DeletedFunctionFunctionImportName { get; private set; }

        protected Function Function
        {
            get
            {
                Function elem = EFElement as Function;
                Debug.Assert(elem != null, "elem != null");
                if (elem == null)
                {
                    throw new InvalidModelItemException();
                }
                return elem;
            }
        }

        /// <summary>
        ///     Delete the passed in function
        /// </summary>
        /// <param name="fim"></param>
        internal DeleteFunctionCommand(Function f)
            : base(f)
        {
            CommandValidation.ValidateFunction(f);
        }

        public DeleteFunctionCommand(Func<Command, CommandProcessorContext, bool> bindingAction)
            : base(bindingAction)
        {
        }

        protected override void PreInvoke(CommandProcessorContext cpc)
        {
            // Save off the deleted function name and function import names
            DeletedFunctionName = Function.Name.Value;
            var fim = Function.GetAntiDependenciesOfType<FunctionImportMapping>().FirstOrDefault();
            if (fim != null
                && fim.FunctionImportName.Target != null)
            {
                DeletedFunctionFunctionImportName = fim.FunctionImportName.Target.Name.Value;
            }

            base.PreInvoke(cpc);
        }

        protected override void InvokeInternal(CommandProcessorContext cpc)
        {
            // if we can't find a Function, don't worry about it.
            // don't use local typed property as this may be null
            if (EFElement != null)
            {
                base.InvokeInternal(cpc);
            }
        }
    }
}
