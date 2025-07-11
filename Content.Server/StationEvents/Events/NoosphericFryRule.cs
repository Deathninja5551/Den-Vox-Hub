// SPDX-FileCopyrightText: 2023 PHCodes <47927305+PHCodes@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Pieter-Jan Briers <pieterjan.briers@gmail.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 sleepyyapril <flyingkarii@gmail.com>
// SPDX-FileCopyrightText: 2025 VMSolidus <evilexecutive@gmail.com>
// SPDX-FileCopyrightText: 2025 sleepyyapril <123355664+sleepyyapril@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later AND MIT

using Robust.Shared.Map;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.GameTicking.Components;
using Content.Shared.Construction.EntitySystems;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Popups;
using Content.Server.Power.Components;
using Content.Server.Power.EntitySystems;
using Content.Server.Psionics.Glimmer;
using Content.Server.StationEvents.Components;
using Content.Shared.Abilities.Psionics;
using Content.Shared.Damage;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Psionics.Glimmer;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map.Components;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Fries tinfoil hats and cages
/// </summary>
internal sealed class NoosphericFryRule : StationEventSystem<NoosphericFryRuleComponent>
{
    [Dependency] private readonly SharedMapSystem _sharedMapSystem = default!;
    [Dependency] private readonly MobStateSystem _mobStateSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;
    [Dependency] private readonly DamageableSystem _damageableSystem = default!;
    [Dependency] private readonly GlimmerSystem _glimmerSystem = default!;
    [Dependency] private readonly FlammableSystem _flammableSystem = default!;
    [Dependency] private readonly GlimmerReactiveSystem _glimmerReactiveSystem = default!;
    [Dependency] private readonly AnchorableSystem _anchorableSystem = default!;
    [Dependency] private readonly PowerReceiverSystem _powerReceiverSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    protected override void Started(EntityUid uid, NoosphericFryRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        List<(EntityUid wearer, TinfoilHatComponent worn)> psionicList = new();

        var query = EntityQueryEnumerator<PsionicInsulationComponent, MobStateComponent>();
        while (query.MoveNext(out var psion, out _, out _))
        {
            if (!_mobStateSystem.IsAlive(psion))
                continue;

            if (!_inventorySystem.TryGetSlotEntity(psion, "head", out var headItem))
                continue;

            if (!TryComp<TinfoilHatComponent>(headItem, out var tinfoil))
                continue;

            psionicList.Add((psion, tinfoil));
        }

        foreach (var pair in psionicList)
        {
            if (pair.worn.DestroyOnFry)
            {
                QueueDel(pair.worn.Owner);
                Spawn("Ash", Transform(pair.wearer).Coordinates);
                _popupSystem.PopupEntity(Loc.GetString("psionic-burns-up", ("item", pair.worn.Owner)), pair.wearer, Filter.Pvs(pair.worn.Owner), true, Shared.Popups.PopupType.MediumCaution);
                _audioSystem.PlayEntity("/Audio/Effects/lightburn.ogg", Filter.Pvs(pair.worn.Owner), pair.worn.Owner, true);
            } else
            {
                _popupSystem.PopupEntity(Loc.GetString("psionic-burn-resist", ("item", pair.worn.Owner)), pair.wearer, Filter.Pvs(pair.worn.Owner), true, Shared.Popups.PopupType.SmallCaution);
                _audioSystem.PlayEntity("/Audio/Effects/lightburn.ogg", Filter.Pvs(pair.worn.Owner), pair.worn.Owner, true);
            }

            DamageSpecifier damage = new();
            damage.DamageDict.Add("Heat", 2.5);
            damage.DamageDict.Add("Shock", 2.5);

            if (_glimmerSystem.GlimmerOutput > component.FryHeadgearMinorThreshold && _glimmerSystem.GlimmerOutput < component.FryHeadgearMajorThreshold)
            {
                damage *= 2;
                if (TryComp<FlammableComponent>(pair.wearer, out var flammableComponent))
                {
                    flammableComponent.FireStacks += 1;
                    _flammableSystem.Ignite(pair.wearer, pair.wearer, flammableComponent);
                }
            } else if (_glimmerSystem.GlimmerOutput > component.FryHeadgearMajorThreshold)
            {
                damage *= 3;
                if (TryComp<FlammableComponent>(pair.wearer, out var flammableComponent))
                {
                    flammableComponent.FireStacks += 2;
                    _flammableSystem.Ignite(pair.wearer, pair.wearer, flammableComponent);
                }
            }

            _damageableSystem.TryChangeDamage(pair.wearer, damage, true, true);
        }

        // for probers:
        var queryReactive = EntityQueryEnumerator<SharedGlimmerReactiveComponent, TransformComponent, PhysicsComponent>();
        while (queryReactive.MoveNext(out var reactive, out _, out var xform, out var physics))
        {
            // shoot out three bolts of lighting...
            _glimmerReactiveSystem.BeamRandomNearProber(reactive, 3, 12);

            // try to anchor if we can
            if (!xform.Anchored)
            {
                var coordinates = xform.Coordinates;
                var gridUid = xform.GridUid;

                if (gridUid == null)
                    continue;

                if (!TryComp<MapGridComponent>(gridUid, out var grid))
                    continue;

                var tileIndices = _sharedMapSystem.TileIndicesFor((EntityUid) gridUid, grid, coordinates);

                if (_anchorableSystem.TileFree(grid, tileIndices, physics.CollisionLayer, physics.CollisionMask))
                    _transformSystem.AnchorEntity(reactive, xform);
            }

            if (!TryComp<ApcPowerReceiverComponent>(reactive, out var power))
                continue;

            // If it's been turned off, turn it back on.
            if (power.PowerDisabled)
                _powerReceiverSystem.TogglePower(reactive, false);
        }
    }
}
