// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the MIT license.  See License.txt in the project root for license information.

using System;
using System.Linq;
using FluentAssertions;
using Microsoft.Data.Entity.Design.Package;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio;
using Microsoft.VisualStudio.Data.Entity.Design.VisualStudio.Package;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Data.Entity.Tests.DesignPackage.CustomCode
{
    /// <summary>
    ///     Covers the parts of the Move Diagrams to Separate File command that do not need a running shell.
    /// </summary>
    /// <remarks>
    ///     The migration itself, the status handler and the project-item checks all reach into VS services and are only
    ///     exercisable in the experimental hive. What is covered here is the guard behaviour that used to be missing:
    ///     the entry points now refuse bad input instead of throwing out of a command handler, where the shell swallows
    ///     the exception and the user sees nothing.
    /// </remarks>
    [TestClass]
    public class MoveDiagramsToSeparateFileTests
    {
        [TestMethod]
        public void CanMoveDiagramsToSeparateFile_returns_false_for_a_null_artifact()
        {
            MicrosoftDataEntityDesignCommandSet.CanMoveDiagramsToSeparateFile(null, out var reason)
                .Should().BeFalse();

            reason.Should().NotBeNullOrWhiteSpace(
                "the caller logs this phrase, so an empty one would produce a log entry that explains nothing");
        }

        [TestMethod]
        public void IsLinkProjectItem_returns_false_when_there_is_no_project_item()
        {
            // SDK-style projects do not always expose the DTE automation property collection. Before this guard the
            // null dereference threw out of the command status handler and the command silently never appeared.
            VsUtils.IsLinkProjectItem(null).Should().BeFalse();
        }

        [TestMethod]
        public void ExplorerWindow_is_declared_transient()
        {
            var explorerWindow = typeof(MicrosoftDataEntityDesignPackage)
                .GetCustomAttributes(typeof(ProvideToolWindowAttribute), inherit: false)
                .Cast<ProvideToolWindowAttribute>()
                .SingleOrDefault(attribute => attribute.ToolType == typeof(EntityDesignExplorerWindow));

            explorerWindow.Should().NotBeNull();
            explorerWindow.Transient.Should().BeTrue(
                "a persisted frame is restored through ModelingPackage.CreateToolWindow, which resolves slots only "
                + "against the DSL tool window registry - EntityDesignExplorerWindow is a shell ToolWindowPane and "
                + "cannot be registered there, so a non-transient declaration throws ArgumentNullException into the "
                + "window's frame at startup");
        }
    }
}
