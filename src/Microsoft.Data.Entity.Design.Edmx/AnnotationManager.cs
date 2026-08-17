// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Data.Entity.Design.EntityFramework;

namespace Microsoft.Data.Entity.Design.Edmx
{

    /// <summary>
    /// Instantiated via an <see cref="XElement" /> and a version, this class will retrieve all annotations under the
    /// <see cref="XElement" /> given an annotation namespace.
    /// </summary>
    /// <remarks>
    /// An "annotation" is any attribute or child element on an EDMX model element that lives in a namespace the EDMX schemas do not
    /// define. Those constructs belong to third parties (or to future versions of the designer), so the designer cannot interpret
    /// them, but it must not lose them either: everything read out of the file has to be written back on save. This class draws that
    /// line by treating the reserved schema namespaces for a given EDMX version as "known" and everything else as an annotation to be
    /// preserved.
    /// </remarks>
    internal class AnnotationManager
    {

        #region Fields

        /// <summary>
        /// The set of namespace names owned by the EDMX schemas for the version passed to <see cref="Load" />.
        /// </summary>
        /// <remarks>
        /// Membership in this set is what disqualifies an attribute or element from being an annotation, so anything absent here is
        /// treated as unknown/third-party content that must survive a round-trip.
        /// </remarks>
        private HashSet<string> _reservedNamespaces;

