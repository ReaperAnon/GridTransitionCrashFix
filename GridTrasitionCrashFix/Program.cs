using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using System.Threading.Tasks;

namespace GridTrasitionCrashFix
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            return await SynthesisPipeline.Instance
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .SetTypicalOpen(GameRelease.SkyrimSE, "TransitionCrashFix.esp")
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            foreach (var questGetter in state.LoadOrder.PriorityOrder.WinningOverrides<IQuestGetter>())
            {
                bool wasChanged = false;
                Quest questCopy = questGetter.DeepCopy();
                foreach (var questAlias in questCopy.Aliases)
                {
                    for (int i = questAlias.Conditions.Count - 1; i >= 0; --i)
                    {
                        var currCond = questAlias.Conditions[i];
                        if (currCond is ConditionFloat condFloat && condFloat.Data is FunctionConditionData condData && condData.Function == Condition.Function.GetInCurrentLocAlias)
                        {
                            if (condFloat.Flags.HasFlag(Condition.Flag.OR))
                            {
                                // Find the last OR condition and move this one after it.
                                int j = Math.Min(i + 1, questAlias.Conditions.Count);
                                while (j < questAlias.Conditions.Count && questAlias.Conditions[j].Flags.HasFlag(Condition.Flag.OR))
                                    ++j;

                                // If the position of the last OR is not different, skip.
                                if (j <= i + 1) continue;
                                questAlias.Conditions.Insert(j, currCond.DeepCopy());
                                questAlias.Conditions.RemoveAt(i);
                                wasChanged = true;
                            }
                            else
                            {
                                // If this is the last condition in a chain of ORs then don't move.
                                if (i > 0 && questAlias.Conditions[i - 1].Flags.HasFlag(Condition.Flag.OR)) continue;

                                // Find the last AND condition and move this one after it.
                                int j = Math.Min(i + 1, questAlias.Conditions.Count);
                                while (j < questAlias.Conditions.Count && !questAlias.Conditions[j].Flags.HasFlag(Condition.Flag.OR))
                                    ++j;

                                // If the position of the last AND is not be different, skip.
                                if (j <= i + 1)
                                {
                                    // Check if after our AND condition we only have ORs.
                                    int ORcount = 0;
                                    while (j < questAlias.Conditions.Count && questAlias.Conditions[j].Flags.HasFlag(Condition.Flag.OR))
                                    {
                                        ++j;
                                        ++ORcount;
                                    }

                                    // If the number of OR conditions is equal to the amount of conditions after our current one.
                                    if (ORcount > 0 && ORcount == questAlias.Conditions.Count - i - 1)
                                    {
                                        Console.WriteLine("Changed multiple conditions in QUST " + questCopy.EditorID);
                                        questAlias.Conditions[questAlias.Conditions.Count - 1].Flags &= ~Condition.Flag.OR;
                                        questAlias.Conditions.Insert(questAlias.Conditions.Count, currCond.DeepCopy());
                                        questAlias.Conditions.RemoveAt(i);
                                        wasChanged = true;
                                    }

                                    continue;
                                }

                                questAlias.Conditions.Insert(j, currCond.DeepCopy());
                                questAlias.Conditions.RemoveAt(i);
                                wasChanged = true;
                            }
                        }
                    }
                }

                if (wasChanged)
                    state.PatchMod.Quests.Add(questCopy);
            }
        }
    }
}
