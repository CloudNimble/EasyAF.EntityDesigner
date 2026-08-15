// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.Data.Entity.Design.Model;
using Microsoft.Data.Entity.Design.Model.Entity;
using Microsoft.Data.Entity.Design.Model.Mapping;
using Microsoft.VisualStudio.Modeling.Diagrams;

namespace Microsoft.Data.Entity.Design.Dsl.View
{
    /// <summary>
    ///     Sets the focus on the most appropriate shape for a given <see cref="EFObject" /> within one diagram.
    /// </summary>
    /// <remarks>
    ///     This is diagram logic and knows nothing about windows, frames, or document views. Choosing which diagram
    ///     to navigate belongs to the host, which walks its open views and calls
    ///     <see cref="NavigateToNodeInDiagram" /> on each until one reports a match. See
    ///     specs/platform-independence.md.
    /// </remarks>
    internal static class DiagramNavigator
    {
        /// <summary>
        ///     Sets the focus on the most appropriate shape for the given <see cref="EFObject" /> and diagram. The
        ///     object is assumed to be either a C-Space node or an M-Space node.
        /// </summary>
        /// <param name="diagram">The diagram to search.</param>
        /// <param name="efobject">The object to navigate to.</param>
        /// <returns><see langword="true" /> if a matching shape was found and selected.</returns>
        internal static bool NavigateToNodeInDiagram(EntityDesignerSurface diagram, EFObject efobject)
        {
            var foundDSLElementMatchInDiagram = false;

            // find the model parent (if this is a c-space object)

            // by default, we assume that this our c-space object
            var cspaceEFObject = efobject;
            EFObject mspaceEFObject = null;
            bool? usesFunctionMapping = null;
            if (efobject.GetParentOfType(typeof(ConceptualEntityModel)) is not ConceptualEntityModel cModel)
            {
                MappingModel mModel = efobject.GetParentOfType(typeof(MappingModel)) as MappingModel;
                Debug.Assert(mModel != null, "efobject is neither in c-space or s-space");

                // if this is a mapping node, then we want to find the closest corresponding c-space node
                // to which this mapping node is mapped, and set the focus on that.
                cspaceEFObject = GetCSpaceEFObjectForMSpaceEFObject(efobject, out usesFunctionMapping);
                mspaceEFObject = efobject;
            }

            // navigate to the shape in the DSL designer
            DiagramItemCollection diagramItemCollection = new DiagramItemCollection();
            RetrieveDiagramItemCollectionForEFObject(diagram, cspaceEFObject, diagramItemCollection);
            if (diagram != null
                && diagramItemCollection.Count > 0)
            {
                diagram.Show();

                if (diagram.ActiveDiagramView != null)
                {
                    diagram.ActiveDiagramView.Focus();
                    diagram.ActiveDiagramView.Selection.Set(diagramItemCollection);
                    diagram.EnsureSelectionVisible();
                }
                else
                {
                    // If no active view exists, do the following:
                    // - Set the selection on the first associated views (if any).
                    // - Set InitialSelectionDIagramItemSelectionProperty to prevent the first EntityTypeShape to be selected (default behavior)
                    //   This case can happen when the diagram is not initialized or is not fully rendered.
                    diagram.InitialDiagramItemSelection = diagramItemCollection;
                    if (diagram.ClientViews != null
                        && diagram.ClientViews.Count > 0)
                    {
                        foreach (DiagramClientView clientView in diagram.ClientViews)
                        {
                            clientView.Selection.Set(diagramItemCollection);
                            clientView.Selection.EnsureVisible(DiagramClientView.EnsureVisiblePreferences.ScrollIntoViewCenter);
                            break;
                        }
                    }
                }
                foundDSLElementMatchInDiagram = true;
            }

            // tell the host we landed on a mapping element, so anything showing mapping details can follow
            if (mspaceEFObject != null)
            {
                diagram?.OnMappingDetailsNavigationRequested(mspaceEFObject, usesFunctionMapping);
            }

            return foundDSLElementMatchInDiagram;
        }

