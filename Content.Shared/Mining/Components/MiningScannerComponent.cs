// SPDX-FileCopyrightText: 2024 Nemanja
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;

namespace Content.Shared.Mining.Components;

/// <summary>
/// This is a component that, when held in the inventory or pocket of a player, gives the the MiningOverlay.
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(MiningScannerSystem))]
public sealed partial class MiningScannerComponent : Component
{
    [DataField]
    public float Range = 5;
}
