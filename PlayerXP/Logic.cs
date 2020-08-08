﻿using Exiled.API.Features;
using Hints;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PlayerXP
{
	partial class EventHandler
	{
		private const int baseXP = 1000;

		private void SendHint(Player player, string msg, float time = 3f)
		{
			player.HintDisplay.Show(new TextHint(msg, new HintParameter[] { new StringHintParameter("") }, HintEffectPresets.FadeInAndOut(0.25f, 1f, 0f), time));
		}

		private void AddXP(string userid, int xp, string msg = null)
		{
			if (pInfoDict.ContainsKey(userid))
			{
				PlayerInfo info = pInfoDict[userid];
				Player player = Player.Get(userid);
				info.xp += (int)(xp * PlayerXP.instance.Config.XpScale);
				if (msg != null) SendHint(player, $"<color=\"yellow\">{msg}</color>");
				int calc = (info.level - 1) * PlayerXP.instance.Config.XpIncrement + baseXP;
				if (info.xp >= calc)
				{
					info.xp -= calc;
					info.level++;
					SendHint(player, $"<color=\"yellow\"><b>You've leveled up to level {info.level}! You need {calc + PlayerXP.instance.Config.XpIncrement - info.xp} xp for your next level.</b></color>", 4f);
				}
				pInfoDict[userid] = info;
			}
			if (PlayerXP.instance.Config.IsDebug) Log.Info($"Giving {xp}xp to {Player.Get(userid).Nickname} ({userid}).");
		}

		private void RemoveXP(string userid, int xp, string msg = null)
		{
			if (pInfoDict.ContainsKey(userid))
			{
				PlayerInfo info = pInfoDict[userid];
				Player player = Player.Get(userid);
				info.xp -= xp;
				if (msg != null) SendHint(player, $"<color=\"yellow\">{msg}</color>", 2f);
				if (info.xp <= 0)
				{
					if (info.level > 1)
					{
						info.level--;
						info.xp = info.level * PlayerXP.instance.Config.XpIncrement + baseXP - Math.Abs(info.xp);
					}
					else
					{
						info.xp = 0;
					}
				}
				pInfoDict[userid] = info;
			}

			if (PlayerXP.instance.Config.IsDebug) Log.Info($"Removing {xp}xp from {Player.Get(userid).Nickname} ({userid}).");
		}

		private int GetLevel(string userid)
		{
			if (pInfoDict.ContainsKey(userid))
			{
				return pInfoDict[userid].level;
			}
			else return -1;
		}

		private int GetXP(string userid)
		{
			if (pInfoDict.ContainsKey(userid))
			{
				return pInfoDict[userid].xp;
			}
			else return -1;
		}

		private void SaveStats()
		{
			if (PlayerXP.instance.Config.IsDebug) Log.Info($"Saving stats for a total of {pInfoDict.Count} players.");
			foreach (KeyValuePair<string, PlayerInfo> info in pInfoDict)
			{
				if (PlayerXP.instance.Config.IsDebug) Log.Info($"Saving stats for {Player.Get(info.Key).Nickname}...");
				File.WriteAllText(Path.Combine(PlayerXP.XPPath, $"{info.Key}.json"), JsonConvert.SerializeObject(info.Value, Formatting.Indented));
			}
		}

		private int XpToLevelUp(string userid)
		{
			if (pInfoDict.ContainsKey(userid))
			{
				PlayerInfo info = pInfoDict[userid];
				return (info.level - 1) * PlayerXP.instance.Config.XpIncrement + baseXP + PlayerXP.instance.Config.XpIncrement - info.xp;
			}
			else return -1;
		}

		private void UpdateCache()
		{
			foreach (FileInfo file in new DirectoryInfo(PlayerXP.XPPath).GetFiles())
			{
				PlayerInfo info = JsonConvert.DeserializeObject<PlayerInfo>(File.ReadAllText(file.FullName));
				if (info.level == 1 && info.xp == 0)
				{
					File.Delete(file.FullName);
					continue;
				}
				string userid = file.Name.Replace(".json", "");
				info.karma = 1f;
				if (PlayerXP.instance.Config.IsDebug) Log.Info($"Loading cached stats for {info.name} ({userid})...");
				pInfoDict.Add(userid, info);
			}
			pInfoDict = pInfoDict.OrderByDescending(x => x.Value.level).ThenByDescending(x => x.Value.xp).ToDictionary(x => x.Key, x => x.Value);
		}

		private bool IsUnarmed(Player player)
		{
			foreach (var item in player.Inventory.items)
			{
				if (item.id == ItemType.GrenadeFrag || item.id == ItemType.GunCOM15 ||
					item.id == ItemType.GunE11SR || item.id == ItemType.GunLogicer ||
					item.id == ItemType.GunMP7 || item.id == ItemType.GunProject90 ||
					item.id == ItemType.GunUSP || item.id == ItemType.MicroHID ||
					item.id == ItemType.SCP018) return false;
			}
			return true;
		}
	}
}
