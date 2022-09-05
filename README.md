# LunarDrawback

Add drawbacks to spending lunar coins. Currently only adds time to the run every time you spend lunar coins.

## Configuration

### Time Drawback
- `Additional Time` : Time (in seconds) added when spending lunar coins. (Default: `30.0`)
- `Mode` : Defines how time is added. (Default: `PerCoin`)
	- `PerCoin`: Multiplies time added per coin spent. 
	- `One`: Time added doesn't depend on amount of coin spent.
- `Player Amount Scale` : Scales time added by player amount. Set to 0 or negative to disable. (Default: `0.0`)
	- Formula: `TimeAdded * PlayerAmount * PlayerAmountScale`
- `Display Time Cost` : Overrides Lunar Cost Display to also display time cost. (Default : `true`)

## Changelog

**1.0.0**

* First Release

## Credits

Original idea by [gamerH_ost](https://www.twitch.tv/gamerh_0st).