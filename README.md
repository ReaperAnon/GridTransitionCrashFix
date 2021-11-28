## Grid Transition Crash Fix

Sometimes location aliases can remain NULL and unresolved when getting checked. This patcher moves those checks to the end of quest alias condition lists so they have more time to be resolved and not cause a crash.

![Header](https://staticdelivery.nexusmods.com/mods/110/images/108260/108260-1625409970-1418939962.png)
