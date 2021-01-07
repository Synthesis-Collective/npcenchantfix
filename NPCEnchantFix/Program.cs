using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Noggog;
using Mutagen.Bethesda.FormKeys.SkyrimSE;

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
            // Loop over all NPCs in the load order
            foreach (var npc in state.LoadOrder.PriorityOrder.WinningOverrides<INpcGetter>())
            {
                try
                {
                    // Skip NPC if it inherits spells from its template
                    if (npc.Configuration.TemplateFlags.HasFlag(NpcConfiguration.TemplateFlag.SpellList)) continue;

                    // Find if the NPC has PerkSkill or AlchemySkill perks
                    var hasPerkSkillBoosts = false;
                    var hasAlchemySkillBoosts = false;

                    foreach (var perk in npc.Perks.EmptyIfNull())
                    {
                        if (perk.Perk.FormKey.Equals(Skyrim.Perk.AlchemySkillBoosts)) hasAlchemySkillBoosts = true;
                        if (perk.Perk.FormKey.Equals(Skyrim.Perk.PerkSkillBoosts)) hasPerkSkillBoosts = true;
                        if (hasAlchemySkillBoosts && hasPerkSkillBoosts) break;
                    }

                    // If NPC has both, it is safe
                    if (hasAlchemySkillBoosts && hasPerkSkillBoosts) continue;

                    // Otherwise, add the NPC to the patch
                    var modifiedNpc = state.PatchMod.Npcs.GetOrAddAsOverride(npc);

                    // Ensure perk list exists
                    modifiedNpc.Perks ??= new ExtendedList<PerkPlacement>();

                    // Add missing perks
                    if (!hasAlchemySkillBoosts)
                    {
                        modifiedNpc.Perks.Add(new PerkPlacement()
                        {
                            Perk = Skyrim.Perk.AlchemySkillBoosts,
                            Rank = 1
                        });
                    }

                    if (!hasPerkSkillBoosts)
                    {
                        modifiedNpc.Perks.Add(new PerkPlacement()
                        {
                            Perk = Skyrim.Perk.PerkSkillBoosts,
                            Rank = 1
                        });
                    }
                }
                catch (Exception ex)
                {
                    throw RecordException.Factory(ex, npc);
                }
            }
        }
    }
}
