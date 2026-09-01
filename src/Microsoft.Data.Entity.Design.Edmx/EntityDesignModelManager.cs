// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using Microsoft.Data.Entity.Design.Edmx.Commands;
using Microsoft.Data.Entity.Design.Edmx.Validation;
using Microsoft.Data.Entity.Design.EntityFramework;
using Microsoft.Data.Entity.Design.XmlEngine.Model;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Commands;
using Microsoft.Data.Entity.Design.XmlEngine.Model.Validation;
using System.Xml.Linq;

namespace Microsoft.Data.Entity.Design.Edmx
{

    /// <summary>
    /// The Entity Designer's concrete <see cref="ModelManager" />, owning the <see cref="EFArtifact" /> instances
    /// that back the currently open EDMX documents.
    /// </summary>
    /// <remarks>
    /// <see cref="ModelManager" /> supplies the artifact cache, change-group routing, and the parse/normalize/resolve
    /// pipeline but leaves the EDMX-specific decisions abstract. This subclass fills those in: it validates attribute
    /// content against the EDMX schema for the artifact's version, walks up the EDMX tree to find the namespace that
    /// governs a node, and produces rename commands that understand Entity Designer naming rules. Because the base
    /// class receives its artifact and artifact-set factories through the constructor, the headless renderer can
    /// reuse this manager while substituting its own artifact type.
    /// </remarks>
    internal class EntityDesignModelManager : ModelManager
    {

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="EntityDesignModelManager" /> class.
        /// </summary>
        /// <param name="artifactFactory">Creates the <see cref="EFArtifact" /> instances loaded for a given URI.</param>
        /// <param name="artifactSetFactory">Creates the <see cref="EFArtifactSet" /> that groups mutually resolvable artifacts.</param>
        /// <remarks>
        /// The factories are the extensibility seam of the model manager: passing different implementations is what
        /// allows hosts other than Visual Studio (for example the headless renderer) to load their own artifact types
        /// without subclassing this manager.
        /// </remarks>
        internal EntityDesignModelManager(IEFArtifactFactory artifactFactory, IEFArtifactSetFactory artifactSetFactory)
            : base(artifactFactory, artifactSetFactory)
        {
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Creates the <see cref="RenameCommand" /> used to rename an item in an EDMX model.
        /// </summary>
        /// <param name="element">The normalizable item being renamed.</param>
        /// <param name="newName">The new name to assign to <paramref name="element" />.</param>
        /// <param name="uniquenessIsCaseSensitive">
        /// <see langword="true" /> if the new name only has to differ from its siblings by case to be considered unique;
        /// otherwise, <see langword="false" />.
        /// </param>
        /// <returns>An <see cref="EntityDesignRenameCommand" /> that performs the rename.</returns>
        /// <remarks>
        /// The base class only knows that some <see cref="RenameCommand" /> is required; this override supplies the
        /// Entity Designer flavor, which additionally repairs the bindings and designer-layer references that point at
        /// the renamed item.
        /// </remarks>
        internal override RenameCommand CreateRenameCommand(EFNormalizableItem element, string newName, bool uniquenessIsCaseSensitive)
        {
            return new EntityDesignRenameCommand(element, newName, uniquenessIsCaseSensitive);
        }

        /// <summary>
        /// Gets the validator used to check XML attribute values against the schema of the given artifact.
        /// </summary>
        /// <param name="artifact">The artifact whose attribute values are to be validated.</param>
        /// <returns>The <see cref="AttributeContentValidator" /> for the artifact's schema version.</returns>
        /// <remarks>
        /// The validator is selected by <see cref="EFArtifact.SchemaVersion" /> rather than cached per manager, because
        /// a single manager can hold artifacts targeting different Entity Framework versions and each version has its
        /// own set of legal attribute values.
        /// </remarks>
        internal override AttributeContentValidator GetAttributeContentValidator(EFArtifact artifact)
        {
            return EdmxAttributeContentValidator.GetInstance(artifact.SchemaVersion);
        }

        /// <summary>
        /// Retrieves the root "Schema" namespace that governs the given node.
        /// </summary>
        /// <param name="node">The node whose governing namespace is required.</param>
        /// <returns>
        /// The <see cref="XNamespace" /> declared by the nearest enclosing runtime-model or designer-info root, or
        /// <see langword="null" /> if the node is not contained by either.
        /// </returns>
        /// <remarks>
        /// An EDMX document interleaves several namespaces: the conceptual, storage, and mapping sections each sit
        /// under an <see cref="EFRuntimeModelRoot" />, while the diagram and designer settings sit under an
        /// <see cref="EFDesignerInfoRoot" />. The correct namespace for a node therefore cannot be inferred from the
        /// artifact alone, so this walks up the parent chain and stops at the first root of either kind. A node above
        /// both roots (or detached from the tree) legitimately has no namespace, hence the <see langword="null" /> result.
        /// </remarks>
        internal override XNamespace GetRootNamespace(EFObject node)
        {
            var currNode = node;
            EFRuntimeModelRoot runtimeModel = null;
            EFDesignerInfoRoot designerInfo = null;
            XNamespace ns = null;

            while (currNode is not null)
            {
                runtimeModel = currNode as EFRuntimeModelRoot;
                designerInfo = currNode as EFDesignerInfoRoot;

                if (runtimeModel is not null)
                {
                    ns = runtimeModel.XNamespace;
                    break;
                }
                else if (designerInfo is not null)
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

        /// <summary>
        /// Validates the artifacts in the given set and compiles their mappings, recording any errors on the set.
        /// </summary>
        /// <param name="artifactSet">The artifact set to validate.</param>
        /// <param name="doEscherValidation">
        /// <see langword="true" /> to run designer-level (Escher) validation before runtime validation;
        /// <see langword="false" /> to clear any existing designer-level errors instead.
        /// </param>
        /// <remarks>
        /// Validation runs in two layers. The designer layer catches problems the runtime cannot express, and when it
        /// is skipped its previous errors must be cleared and the artifact marked validity-dirty, otherwise stale
        /// errors would linger in the error list and the artifact would never be re-validated. The runtime layer is
        /// then targeted at the set's own <see cref="EFArtifactSet.SchemaVersion" /> so the model is checked against
        /// the Entity Framework version it actually declares. Runtime mapping validation is suppressed when the
        /// designer layer found errors severe enough to make the runtime's own diagnostics misleading, which is what
        /// <see cref="EntityDesignArtifactSet.ShouldDoRuntimeMappingValidation" /> reports.
        /// </remarks>
        internal void ValidateAndCompileMappings(EntityDesignArtifactSet artifactSet, bool doEscherValidation)
        {
            var artifact = artifactSet.GetEntityDesignArtifact();

            if (doEscherValidation)
            {
                EdmxModelValidator.ValidateEscherModel(artifactSet, false);
            }
            else
            {
                EdmxModelValidator.ClearErrors(artifactSet);
                artifact?.SetValidityDirtyForErrorClass(ErrorClass.Escher_All, true);
            }

            // validate using the schema version of this artifact as the target Entity Framework version
            new RuntimeMetadataValidator(this, artifactSet.SchemaVersion, DependencyResolver.Instance)
                .ValidateAndCompileMappings(artifactSet, artifactSet.ShouldDoRuntimeMappingValidation());
        }

        #endregion

    }

}
