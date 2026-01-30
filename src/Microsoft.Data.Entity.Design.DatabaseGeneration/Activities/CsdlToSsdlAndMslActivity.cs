// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Activities;
using System.Activities.Hosting;
using System.Collections.Generic;
using System.Data.Entity.Core.Metadata.Edm;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Data.Entity.Design.DatabaseGeneration.OutputGenerators;
using Microsoft.Data.Entity.Design.DatabaseGeneration.Properties;
using Microsoft.Data.Entity.Design.VersioningFacade;

namespace Microsoft.Data.Entity.Design.DatabaseGeneration.Activities
{
    /// <summary>
    ///     A Windows Workflow activity that generates a storage model and mapping information based on a conceptual model.
    /// </summary>
    public sealed class CsdlToSsdlAndMslActivity : OutputGeneratorActivity
    {
        /// <summary>
        ///     A Windows Workflow <see cref="InArgument{T}" /> that specifies the conceptual schema definition language (CSDL) from
        ///     which store schema definition language (SSDL) and mapping specification language (MSL) are generated.
        /// </summary>
        public InArgument<EdmItemCollection> CsdlInput { get; set; }

        /// <summary>
        ///     The assembly-qualified name of the type used to generate mapping specification language (MSL)
        ///     from the conceptual schema definition language (CSDL) in the CsdlInput property.
        /// </summary>
        public InArgument<string> MslOutputGeneratorType { get; set; }

        /// <summary>
        ///     A Windows Workflow <see cref="OutArgument{T}" /> that specifies the store schema language definition (SSDL)
        ///     generated from conceptual schema definition language (CSDL) in the CsdlInput property.
        /// </summary>
        public OutArgument<string> SsdlOutput { get; set; }

        /// <summary>
        ///     A Windows Workflow <see cref="OutArgument{T}" /> that specifies the mapping specification language (MSL)
        ///     generated from conceptual schema definition language (CSDL) in the CsdlInput property.
        /// </summary>
        public OutArgument<string> MslOutput { get; set; }

        /// <summary>
        ///     Generates output that is supplied to the specified NativeActivityContext based on input specified in the NativeActivityContext.
        /// </summary>
        /// <param name="context"> The state of the current activity. </param>
        protected override void Execute(NativeActivityContext context)
        {
            var edmItemCollection = CsdlInput.Get(context);
            if (edmItemCollection == null)
            {
                throw new InvalidOperationException(Resources.ErrorCouldNotFindCSDL);
            }

            var symbolResolver = context.GetExtension<SymbolResolver>();
            if (symbolResolver[typeof(EdmParameterBag).Name] is not EdmParameterBag edmParameterBag)
            {
                throw new InvalidOperationException(Resources.ErrorNoEdmParameterBag);
            }

            // Find the TargetVersion parameter
            var targetFrameworkVersion = edmParameterBag.GetParameter<Version>(EdmParameterBag.ParameterName.TargetVersion);
            if (targetFrameworkVersion == null)
            {
                throw new InvalidOperationException(
                    String.Format(
                        CultureInfo.CurrentCulture, Resources.ErrorNoParameterDefined,
                        EdmParameterBag.ParameterName.TargetVersion.ToString()));
            }

            // Validate the TargetVersion parameter
            if (false == EntityFrameworkVersion.IsValidVersion(targetFrameworkVersion))
            {
                throw new InvalidOperationException(
                    String.Format(CultureInfo.CurrentCulture, Resources.ErrorNonValidTargetVersion, targetFrameworkVersion));
            }

            // Construct the Code View inputs in a dictionary 
            Dictionary<string, object> inputs = new Dictionary<string, object>
                {
                    { EdmConstants.csdlInputName, edmItemCollection }
                };

            // Process the SSDL and MSL code views, feeding in the CSDL
            var ssdl = ProcessOutputGenerator<string>(OutputGeneratorType.Get(context), context, inputs);
            var msl = ProcessOutputGenerator<string>(MslOutputGeneratorType.Get(context), context, inputs);

            // Validate the SSDL, but catch any naming errors and throw a friendlier one
            var ssdlCollection = EdmExtension.CreateAndValidateStoreItemCollection(
                ssdl, targetFrameworkVersion, DependencyResolver.Instance, true);

#if DEBUG
            // Validate the MSL in Debug mode
            EdmExtension.CreateStorageMappingItemCollection(
                edmItemCollection, ssdlCollection, msl, out IList<EdmSchemaError> mslErrors);
            if (mslErrors != null
                && mslErrors.Count > 0)
            {
                var errorSb = new StringBuilder();
                errorSb.AppendLine("Encountered the following errors while validating the MSL:");
                foreach (var error in mslErrors)
                {
                    errorSb.AppendLine(error.Message);
                }

                Debug.Fail(errorSb.ToString());
            }
#endif

            // We are done processing, save off all the outputs for the next stage
            SsdlOutput.Set(context, ssdl);
            MslOutput.Set(context, msl);
        }
    }
}
