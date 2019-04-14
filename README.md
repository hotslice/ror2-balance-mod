# Balance Mod 2.2.0  
by Hotslice

# How to Install and Configure
This mod now requires the community mod loader / BepInExPack to load. You can find that release on thunderstore.io.  
To install, copy the files inside this mod package's BepInEx folder to your Risk of Rain 2 / BepInEx folder.  
To configure, edit the installed BepInEx/config/com.hotslice.balancemod.cfg file in a text editor. Currently the only supported values are true or false.

# All Changes Compiled:

You can enable or disable any of these change blocks via the BepInEx/config/com.hotslice.balancemod.cfg file.

Artificer base move speed increased from 7 to 9 (same as pre-nerf MUL-T)  

Artificer M1 proc coefficient increased from 0.2 to 1.0  
Artificer M1 damage coefficient reduced from 2.2 to 1.0  
Increasing the proc coefficient has the side effect of making her burning DoT tick 8 times instead of 2.  
Some math behind this change, at level 1 the ability damage is:  
OLD: 24 + 2x3 ticks = 30 damage, 0.2 x 220% = 44% proc efficiency  
NEW: 12 + 8x3 ticks = 36 damage, 1.0 x 100% = 100% proc efficiency  
So she does less frontloaded damage but slightly more overall and scales better into late game with procs (less hard hitting, but 5x as common)

Monster DoT (burn) damage reduced from 4x100% (of base?) to 4x60% (of hit damage)  
Monster DoT (burn) now scales off the damage of the hit instead of the base damage of the monster (Blazing Titan beam fix)  

Gesture of the Drowned no longer infinitely spams the looted equipment if you pick it up before picking up any equipment  

Wake of Vultures item Blue Affix now grants +50% bonus HP as shield instead of cutting your health in half

Item damage now scales linearly off the damage of the hit, giving the listed damage increase on the item.  
This is a buff to the following scenarios:  
Base damage <100% (Double Tap/Auto-Nailgun) + item damage <100% (Ukulele)  
Base damage >100% (Rebar, all M2, etc) + item damage >100% (Sticky Bomb, ATG Missile, etc)  
All characters received a small buff from this change, but it is a bigger buff to the hardest hitting abilities.  

In very long games, when the Combat Director would start skipping spawns due to them being "too cheap", mobs will start to spawn with items.  
Items will be added until the mob reaches an acceptable difficulty. What items are added and in what number is totally at my discretion.  
A chat message will be displayed when this behavior is enabled.  
What items each mob gets will also be displayed in chat. All mob names show as "???" for now. These messages are kind of spammy, there is a config to disable them.  

# Changelog

v0.1.1:

Removed unnecessary debug logging

v0.1.2:

Fixed an unintended bug with Wake of Vultures fix affecting overloading elites

v0.1.3:

Monster DoT (burn) now scales off the damage of the hit instead of the base damage of the monster (Blazing Titan beam fix)

v0.1.4:

Artificer M1 proc coefficient increased from 0.2 to 0.4 (seriously Hopoo wtf)  
Removed unnecessary debug logging... again

v1.0.0:

No gameplay changes.  
Now compatible with Discord community mod loader aka BepInEx. See "How to Install and Configure" at the top.

v1.1.0:
Increased Blazing Monster DoT back to 60%, the change to make it scale off the actual hit damage seems to have fixed the most broken scenarios.  
Artificer M1 proc coefficient and damage coefficient both to 100%. See full changelist.  

v2.0.0:  
Updated for compatibility with BepInExPack 1.3.1 or higher  
Not backwards compatible  

v2.1.0:  
Gesture of the Drowned no longer infinitely spams the looted equipment if you pick it up before picking up any equipment  

v2.2.0:  
Added a new way to scale difficulty of mobs in very long games. See full changelist.  