        /// <summary>
        /// The element whose attributes and immediate child elements are scanned for annotations.
        /// </summary>
        private XElement _xElement;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="AnnotationManager" /> class bound to the specified element and EDMX version.
        /// </summary>
        /// <param name="xelement">The element whose annotations this instance manages.</param>
        /// <param name="version">The EDMX schema version used to determine which namespaces are reserved.</param>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when <paramref name="version" /> is not a schema version known to <see cref="SchemaManager" />.
        /// </exception>
        internal AnnotationManager(XElement xelement, Version version)
        {
            Load(xelement, version);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Gets the single annotation of the specified type with the given namespace and local name.
        /// </summary>
        /// <typeparam name="T">The kind of <see cref="XObject" /> to look for, typically <see cref="XAttribute" /> or <see cref="XElement" />.</typeparam>
        /// <param name="namespaceName">The annotation namespace to match.</param>
        /// <param name="name">The local name to match.</param>
        /// <returns>
        /// The matching annotation, or <see langword="null" /> if no annotation matches.
        /// </returns>
        /// <exception cref="InvalidOperationException">Thrown when more than one annotation matches.</exception>
        /// <remarks>
        /// Uses <see cref="Enumerable.SingleOrDefault{TSource}(IEnumerable{TSource})" /> deliberately: a well-formed document cannot
        /// carry two attributes or two same-named siblings with identical qualified names, so a duplicate signals a corrupt document
        /// rather than a case to silently pick a winner for.
        /// </remarks>
        internal T GetAnnotation<T>(string namespaceName, string name) where T : XObject
        {
            return GetAnnotations<T>(namespaceName).Where(
                xo =>
                    {
                        if (xo is XAttribute xa)
                        {
                            return xa.Name.LocalName == name;
                        }
                        if (xo is XElement xe)
                        {
                            return xe.Name.LocalName == name;
                        }
                        return false;
                    }).SingleOrDefault();
        }

        /// <summary>
        /// Gets every annotation on the managed element, regardless of namespace.
        /// </summary>
        /// <returns>
        /// The attributes and immediate child elements that fall outside the reserved EDMX namespaces.
        /// </returns>
        /// <remarks>
        /// This is the single definition of "what the designer does not own but must preserve". Attributes with no namespace are
        /// skipped because the EF runtime leaves most of its own attributes unqualified, so an unqualified attribute is schema
        /// content rather than a third-party annotation.
        /// </remarks>
        internal IEnumerable<XObject> GetAnnotations()
        {
            foreach (var xa in _xElement.Attributes())
            {
                // EFRuntime doesn't namespace-qualify most of their attributes, so we just skip them here
                if (xa.Name is not null
                    && String.IsNullOrEmpty(xa.Name.NamespaceName) == false)
                {
                    if (_reservedNamespaces.Contains(xa.Name.NamespaceName) == false)
                    {
                        yield return xa;
                    }
                }
            }

            foreach (var xe in _xElement.Elements())
            {
                if (xe.Name.NamespaceName is not null
                    && _reservedNamespaces.Contains(xe.Name.NamespaceName) == false)
                {
                    yield return xe;
                }
            }
        }

        /// <summary>
        /// Gets the annotations of the specified type that belong to the given namespace.
        /// </summary>
        /// <typeparam name="T">The kind of <see cref="XObject" /> to return, typically <see cref="XAttribute" /> or <see cref="XElement" />.</typeparam>
        /// <param name="namespaceName">The annotation namespace to match.</param>
        /// <returns>
        /// The annotations of type <typeparamref name="T" /> whose namespace matches <paramref name="namespaceName" />.
        /// </returns>
        /// <remarks>
        /// The type filter alone is not enough, because <see cref="XAttribute" /> and <see cref="XElement" /> expose their names
        /// through unrelated members; the predicate re-tests the concrete type to read the right one.
        /// </remarks>
        internal IEnumerable<T> GetAnnotations<T>(string namespaceName) where T : XObject
        {
            return GetAnnotations().OfType<T>().Where(
                xo =>
                    {
                        if (xo is XAttribute xa)
                        {
                            return xa.Name.NamespaceName == namespaceName;
                        }
                        if (xo is XElement xe)
                        {
                            return xe.Name.NamespaceName == namespaceName;
                        }
                        return false;
                    });
        }

        /// <summary>
        /// Rebinds this instance to the specified element and EDMX version.
        /// </summary>
        /// <param name="xelement">The element whose annotations this instance manages.</param>
        /// <param name="version">The EDMX schema version used to determine which namespaces are reserved.</param>
        /// <exception cref="KeyNotFoundException">
        /// Thrown when <paramref name="version" /> is not a schema version known to <see cref="SchemaManager" />.
        /// </exception>
        /// <remarks>
        /// The reserved-namespace set is rebuilt from scratch rather than merged, so reloading against a different version cannot
        /// leave a previous version's namespaces behind and wrongly suppress annotations.
        /// </remarks>
        internal void Load(XElement xelement, Version version)
        {
            _reservedNamespaces = [];
            _xElement = xelement;
            foreach (var n in SchemaManager.GetAllNamespacesForVersion(version))
            {
                _reservedNamespaces.Add(n);
            }
        }

        /// <summary>
        /// Removes every annotation of the specified type in the given namespace.
        /// </summary>
        /// <typeparam name="T">The kind of <see cref="XObject" /> to remove, typically <see cref="XAttribute" /> or <see cref="XElement" />.</typeparam>
        /// <param name="namespaceName">The annotation namespace whose contents should be removed.</param>
        /// <remarks>
        /// The matches are copied into a list before anything is removed, because the underlying query is lazy and removing nodes
        /// while enumerating it would mutate the collection being walked.
        /// </remarks>
        internal void RemoveAnnotations<T>(string namespaceName) where T : XObject
        {
            IEnumerable<T> annotations = new List<T>(GetAnnotations<T>(namespaceName));
            foreach (var annotation in annotations.OfType<XAttribute>())
            {
                annotation.Remove();
            }
            foreach (var annotation in annotations.OfType<XElement>())
            {
                annotation.Remove();
            }
        }

        /// <summary>
        /// Removes the annotation attribute with the specified namespace and name, if it is present.
        /// </summary>
        /// <param name="namespaceName">The annotation namespace of the attribute to remove.</param>
        /// <param name="name">The local name of the attribute to remove.</param>
        /// <remarks>
        /// A missing attribute is not an error; the method is a no-op so callers can remove unconditionally.
        /// </remarks>
        internal void RemoveAttribute(string namespaceName, string name)
        {
            Debug.Assert(!String.IsNullOrEmpty(name), "Attribute name for AnnotationManager.RemoveAttribute is null or empty");
            if (!String.IsNullOrEmpty(name))
            {
                var xattr = GetAnnotation<XAttribute>(namespaceName, name);
                xattr?.Remove();
            }
        }

        /// <summary>
        /// Renames an annotation attribute, preserving its namespace and value.
        /// </summary>
        /// <param name="namespaceName">The annotation namespace of the attribute to rename.</param>
        /// <param name="oldName">The current local name of the attribute.</param>
        /// <param name="newName">The new local name for the attribute.</param>
        /// <remarks>
        /// <see cref="XAttribute" /> names are immutable, so the rename is done by removing the attribute and adding a replacement to
        /// the same parent. The namespace and value are captured from the original attribute rather than recomputed, which keeps a
        /// third-party annotation intact when the designer only knows that its name changed.
        /// </remarks>
        internal void UpdateAttributeName(string namespaceName, string oldName, string newName)
        {
            Debug.Assert(!String.IsNullOrEmpty(oldName), "Attribute name for AnnotationManager.UpdateAttributeName is null or empty");

            if (!String.IsNullOrEmpty(oldName))
            {
                var xattr = GetAnnotation<XAttribute>(namespaceName, oldName);
                if (xattr is not null)
                {
                    var parent = xattr.Parent;
                    if (parent is not null)
                    {
                        var existingNs = xattr.Name.Namespace;
                        var existingvalue = xattr.Value;
                        xattr.Remove();
                        parent.Add(new XAttribute(existingNs + newName, existingvalue));
                    }
                }
            }
        }

        /// <summary>
        ///     Update, Create, or Delete an annotation that is represented by an attribute.
        ///     This also handles defaults.
        /// </summary>
        /// <param name="namespaceName">The annotation namespace of the attribute.</param>
        /// <param name="name">The local name of the attribute.</param>
        /// <param name="newValue">Pass in null to delete</param>
        /// <param name="isDefault">
        /// <see langword="true" /> when <paramref name="newValue" /> is the value the attribute would have if it were absent.
        /// </param>
        /// <remarks>
        /// Default values are never written out: an attribute set to its default is removed and one that does not exist is not
        /// created. This keeps the saved EDMX free of redundant markup and avoids spurious diffs when a document is round-tripped.
        /// </remarks>
        internal void UpdateAttributeValue(string namespaceName, string name, string newValue, bool isDefault)
        {
            Debug.Assert(!String.IsNullOrEmpty(name), "Attribute name for AnnotationManager.UpdateAttributeValue is null or empty");

            if (!String.IsNullOrEmpty(name))
            {
                var xattr = GetAnnotation<XAttribute>(namespaceName, name);
                if (xattr is not null)
                {
                    if (newValue is null || isDefault)
                    {
                        xattr.Remove();
                    }
                    else if (xattr.Value != newValue)
                    {
                        xattr.Value = newValue;
                    }
                }
                else if (!isDefault
                         && newValue is not null)
                {
                    xattr = new XAttribute(XName.Get(name, namespaceName), newValue);
                    _xElement.Add(xattr);
                }
            }
        }

        #endregion

    }

}
