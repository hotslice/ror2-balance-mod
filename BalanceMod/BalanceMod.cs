using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;

namespace BalanceMod
{
	[BepInPlugin("com.hotslice.balancemod", "BalanceMod", "1.0.0")]
	public class BalanceMod : BaseUnityPlugin
	{

        public static ConfigWrapper<bool> BlazingDoTFixEnabled { get; set; }
        public static ConfigWrapper<bool> WakeOfVulturesFixEnabled { get; set; }
        public static ConfigWrapper<bool> ItemBalanceEnabled { get; set; }
        public static ConfigWrapper<bool> ArtificerMoveSpeedBuffEnabled { get; set; }
        public static ConfigWrapper<bool> ArtificerM1CoeffBuffEnabled { get; set; }

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

            Hooks.Hook();
        }
    }
}