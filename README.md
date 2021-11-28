## Grid Trasition Crash Fix

Sometimes location aliases can remain NULL and unresolved when getting checked. This patcher moves those checks to the end of quest alias condition lists so they have more time to be resolved and not cause a crash.
