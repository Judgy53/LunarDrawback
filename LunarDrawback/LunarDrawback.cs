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
        private ConfigEntry<bool> _configTimeDisplay;

        private CostTypeDef.PayCostDelegate _lunarCoinPayCostDefaultDelegate;
        private CostTypeDef.BuildCostStringDelegate _lunarCoinBuildCostStringDefaultDelegate;

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
            _configTimeDisplay = Config.Bind("Time Drawback", "Display Time Cost", true, "Overrides Lunar Cost Display to also display time cost.");
        }

        private void OnDisable()
        {
            CostTypeDef lunarCoinDef = CostTypeCatalog.GetCostTypeDef(CostTypeIndex.LunarCoin);

            if (lunarCoinDef != null)
            {
                lunarCoinDef.payCost = _lunarCoinPayCostDefaultDelegate;
                lunarCoinDef.buildCostString = _lunarCoinBuildCostStringDefaultDelegate;
            }
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

            if(lunarCoinDef != null)
            {
#pragma warning disable Publicizer001 // Accessing a member that was not originally public
                _lunarCoinPayCostDefaultDelegate = lunarCoinDef.payCost;
                _lunarCoinBuildCostStringDefaultDelegate = lunarCoinDef.buildCostString;
#pragma warning restore Publicizer001 // Accessing a member that was not originally public

                //Override paycost handling
                lunarCoinDef.payCost = delegate (CostTypeDef costTypeDef, CostTypeDef.PayCostContext context)
                {
                    _lunarCoinPayCostDefaultDelegate(costTypeDef, context);
                    OnPaidLunarCoins(context.cost);
                };

                //Override string display
                lunarCoinDef.buildCostString = delegate (CostTypeDef costTypeDef, CostTypeDef.BuildCostStringContext context)
                {
                    _lunarCoinBuildCostStringDefaultDelegate(costTypeDef, context);
                    if(_configTimeDisplay.Value == true)
                        context.stringBuilder.Append($" & {GetAdditionalTime(context.cost).ToString("0.#")} seconds");
                };
            }
        }
    }
}
