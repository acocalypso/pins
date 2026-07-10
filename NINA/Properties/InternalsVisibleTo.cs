#region "copyright"

/*
    Copyright © 2026 Nico Trost <nico.trost57@gmail.com> and the PI.N.S. contributors

    This file is part of PI 'N' Stars.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using System.Runtime.CompilerServices;

// Upstream N.I.N.A. declares these in NINA/Properties/AssemblyInfo.cs (DEBUG only).
// The pins fork dropped that file; NINA.Test needs internals access (e.g. SkyAtlasVM)
// and Moq needs DynamicProxyGenAssembly2 to proxy internal types.
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("NINA.Test")]