        /// <summary>
        ///     Given a efobject in m-space (ie, it is defined in the mapping section of the edmx file), find the
        ///     closest object in the c-space (ie, defined in the conceptual schema).  We do this by looking
        ///     for binding objects of the current node bound to something in c-space. If there is no such object, we
        ///     recursively call this on efobject's parent.
        /// </summary>
        /// <param name="mspaceEFObject">The m-space object to resolve.</param>
        /// <param name="usesFunctionMapping">
        ///     Receives whether the object maps through modification functions rather than tables, or
        ///     <see langword="null" /> when the object says nothing either way.
        /// </param>
        /// <returns>The closest corresponding c-space object, or <see langword="null" /> if there is none.</returns>
        private static EFObject GetCSpaceEFObjectForMSpaceEFObject(EFObject mspaceEFObject, out bool? usesFunctionMapping)
        {
            EFObject cspaceEFObject = null;
            var o = mspaceEFObject;
            usesFunctionMapping = null;

            // see if this m-space object has a parent of an association set mapping.
            if (mspaceEFObject.GetParentOfType(typeof(AssociationSetMapping)) is AssociationSetMapping asm)
            {
                var associationSet = asm.Name.Target;
                if (associationSet != null)
                {
                    var association = associationSet.Association.Target;
                    if (association != null)
                    {
                        Debug.Assert(
                            association.RuntimeModelRoot() is ConceptualEntityModel, "Expected association to be in C-space, but it is not!");
                        return association;
                    }
                }
            }

            // see if this is a node that requires the function view in the mapping pane
            usesFunctionMapping = mspaceEFObject.GetParentOfType(typeof(ModificationFunctionMapping)) is ModificationFunctionMapping;

            // default case, walk up the model looking for node that has a binding bound to something in c-space.  
            while (cspaceEFObject == null
                   && o != null)
            {
                if (o is ItemBinding binding)
                {
                    // see if this binding is bound to something in c-space
                    cspaceEFObject = GetCSpaceObjectFromBinding(binding);
                }
                else if (o is EFContainer container)
                {
                    // see if any direct children are bindings bound to something in c-space
                    foreach (var child in container.Children)
                    {
                        // check every binding to see if it is bound to seomthing in cspace
                        if (child is ItemBinding b)
                        {
                            cspaceEFObject = GetCSpaceObjectFromBinding(b);
                            if (cspaceEFObject != null)
                            {
                                // break out of the for loop
                                break;
                            }
                        }
                    }
                }

                if (cspaceEFObject == null)
                {
                    o = o.Parent;
                }
            }
            return cspaceEFObject;
        }

        /// <summary>
        ///     Given an item binding, return the target of the binding if it is mapped to something in c-space
        /// </summary>
        /// <param name="itemBinding"></param>
        /// <returns></returns>
        private static EFObject GetCSpaceObjectFromBinding(ItemBinding itemBinding)
        {
            foreach (EFObject dep in itemBinding.ResolvedTargets)
            {
                if (dep.RuntimeModelRoot() is ConceptualEntityModel cModel)
                {
                    return dep;
                }
            }
            return null;
        }

