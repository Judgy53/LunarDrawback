using BepInEx;
using BepInEx.Configuration;
using RoR2;
using UnityEngine.Networking;

namespace LunarDrawback
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class LunarDrawback : BaseUnityPlugin
    {
        public const string PluginGUID = PluginAuthor + "." + PluginName;
        public const string PluginAuthor = "Judgy";
        public const string PluginName = "LunarDrawback";
        public const string PluginVersion = "1.0.0";

        private enum TimeAddMode
        {
            PerCoin,
            One
        }

        private ConfigEntry<float> _configTimePerCoin;
        private ConfigEntry<TimeAddMode> _configTimeMode;
        private ConfigEntry<float> _configTimePlayerScale;

        public void Awake()
        {
            //Init our logging class so that we can properly log for debugging
            Log.Init(Logger);
            InitConfig();

            On.RoR2.CostTypeCatalog.Init += CostTypeCatalog_Init;

            Log.LogInfo(nameof(Awake) + " done.");
        }

        private void InitConfig()
        {
            _configTimePerCoin = Config.Bind("Time Drawback", "Additional Time", 30.0f, "Time (in seconds) added when spending lunar coins.");
            _configTimeMode = Config.Bind("Time Drawback", "Mode", TimeAddMode.PerCoin, "Defines how time is added.\n`PerCoin`: Multiplies time added per coin spent.\n`One`: Time added doesn't depend on amount of coin spent.");
            _configTimePlayerScale = Config.Bind("Time Drawback", "Player Amount Scale", 0.0f, "Scales time added by player amount. Set to 0 or negative to disable.\nFormula: TimeAdded * PlayerAmount * PlayerAmountScale");
        }

        private void OnPaidLunarCoins(int amount)
        {
            if (!NetworkServer.active)
                return;

            float currentTime = Run.instance.GetRunStopwatch();
            float addedTime = GetAdditionalTime(amount);

            Run.instance.SetRunStopwatch(currentTime + addedTime);
            Log.LogDebug($"Spent {amount} lunar coins. Added {addedTime} seconds (from {currentTime} to {currentTime + addedTime})");
        }

        private float GetAdditionalTime(int coinSpent)
        {
            float mult = coinSpent;
            if (_configTimeMode.Value == TimeAddMode.One)
                mult = 1;
            if (_configTimePlayerScale.Value > 0.0f)
                mult *= Run.instance.participatingPlayerCount * _configTimePlayerScale.Value;

            return _configTimePerCoin.Value * mult;
        }

        private void CostTypeCatalog_Init(On.RoR2.CostTypeCatalog.orig_Init orig)
        {
            orig();

            CostTypeDef lunarCoinDef = CostTypeCatalog.GetCostTypeDef(CostTypeIndex.LunarCoin);

#pragma warning disable Publicizer001 // Accessing a member that was not originally public
            CostTypeDef.PayCostDelegate lunarCoinDefaultDelegate = lunarCoinDef.payCost;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

            lunarCoinDef.payCost = delegate (CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
            {
                lunarCoinDefaultDelegate(costTypeDef, context);
                OnPaidLunarCoins(context.cost);
            };
        }
    }
}
