// SPDX-FileCopyrightText: 2024 Fansana <116083121+Fansana@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 fox <daytimer253@gmail.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using System.Numerics;

namespace Content.Shared.Floofstation.Leash.Components;

/// <summary>
///     Indicates that this entity or the entity that wears this entity can be leashed.
/// </summary>
[RegisterComponent]
public sealed partial class LeashAnchorComponent : Component
{
    /// <summary>
    ///     The visual offset of the "anchor point".
    /// </summary>
    [DataField]
    public Vector2 Offset = Vector2.Zero;
}