        /// <summary>
        ///     Given an EFObject, return collection of DiagramItems for it.
        /// </summary>
        private static void RetrieveDiagramItemCollectionForEFObject(
            EntityDesignerSurface diagram, EFObject efobject, DiagramItemCollection diagramItemCollection)
        {
            if (efobject == null)
            {
                return;
            }


            if (efobject.RuntimeModelRoot() is not ConceptualEntityModel cModel)
            {
                // this either isn't a c-space object, or it is the ConceptualEntityModel node, so just return null
                return;
            }

            // if this is a child element of the association, return the diagram item for the association
            if (!(efobject is Association))
            {
                if (efobject.GetParentOfType(typeof(Association)) is Association association)
                {
                    RetrieveDiagramItemCollectionForEFObject(diagram, association, diagramItemCollection);
                    return;
                }
            }

            if (efobject is Association)
            {
                var shapeElement = GetDesignerShapeElementForEFObject(diagram, efobject);
                if (shapeElement != null)
                {
                    diagramItemCollection.Add(new DiagramItem(shapeElement));
                    return;
                }
            }
            else if (efobject is NavigationProperty)
            {
                NavigationProperty np = efobject as NavigationProperty;
                var shapeElement = GetDesignerShapeElementForEFObject(diagram, np.Parent);

                if (shapeElement is EntityTypeShape entityTypeShape)
                {
                    // get the view model navigation property

                    // try to create the DiagramItem from this
                    if (diagram.ModelElement.ModelXRef.GetExisting(np) is ViewModel.NavigationProperty vmNavProp)
                    {
                        var index = entityTypeShape.NavigationCompartment.Items.IndexOf(vmNavProp);
                        if (index >= 0)
                        {
                            diagramItemCollection.Add(
                                new DiagramItem(
                                    entityTypeShape.NavigationCompartment, entityTypeShape.NavigationCompartment.ListField,
                                    new ListItemSubField(index)));
                            return;
                        }
                    }
                }
            }
            else if (efobject is Property)
            {
                Property prop = efobject as Property;
                if (prop.IsComplexTypeProperty)
                {
                    // complex type properties are not supported in the designer
                    return;
                }
                var shapeElement = GetDesignerShapeElementForEFObject(diagram, prop.Parent);
                if (shapeElement is EntityTypeShape entityTypeShape)
                {
                    // get the view model  property

                    if (diagram.ModelElement.ModelXRef.GetExisting(prop) is ViewModel.Property vmProp)
                    {
                        var index = entityTypeShape.PropertiesCompartment.Items.IndexOf(vmProp);
                        if (index >= 0)
                        {
                            diagramItemCollection.Add(
                                new DiagramItem(
                                    entityTypeShape.PropertiesCompartment, entityTypeShape.PropertiesCompartment.ListField,
                                    new ListItemSubField(index)));
                            return;
                        }
                    }
                }
            }
            else if (efobject is EntityType)
            {
                var shapeElement = GetDesignerShapeElementForEFObject(diagram, efobject);
                if (shapeElement != null)
                {
                    diagramItemCollection.Add(new DiagramItem(shapeElement));
                    return;
                }
            }
            else if (efobject is EntitySet)
            {
                EntitySet es = efobject as EntitySet;
                foreach (var entityType in es.GetEntityTypesInTheSet())
                {
                    if (entityType != null)
                    {
                        RetrieveDiagramItemCollectionForEFObject(diagram, entityType, diagramItemCollection);
                    }
                }
                return;
            }
            else if (efobject is AssociationSet)
            {
                // return a diagram item for the association
                AssociationSet associationSet = efobject as AssociationSet;
                var association = associationSet.Association.Target;
                if (association != null)
                {
                    RetrieveDiagramItemCollectionForEFObject(diagram, association, diagramItemCollection);
                    return;
                }
            }
            else if (efobject is AssociationSetEnd)
            {
                AssociationSetEnd associationSetEnd = efobject as AssociationSetEnd;
                var end = associationSetEnd.Role.Target;
                if (end != null)
                {
                    RetrieveDiagramItemCollectionForEFObject(diagram, end, diagramItemCollection);
                    return;
                }
                else
                {
                    var es = associationSetEnd.EntitySet.Target;
                    if (es != null)
                    {
                        RetrieveDiagramItemCollectionForEFObject(diagram, es, diagramItemCollection);
                        return;
                    }
                }
            }
            else if (efobject is PropertyRef)
            {
                PropertyRef pref = efobject as PropertyRef;
                if (pref.Name.Target != null)
                {
                    RetrieveDiagramItemCollectionForEFObject(diagram, pref.Name.Target, diagramItemCollection);
                    return;
                }
            }
            else if (efobject is PropertyRefContainer)
            {
                PropertyRefContainer prefContainer = efobject as PropertyRefContainer;

                // just use the first entry in the list.
                foreach (var pref in prefContainer.PropertyRefs)
                {
                    RetrieveDiagramItemCollectionForEFObject(diagram, pref, diagramItemCollection);
                    return;
                }
            }
            else if (efobject is EFAttribute)
            {
                // this is an EFAttribute node, so get the DiagramItem for the parent
                RetrieveDiagramItemCollectionForEFObject(diagram, efobject.Parent, diagramItemCollection);
                return;
            }
            else if (efobject is ConceptualEntityModel)
            {
                // nothing in the DSL surface to map to, so return null
                return;
            }
            else if (efobject is ConceptualEntityContainer)
            {
                // nothing in the DSL surface to map to, so return null
                return;
            }
            else if (efobject is FunctionImport)
            {
                // nothing in the DSL surface to map to, so return null
                return;
            }
            else
            {
                Debug.Fail("unexpected type of efobject.  type = " + efobject.GetType());
                if (efobject.Parent != null)
                {
                    RetrieveDiagramItemCollectionForEFObject(diagram, efobject.Parent, diagramItemCollection);
                }
            }
        }

        /// <summary>
        ///     This method will return a DSL ShapeElement for the given efobject.  If the given efobject doesn't map to a designer shape,
        ///     then this will look for a designer shape for the object's parent.
        ///     If no designer shape can be found, this will return null.
        /// </summary>
        /// <param name="efobject"></param>
        /// <returns></returns>
        private static ShapeElement GetDesignerShapeElementForEFObject(EntityDesignerSurface diagram, EFObject efobject)
        {
            ShapeElement shapeElement = null;
            while (shapeElement == null
                   && efobject != null
                   && ((efobject is ConceptualEntityModel) == false))
            {
                var dslElement = diagram.ModelElement.ModelXRef.GetExisting(efobject);
                shapeElement = dslElement as ShapeElement;

                if (shapeElement == null
                    && dslElement != null)
                {
                    var shapes = PresentationViewsSubject.GetPresentation(dslElement);

                    // just select the first shape for this item
                    if (shapes != null
                        && shapes.Count > 0)
                    {
                        shapeElement = shapes[0] as ShapeElement;
                    }
                }

                // walk up the EFObject tree until we find a node that has a ShapeElement.
                if (shapeElement == null)
                {
                    efobject = efobject.Parent;
                }
            }
            return shapeElement;
        }
    }
}
