using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.ObjectSystem;

namespace RebelliousKingdoms.Behaviors
{
	public class CleanupBehavior : CampaignBehaviorBase
	{
		protected static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		private static readonly Random Rand = new Random();

		public CleanupBehavior()
		{ }

		public override void RegisterEvents()
		{
			try
			{
				CampaignEvents.DailyTickEvent
					.AddNonSerializedListener(this, FreeClansFromKingdom);
			}
			catch
			{
				Log.Info("Exception in Register Events");
			}
		}

		public override void SyncData(IDataStore dataStore)
		{ }

		private void FreeClansFromKingdom()
		{
			try
			{
				foreach (Clan clan in Campaign.Current.Clans)
				{
					if(clan?.Leader == null)
						continue;

					if (clan.Leader.IsHumanPlayerCharacter)
						continue;

					Kingdom kingdom = clan.Kingdom;

					if (kingdom == null)
						continue;

					if (!kingdom.IsKingdomFaction || kingdom.IsMinorFaction || kingdom.IsBanditFaction)
						continue;

					if(clan.IsMinorFaction || clan.IsClanTypeMercenary || clan.IsBanditFaction)
						continue;

					if (clan.Fortifications.Count > 0)
						continue;

					if (clan.Leader?.PartyBelongedTo?.BesiegedSettlement != null)
						continue;

					bool otherClanHasForts = false;
					foreach (Clan other in kingdom.Clans)
					{
						if (other.Fortifications.Count > 0)
						{
							otherClanHasForts = true;
							break;
						}
					}

					if (otherClanHasForts)
						continue;

					// We have now determined that the clan and possible other clans in the kingdom have no forts
					// So we can go ahead an make the clan available to join other Kingdoms or die


					int lowestClanCount = int.MaxValue;
					Kingdom newKingdom = null;

					Dictionary<string, Kingdom> allKingdoms = new Dictionary<string, Kingdom>();
					foreach(Clan clan1 in Campaign.Current.Clans)
					{
						if (clan1?.Kingdom == null) 
							continue;

						allKingdoms[clan1.Kingdom.StringId] = clan1.Kingdom;
					}

					foreach (KeyValuePair<string, Kingdom> weakest in allKingdoms)
					{
						if (lowestClanCount > weakest.Value.Clans.Count
						    && !weakest.Value.StringId.Equals(kingdom.StringId)
						    && weakest.Value.Clans.Count != 0
						    && weakest.Value.Fortifications.Count() != 0)
						{
							lowestClanCount = weakest.Value.Clans.Count;
							newKingdom = weakest.Value;
						}
					}

					double surviveChance = 50;

					float diceRoll = Rand.Next(0, 100);

					if (diceRoll < surviveChance && newKingdom?.Clans.Count <= 3)
					{
						ChangeKingdomAction.ApplyByJoinToKingdom(clan, newKingdom, false);
					}
					else
					{
						clan.ClanLeaveKingdom(true);
						KillCharacterAction.ApplyByRemove(clan.Leader);

						// Forces so many annoying popups
						DestroyClanAction.Apply(clan);
					}

					// Forces so many annoying popups
					//if (kingdom.Clans.Count == 0)
					//	DestroyKingdomAction.Apply(kingdom);
				}
			}
			catch (Exception e)
			{
				Log.Info("Exception in FreeClansFromKingdom");
				Log.Info(e);
			}
		}
	}
}
