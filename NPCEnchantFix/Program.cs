using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace NPCEnchantFix
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return SynthesisPipeline.Instance.Patch<ISkyrimMod, ISkyrimModGetter>(
                args: args,
                patcher: RunPatch,
                new UserPreferences()
                {
                    ActionsForEmptyArgs = new RunDefaultPatcher()
                    {
                        IdentifyingModKey = "NPCEnchantFix.esp",
                        TargetRelease = GameRelease.SkyrimSE
                    }
                }
            );
        }

        public static void RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state)
        {
            ModKey skyrimKey = "Skyrim.esm";
            var skyrim = state.LoadOrder[skyrimKey];
            var alchemySkillBoosts = skyrim.Mod.Perks.RecordCache[new FormKey(skyrimKey, 0x0a725c)];
            var perkSkillBoosts = skyrim.Mod.Perks.RecordCache[new FormKey(skyrimKey, 0x0cf788)];

            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                if (npc.Perks == null) continue;

                if (npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.SpellList)) continue;
                
                foreach (var perk in npc.Perks.Distinct())
                {
                    if (perk.Perk.FormKey.Equals(alchemySkillBoosts.FormKey))
                    {
                        state.PatchMod.Npcs.GetOrAddAsOverride(npc).Perks.Add(new PerkPlacement()
                        {
                            Perk = alchemySkillBoosts.FormKey,
                            Rank = 1
                        });
                    }

                    if (perk.Perk.FormKey.Equals(perkSkillBoosts.FormKey))
                    {
                        state.PatchMod.Npcs.GetOrAddAsOverride(npc).Perks.Add(new PerkPlacement()
                        {
                            Perk = perkSkillBoosts.FormKey,
                            Rank = 1
                        });
                    }
                }
            }
        }
    }
}

