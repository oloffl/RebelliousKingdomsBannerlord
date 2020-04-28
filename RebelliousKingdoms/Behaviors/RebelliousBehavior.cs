
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace RebelliousKingdoms.Behaviors
{
	public class RebelliousBehavior : CampaignBehaviorBase
	{
		protected static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

		private static readonly Random Rand = new Random();

		private static int FortificationRebellionLimit { get; set; }
		private static int RebellionChanceModifier { get; set; }
		private static int MinimumChanceModifier { get; set; }
		private static bool OnlyRebelInDifferentCultureForts { get; set; }
		private static bool OnlySiegeCastles { get; set; }

		public RebelliousBehavior(Config config)
		{
			FortificationRebellionLimit = config.FortificationRebellionLimit;
			RebellionChanceModifier = config.RebellionChanceModifier;
			MinimumChanceModifier = config.MinimumChanceModifier;
			OnlyRebelInDifferentCultureForts = config.OnlyRebelInDifferentCultureForts;
			OnlySiegeCastles = config.OnlySiegeCastles;
		}

		public override void RegisterEvents()
		{
			try
			{
				CampaignEvents
					.WeeklyTickEvent.AddNonSerializedListener(this, ShatterRandomClan);
				CampaignEvents
					.MapEventEnded.AddNonSerializedListener(this, OnSiegeEnded);
			}
			catch (Exception e)
			{
				Log.Info("Exception in Register Events");
				Log.Error(e);
			}
		}

		public override void SyncData(IDataStore dataStore)
		{ }

		private void ShatterRandomClan()
		{
			try
			{
				foreach (Kingdom kingdom in Campaign.Current.Kingdoms)
				{
					// Initial null check
					if (kingdom == null)
						continue;

					// Do not rebel in player fortifications, because it's probably annoying. 
					if (kingdom.Leader.IsHumanPlayerCharacter)
						continue;

					// Only rebel in kingdoms where fortification count is over a specified limit
					if (kingdom.Fortifications.Count() < FortificationRebellionLimit)
						continue;

					// The more fortifications, the higher the risk of rebellion
					float x = kingdom.Fortifications.Count();

					float rebelChance = ((x - FortificationRebellionLimit) * (x - FortificationRebellionLimit) / RebellionChanceModifier) + MinimumChanceModifier;

					float diceRoll = Rand.Next(0, 100);

					if (diceRoll > rebelChance)
						continue;
					
					foreach (Clan clan in kingdom.Clans)
					{
						// Initial null checks
						if (clan?.Leader == null || clan.Kingdom == null)
							continue;

						// Do not rebel in leader's holdings
						//if (clan.Leader.GetName().ToString().Equals(kingdom.Leader.GetName().ToString()))
						//	continue;

						// Clan needs at least one fortification
						if (clan.Fortifications.Count < 1)
							continue;

						Settlement settlementTarget = clan
							.Fortifications[Rand.Next(clan.Fortifications.Count - 1)].Settlement;

						// Prefer settlements with different culture
						foreach (Town possibleTarget in clan.Fortifications)
						{
							if (possibleTarget.Culture.GetCultureCode() != kingdom.Culture.GetCultureCode())
							{
								settlementTarget = possibleTarget.Settlement;
							}
						}

						// If config only allows rebellion in different settlement culture
						if (OnlyRebelInDifferentCultureForts
						    && settlementTarget.Culture.ToString().Equals(kingdom.Culture.ToString()))
							continue;

						if(OnlySiegeCastles && !settlementTarget.IsCastle)
							continue;

						// Do not rebel if settlement is already under siege
						if (settlementTarget.IsUnderSiege)
							continue;

						Hero newLeader = CreateRandomLeader(clan, settlementTarget);

						MobileParty rebelParty = FillPartyWithTroopsAndInit(newLeader, settlementTarget);

						InitializeClan(newLeader, settlementTarget.Name.ToString());

						InitializeKingdom(newLeader, settlementTarget);

						InitializeSiege(newLeader, clan, settlementTarget, rebelParty);

						// Break after one successful settlement rebellion so as not to overwhelm a kingdom completely
						break;
					}
				}
			}
			catch (Exception e)
			{
				Log.Info("Exception in the ShatterRandomClan");
				Log.Error(e);
			}
		}

		private static Hero CreateRandomLeader(Clan clan, Settlement origin)
		{
			Hero specialHero = null;

			try
			{
				CharacterObject templateBase = clan.Leader.CharacterObject;

				CharacterObject template = CharacterObject.Templates.Where(x => 
					origin.Culture == x.Culture 
					&& x.Occupation == Occupation.Lord 
					|| x.Occupation == Occupation.Lady).GetRandomElement();

				template.InitializeEquipmentsOnLoad(templateBase.AllEquipments.ToList());

				specialHero = HeroCreator.CreateSpecialHero(template,
					origin, clan, null, -1);

				specialHero.StringId = Campaign.Current.Heroes[Campaign.Current.Heroes.Count - 1].StringId +
				                       Rand.Next(int.MaxValue);

				specialHero.Name = NameGenerator.Current.GenerateHeroFirstName(specialHero, true);

				//TextObject encyclopediaText = new TextObject("{=!}{RULER} has stretched their borders too thin. Usurper {REBEL} from {SETTLEMENT} is taking advantage of it.");
				//encyclopediaText.SetTextVariable("REBEL", specialHero.Name);
				//encyclopediaText.SetTextVariable("SETTLEMENT", origin.Name);
				//encyclopediaText.SetTextVariable("RULER", clan.Kingdom.Leader.Name);

				//specialHero.EncyclopediaText = encyclopediaText;

				specialHero.ChangeState(Hero.CharacterStates.NotSpawned);

				specialHero.IsMinorFactionHero = false;

				specialHero.IsNoble = true;

				specialHero.BornSettlement = origin;

				specialHero.UpdateHomeSettlement();

				specialHero.AddSkillXp(SkillObject.GetSkill(0), Rand.Next(80000, 500000)); // One Handed
				specialHero.AddSkillXp(SkillObject.GetSkill(2), Rand.Next(80000, 500000)); // Pole Arm
				specialHero.AddSkillXp(SkillObject.GetSkill(6), Rand.Next(80000, 500000)); // Riding
				specialHero.AddSkillXp(SkillObject.GetSkill(7), Rand.Next(80000, 500000)); // Athletics
				specialHero.AddSkillXp(SkillObject.GetSkill(9), Rand.Next(80000, 500000)); // Tactics
				specialHero.AddSkillXp(SkillObject.GetSkill(13), Rand.Next(80000, 500000)); // Leadership
				specialHero.AddSkillXp(SkillObject.GetSkill(15), Rand.Next(80000, 500000)); // Steward
				specialHero.AddSkillXp(SkillObject.GetSkill(17), Rand.Next(80000, 500000)); // Engineering

				specialHero.ChangeState(Hero.CharacterStates.Active);

				specialHero.Gold += 100000;

				MBObjectManager.Instance.RegisterObject(specialHero);
			}
			catch (Exception e)
			{
				Log.Info("Exception when trying to create new Hero");
				Log.Error(e);
			}

			return specialHero;
		}

		private static MobileParty FillPartyWithTroopsAndInit(Hero leader, Settlement target)
		{
			MobileParty rebelParty = MBObjectManager.Instance.CreateObject<MobileParty>(leader.CharacterObject.Name.ToString() + "_" + leader.Id);

			try
			{
				rebelParty.Initialize();

				TroopRoster roster = new TroopRoster();

				int basicTroopAmount = 128;
				TroopRoster basicUnits = new TroopRoster();

				basicUnits.AddToCounts(leader.Culture.BasicTroop, basicTroopAmount);

				foreach (var upgrade1 in leader.Culture.BasicTroop.UpgradeTargets)
				{
					basicUnits.AddToCounts(upgrade1, basicTroopAmount / 2);

					foreach (var upgrade2 in upgrade1.UpgradeTargets)
					{
						basicUnits.AddToCounts(upgrade2, basicTroopAmount / 2);

						foreach (var upgrade3 in upgrade2.UpgradeTargets)
						{
							basicUnits.AddToCounts(upgrade3, basicTroopAmount / 4);

							foreach (var upgrade4 in upgrade3.UpgradeTargets)
							{
								basicUnits.AddToCounts(upgrade4, basicTroopAmount / 8);
							}
						}
					}
				}

				roster.Add(basicUnits);

				int eliteTroopAmount = 64;
				TroopRoster eliteUnits = new TroopRoster();
				eliteUnits.AddToCounts(leader.Culture.EliteBasicTroop, eliteTroopAmount);

				foreach (var upgrade1 in leader.Culture.EliteBasicTroop.UpgradeTargets)
				{
					eliteUnits.AddToCounts(upgrade1, eliteTroopAmount / 2);

					foreach (var upgrade2 in upgrade1.UpgradeTargets)
					{
						eliteUnits.AddToCounts(upgrade2, eliteTroopAmount / 2);

						foreach (var upgrade3 in upgrade2.UpgradeTargets)
						{
							eliteUnits.AddToCounts(upgrade3, eliteTroopAmount / 4);

							foreach (var upgrade4 in upgrade3.UpgradeTargets)
							{
								eliteUnits.AddToCounts(upgrade4, eliteTroopAmount / 8);
							}
						}
					}
				}

				roster.Add(eliteUnits);

				TroopRoster prisoners = new TroopRoster
				{
					IsPrisonRoster = true
				};

				rebelParty.Party.Owner = leader;

				rebelParty.MemberRoster.AddToCounts(leader.CharacterObject, 1, false, 0, 0, true, 0);

				rebelParty.SetAsMainParty();

				rebelParty.InitializeMobileParty(new TextObject(
						leader.CharacterObject.GetName().ToString(), null),
					roster,
					prisoners,
					target.GatePosition,
					0.0f,
					0.0f);

				foreach (ItemObject item in ItemObject.All)
				{
					if (item.IsFood)
					{
						rebelParty.ItemRoster.AddToCounts(item, 150);
						break;
					}
				}

				rebelParty.HomeSettlement = target.BoundVillages[0].Settlement;

				rebelParty.Quartermaster = leader;
			}
			catch (Exception e)
			{
				Log.Info("Exception when trying to create new Army");
				Log.Error(e);
			}

			return rebelParty;
		}

		private void InitializeClan(Hero leader, string origin)
		{
			Clan newClan = MBObjectManager.Instance.CreateObject<Clan>();

			try
			{
				newClan.Culture = leader.Culture;

				TextObject name = new TextObject("{=!}{CLAN_NAME}");

				origin = origin.Replace(" Castle", "");

				name.SetTextVariable("CLAN_NAME", leader.Name + " of " + origin);

				newClan.AddRenown(900, false);

				newClan.SetLeader(leader);

				leader.Clan = newClan;

				newClan.InitializeClan(name,
					name,
					leader.Culture,
					Banner.CreateRandomClanBanner(leader.StringId.GetDeterministicHashCode()));
			}
			catch (Exception e)
			{
				Log.Info("Exception in InitializeClan");
				Log.Error(e);
			}
		}

		private void InitializeKingdom(Hero leader, Settlement target)
		{
			Kingdom newKingdom = MBObjectManager.Instance.CreateObject<Kingdom>();
			try
			{
				TextObject name = new TextObject("{=!}{CLAN_NAME}");

				string origin = target.Name.ToString().Replace("Castle", "").Trim();

				name.SetTextVariable("CLAN_NAME", leader.Name + " of " + origin);

				newKingdom.InitializeKingdom(name,
					name,
					leader.Culture,
					Banner.CreateRandomClanBanner(leader.StringId.GetDeterministicHashCode()),
					0,
					0,
					new Vec2(target.GatePosition.X, target.GatePosition.Y));

				ChangeKingdomAction.ApplyByJoinToKingdom(leader.Clan, newKingdom, false);
				newKingdom.RulingClan = leader.Clan;

				newKingdom.AddPolicy(DefaultPolicies.NobleRetinues);

				MBObjectManager.Instance.RegisterObject(newKingdom);
			}
			catch (Exception e)
			{
				Log.Info("Exception in InitializeKingdom");
				Log.Error(e);
			}
		}

		private void InitializeSiege(Hero leader, Clan clan, Settlement target, MobileParty party)
		{
			try
			{
				FactionManager.DeclareWar(leader.MapFaction, target.MapFaction);

				Campaign.Current.FactionManager.RegisterCampaignWar(leader.MapFaction, target.MapFaction);

				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leader, clan.Leader, -20, false);
				
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(leader, clan.Kingdom.Leader, -20, false);

				party.Ai.SetDoNotMakeNewDecisions(true);

				SetPartyAiAction.GetActionForBesiegingSettlement(party, target);
			}
			catch (Exception e)
			{
				Log.Info("Exception when trying to siege settlement");
				Log.Error(e);
			}
		}

		private void OnSiegeEnded(MapEvent mapEvent)
		{
			if (mapEvent?.InvolvedParties == null)
				return;

			try
			{
				foreach (PartyBase party in mapEvent.InvolvedParties)
				{
					if (party?.MobileParty?.Ai == null)
						continue;

					if (party.MobileParty.Ai.DoNotMakeNewDecisions)
						party.MobileParty.Ai.SetDoNotMakeNewDecisions(false);
				}
			}
			catch (Exception e)
			{
				Log.Info("Exception when trying to end siege");
				Log.Error(e);
			}
		}
	}
}

