// -----------------------------------------------------------------------
// <copyright file="NWFixScp096BreakingDoor.cs" company="ExMod Team">
// Copyright (c) ExMod Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.Patches.Fixes
{
    using System.Collections.Generic;
    using System.Reflection.Emit;

    using API.Features.Pools;
    using Footprinting;
    using HarmonyLib;
    using Interactables.Interobjects;
    using Interactables.Interobjects.DoorUtils;
    using PlayerRoles.PlayableScps.Scp096;
    using UnityEngine;

    using static HarmonyLib.AccessTools;

    /// <summary>
    /// Patches the <see cref="Scp096HitHandler.CheckDoorHit(Collider,Footprint)"/> delegate.
    /// Fixes open doors getting easily broke.
    /// Bug reported to NW (https://git.scpslgame.com/northwood-qa/scpsl-bug-reporting/-/issues/198).
    /// </summary>
    [HarmonyPatch(typeof(Scp096HitHandler), nameof(Scp096HitHandler.CheckDoorHit))]
    internal class NWFixScp096BreakingDoor
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Pool.Get(instructions);

            Label ret = generator.DefineLabel();
            int offset = -5;
            int index = newInstructions.FindIndex(x => x.operand == (object)Method(typeof(IDamageableDoor), nameof(IDamageableDoor.ServerDamage))) + offset;

            newInstructions.InsertRange(index, new[]
            {
                new CodeInstruction(OpCodes.Ldloc_0).MoveLabelsFrom(newInstructions[index]),
                new(OpCodes.Call, Method(typeof(NWFixScp096BreakingDoor), nameof(IsFullyOpen))),
                new(OpCodes.Brtrue_S, ret),
            });

            newInstructions[newInstructions.Count - 1].labels.Add(ret);

            for (int z = 0; z < newInstructions.Count; z++)
                yield return newInstructions[z];

            ListPool<CodeInstruction>.Pool.Return(newInstructions);
        }

        private static bool IsFullyOpen(IDamageableDoor damageableDoor) => damageableDoor is BreakableDoor breakableDoor && breakableDoor.GetExactState() is 1;
    }
}
