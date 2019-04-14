using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace BalanceMod
{
	[BepInPlugin("com.hotslice.balancemod", "BalanceMod", "2.2.0")]
	public class BalanceMod : BaseUnityPlugin
	{

        public static ConfigWrapper<bool> BlazingDoTFixEnabled { get; set; }
        public static ConfigWrapper<bool> WakeOfVulturesFixEnabled { get; set; }
        public static ConfigWrapper<bool> GestureOfTheDrownedFixEnabled { get; set; }
        public static ConfigWrapper<bool> ItemBalanceEnabled { get; set; }
        public static ConfigWrapper<bool> ArtificerMoveSpeedBuffEnabled { get; set; }
        public static ConfigWrapper<bool> ArtificerM1CoeffBuffEnabled { get; set; }
        public static ConfigWrapper<bool> LateGameEnemyItemsEnabled { get; set; }
        //public static ConfigWrapper<float> LateGameEnemyItemsActivationThreshold { get; set; }
        public static ConfigWrapper<bool> EnemyItemsInChatEnabled { get; set; }

        static public new ManualLogSource Logger { get; protected set; }

        public BalanceMod()
        {
            Logger = base.Logger;

            BlazingDoTFixEnabled = Config.Wrap(
                "BlazingDoTFix",
                "BlazingDoTFix",
                "Enables or disables Blazing elite enemy DoT nerf / fix.",
                true);

            WakeOfVulturesFixEnabled = Config.Wrap(
                "WakeOfVulturesFix",
                "WakeOfVulturesFix",
                "Enables or disables Wake of Vultures hp -> shield fix.",
                true);

            GestureOfTheDrownedFixEnabled = Config.Wrap(
                "GestureOfTheDrownedFix",
                "GestureOfTheDrownedFix",
                "Enables or disables Gesture of the Drowned infinite equip spam bug fix.",
                true);

            ItemBalanceEnabled = Config.Wrap(
                "ItemProcScalingFix",
                "ItemProcScalingFix",
                "Enables or disables item proc damage linear scaling.",
                true);

            ArtificerMoveSpeedBuffEnabled = Config.Wrap(
                "ArtificerMoveSpeedBuff",
                "ArtificerMoveSpeedBuff",
                "Enables or disables improved Artificer base move speed.",
                true);

            ArtificerM1CoeffBuffEnabled = Config.Wrap(
                "ArtificerM1CoeffBuff",
                "ArtificerM1CoeffBuff",
                "Enables or disables improved Artificer M1 coefficients.",
                true);

            LateGameEnemyItemsEnabled = Config.Wrap(
                "LateGameEnemyItems",
                "LateGameEnemyItems",
                "Instead of skipping spawns that are too easy, the Combat Director will spawn enemies with items. Only activates in very long games.",
                true);

            //LateGameEnemyItemsActivationThreshold = Config.Wrap(
            //    "LateGameEnemyItemsActivationThreshold",
            //    "LateGameEnemyItemsActivationThreshold",
            //    "The threshold of CombatDirector credits at which LateGameEnemyItems will activate. Allowed values are 0 to 15000. Default is 15000.",
            //    15000f);

            EnemyItemsInChatEnabled = Config.Wrap(
                "EnemyItemsInChat",
                "EnemyItemsInChat",
                "When an enemy is granted an item via LateGameEnemyItems, a chat message will appear. It will say \"??? has picked up X\".",
                true);

            Hooks.Hook();
        }
    }
}