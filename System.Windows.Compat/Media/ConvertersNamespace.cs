#region "copyright"
/*
    Copyright © 2026 Nico Trost and the PI.N.S. contributors

    Compatibility shim: expose System.Windows.Media.Converters namespace so code
    that uses `using System.Windows.Media.Converters;` compiles against
    `System.Windows.Compat`.

    This file is licensed under the same terms as the containing project.
*/
#endregion

using System;

namespace System.Windows.Media.Converters
{
    /// <summary>
    /// Compatibility placeholder to make the namespace available for code that
    /// imports `System.Windows.Media.Converters`.
    /// Add real converter implementations here if needed in the future.
    /// </summary>
    public static class NamespaceShim
    {
        // Intentionally empty — presence of this public type makes the
        // `System.Windows.Media.Converters` namespace resolvable by the compiler.
    }
}
