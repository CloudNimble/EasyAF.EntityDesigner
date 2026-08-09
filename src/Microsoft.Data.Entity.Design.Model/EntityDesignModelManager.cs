// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Xml.Linq;
using Microsoft.Data.Entity.Design.Model.Commands;
using Microsoft.Data.Entity.Design.Model.Validation;
using Microsoft.Data.Entity.Design.VersioningFacade;

namespace Microsoft.Data.Entity.Design.Model
{
    /// <summary>
    ///     Class that contains ado.net metadata and information about the metadata source representation, in this case xml source.
    /// </summary>
    internal class EntityDesignModelManager : ModelManager
    {
        internal EntityDesignModelManager(IEFArtifactFactory artifactFactory, IEFArtifactSetFactory artifactSetFactory)
            : base(artifactFactory, artifactSetFactory)
        {
        }

        internal override AttributeContentValidator GetAttributeContentValidator(EFArtifact artifact)
        {
            return EscherAttributeContentValidator.GetInstance(artifact.SchemaVersion);
        }

        /// <summary>
        ///     Retrieve the root "Schema" node for current node.  If there is no root mapping node, this will return null.
        /// </summary>
        internal override XNamespace GetRootNamespace(EFObject node)
        {
            var currNode = node;
            EFRuntimeModelRoot runtimeModel = null;
            EFDesignerInfoRoot designerInfo = null;
            XNamespace ns = null;

            while (currNode != null)
            {
                runtimeModel = currNode as EFRuntimeModelRoot;
                designerInfo = currNode as EFDesignerInfoRoot;

                if (runtimeModel != null)
                {
                    ns = runtimeModel.XNamespace;
                    break;
                }
                else if (designerInfo != null)
                {
                    ns = designerInfo.XNamespace;
                    break;
                }
                else
                {
                    currNode = currNode.Parent;
                }
            }

            return ns;
        }

        internal override RenameCommand CreateRenameCommand(EFNormalizableItem element, string newName, bool uniquenessIsCaseSensitive)
        {
            return new EntityDesignRenameCommand(element, newName, uniquenessIsCaseSensitive);
        }

        internal void ValidateAndCompileMappings(EntityDesignArtifactSet artifactSet, bool doEscherValidation)
        {
            var artifact = artifactSet.GetEntityDesignArtifact();

            if (doEscherValidation)
            {
                EscherModelValidator.ValidateEscherModel(artifactSet, false);
            }
            else
            {
                EscherModelValidator.ClearErrors(artifactSet);
                artifact?.SetValidityDirtyForErrorClass(ErrorClass.Escher_All, true);
            }

            // validate using the schema version of this artifact as the target Entity Framework version
            new RuntimeMetadataValidator(this, artifactSet.SchemaVersion, DependencyResolver.Instance)
                .ValidateAndCompileMappings(artifactSet, artifactSet.ShouldDoRuntimeMappingValidation());
        }
    }
}
