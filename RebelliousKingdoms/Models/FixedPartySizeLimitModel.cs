using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace RebelliousKingdoms.Models
{
	public class FixedPartySizeLimitModel : DefaultPartySizeLimitModel
    {
        private readonly TextObject _leadershipSkillLevelBonusText = GameTexts.FindText("str_leadership_skill_level_bonus", (string)null);
        private readonly TextObject _leadershipPerkUltimateLeaderBonusText = GameTexts.FindText("str_leadership_perk_bonus", (string)null);
        private readonly TextObject _baseSizeText = GameTexts.FindText("str_base_size", (string)null);
        private readonly TextObject _clanTierText = GameTexts.FindText("str_clan_tier_bonus", (string)null);
        private readonly TextObject _renownText = GameTexts.FindText("str_renown_bonus", (string)null);
        private readonly TextObject _clanLeaderText = GameTexts.FindText("str_clan_leader_bonus", (string)null);
        private readonly TextObject _factionLeaderText = GameTexts.FindText("str_faction_leader_bonus", (string)null);
        private readonly TextObject _leaderLevelText = GameTexts.FindText("str_leader_level_bonus", (string)null);
        private readonly TextObject _townBonusText = GameTexts.FindText("str_town_bonus", (string)null);
        private readonly TextObject _minorFactionText = GameTexts.FindText("str_minor_faction_bonus", (string)null);
        private readonly TextObject _currentPartySizeBonusText = GameTexts.FindText("str_current_party_size_bonus", (string)null);
        private readonly TextObject _randomSizeBonusTemporary = new TextObject("{=hynFV8jC}Extra size bonus (Perk-like Effect)", (Dictionary<string, TextObject>)null);
        private const int BaseMobilePartySize = 20;
        private const int BasePrisonerSize = 10;
        private const int BaseGarrisonPartySize = 200;
        private const int TownGarrisonSizeBonus = 100;
        private static int _additionalPartySizeAsCheat;
        private static int _additionalPrisonerSizeAsCheat;
        private TextObject _quarterMasterText;

        public FixedPartySizeLimitModel()
        {
            this._quarterMasterText = GameTexts.FindText("str_clan_role", "quartermaster");
        }

        public override int GetPartyMemberSizeLimit(PartyBase party, StatExplainer explanation = null)
        {
            if (!party.IsMobile)
                return 0;
            return party.MobileParty.IsGarrison ? this.CalculateGarrisonPartySizeLimit(party.MobileParty, explanation) : this.CalculateMobilePartyMemberSizeLimit(party.MobileParty, explanation);
        }

        public override int GetPartyPrisonerSizeLimit(PartyBase party, StatExplainer explanation = null)
        {
            return this.CalculateMobilePartyPrisonerSizeLimitInternal(party, explanation);
        }

        private int CalculateMobilePartyMemberSizeLimit(MobileParty party, StatExplainer explanation = null)
        {
            ExplainedNumber result = new ExplainedNumber(0.0f, explanation, (TextObject)null);
            result.Add(20f, this._baseSizeText);
            if (party.LeaderHero != null && !party.IsCaravan)
            {
                if (party.MapFaction != null && party.MapFaction.IsKingdomFaction && party.LeaderHero.MapFaction.Leader == party.LeaderHero)
                    result.Add(20f, this._factionLeaderText);
                if (party.LeaderHero.Clan.Leader == party.LeaderHero)
                {
                    if (party.LeaderHero.Clan.Tier >= 4 && party.MapFaction.IsKingdomFaction && ((Kingdom)party.MapFaction).ActivePolicies.Contains(DefaultPolicies.NobleRetinues))
                        result.Add(50f, DefaultPolicies.NobleRetinues.Name);
                    if (party.MapFaction.IsKingdomFaction && party.MapFaction.Leader == party.LeaderHero && ((Kingdom)party.MapFaction).ActivePolicies.Contains(DefaultPolicies.RoyalGuard))
                        result.Add(80f, DefaultPolicies.RoyalGuard.Name);
                    if (party.LeaderHero.Clan.Tier > 0)
                    {
                        int num = !party.LeaderHero.Clan.IsMinorFaction || party.LeaderHero.Clan == Clan.PlayerClan ? 25 : 25;
                        result.Add((float)(party.LeaderHero.Clan.Tier * num), this._clanTierText);
                    }
                }
                else if (party.LeaderHero.Clan.Tier > 0)
                {
                    int num = !party.LeaderHero.Clan.IsMinorFaction || party.LeaderHero.Clan == Clan.PlayerClan ? 15 : 15;
                    result.Add((float)(party.LeaderHero.Clan.Tier * num), this._clanTierText);
                }
                result.Add((float)(party.EffectiveQuartermaster.GetSkillValue(DefaultSkills.Steward) / 4), this._quarterMasterText);
                this.AddMobilePartyLeaderPartySizePerkEffects(party, ref result);
                this.AddUltimateLeaderPerkEffect(party, ref result);
                if (FixedPartySizeLimitModel._additionalPartySizeAsCheat != 0 && party.IsMainParty)
                    result.Add((float)FixedPartySizeLimitModel._additionalPartySizeAsCheat, new TextObject("{=V6R0wk6N}Additional size from extra party cheat", (Dictionary<string, TextObject>)null));
            }
            else if (party.IsCaravan)
            {
                if (party.Party.Owner == Hero.MainHero)
                {
                    result.Add(10f, this._randomSizeBonusTemporary);
                }
                else
                {
                    Hero owner = party.Party.Owner;
                    if ((owner != null ? (owner.IsNotable ? 1 : 0) : 0) != 0)
                        result.Add((float)(10 * (party.Party.Owner.Power < 100 ? 1 : (party.Party.Owner.Power < 200 ? 2 : 3))), this._randomSizeBonusTemporary);
                }
            }
            else if (party.IsVillager)
                result.Add(40f, this._randomSizeBonusTemporary);
            return (int)result.ResultNumber;
        }

        private int CalculateGarrisonPartySizeLimit(MobileParty party, StatExplainer explanation)
        {
            ExplainedNumber result = new ExplainedNumber(0.0f, explanation, (TextObject)null);
            result.Add(200f, this._baseSizeText);
            result.Add((float)this.GetLeadershipSkillLevelEffect(party, LimitType.GarrisonPartySizeLimit), this._leadershipSkillLevelBonusText);
            result.Add((float)this.GetTownBonus(party), this._townBonusText);
            this.AddGarrisonOwnerPerkEffects(party.CurrentSettlement, ref result);
            this.AddSettlementProjectBonuses(party.Party, ref result);
            return (int)result.ResultNumber;
        }

        private int CalculateMobilePartyPrisonerSizeLimitInternal(
          PartyBase party,
          StatExplainer explanation)
        {
            ExplainedNumber result = new ExplainedNumber(0.0f, explanation, (TextObject)null);
            result.Add(10f, this._baseSizeText);
            result.Add((float)this.GetCurrentPartySizeEffect(party), this._currentPartySizeBonusText);
            this.AddMobilePartyLeaderPrisonerSizePerkEffects(party, ref result);
            if (FixedPartySizeLimitModel._additionalPrisonerSizeAsCheat != 0 && party.IsMobile && party.MobileParty.IsMainParty)
                result.Add((float)FixedPartySizeLimitModel._additionalPrisonerSizeAsCheat, new TextObject("{=eaSlwKRY}Additional size from extra prisoner cheat", (Dictionary<string, TextObject>)null));
            return (int)result.ResultNumber;
        }

        private int GetLeadershipSkillLevelEffect(
          MobileParty party,
          FixedPartySizeLimitModel.LimitType type)
        {
            Hero hero = party.IsGarrison ? party?.CurrentSettlement?.OwnerClan?.Leader : party.LeaderHero;
            if (hero == null)
                return 0;
            ExplainedNumber stat = new ExplainedNumber(1f, (StringBuilder)null);
            if (type == FixedPartySizeLimitModel.LimitType.GarrisonPartySizeLimit)
                SkillHelper.AddSkillBonusForCharacter(DefaultSkills.Leadership, DefaultSkillEffects.LeadershipGarrisonSizeBonus, hero.CharacterObject, ref stat, true);
            return MathF.Round(stat.ResultNumber - 1f);
        }

        private void AddMobilePartyLeaderPartySizePerkEffects(
          MobileParty party,
          ref ExplainedNumber result)
        {
            CharacterObject leader = party.Leader;
        }

        private void AddUltimateLeaderPerkEffect(MobileParty party, ref ExplainedNumber result)
        {
            if (party.LeaderHero == null || !party.LeaderHero.Clan.IsMapFaction || (party.LeaderHero.Clan.Leader.GetSkillValue(DefaultSkills.Leadership) <= 250 || !party.LeaderHero.Clan.Leader.GetPerkValue(DefaultPerks.Leadership.UltimateLeader)))
                return;
            result.Add((float)(party.LeaderHero.Clan.Leader.GetSkillValue(DefaultSkills.Leadership) - 250) * DefaultPerks.Leadership.UltimateLeader.PrimaryBonus, this._leadershipPerkUltimateLeaderBonusText);
        }

        private void AddMobilePartyLeaderPrisonerSizePerkEffects(
          PartyBase party,
          ref ExplainedNumber result)
        {
            CharacterObject leader = party.Leader;
        }

        private void AddGarrisonOwnerPerkEffects(
          Settlement currentSettlement,
          ref ExplainedNumber result)
        {
            if (currentSettlement == null || !currentSettlement.IsTown)
                return;
            PerkHelper.AddPerkBonusForTown(DefaultPerks.TwoHanded.GarrisonCapacity, currentSettlement.Town, ref result);
        }

        public override int GetTierPartySizeEffect(int tier)
        {
            return tier >= 1 ? 15 * tier : 0;
        }

        private void AddSettlementProjectBonuses(PartyBase party, ref ExplainedNumber result)
        {
            if (party?.Owner?.HomeSettlement == null)
                return;
            Settlement homeSettlement = party.Owner.HomeSettlement;
            if (!homeSettlement.IsTown && !homeSettlement.IsCastle)
                return;
            foreach (Building building in homeSettlement.Town.Buildings)
            {
                int buildingEffectAmount = building.GetBuildingEffectAmount(DefaultBuildingEffects.GarrisonCapacity);
                if (buildingEffectAmount > 0)
                    result.Add((float)buildingEffectAmount, building.Name);
            }
        }

        private int GetTownBonus(MobileParty party)
        {
            Settlement homeSettlement = party.HomeSettlement;
            return homeSettlement.IsFortification && homeSettlement.IsTown ? 100 : 0;
        }

        private int GetCurrentPartySizeEffect(PartyBase party)
        {
            return party.NumberOfHealthyMembers / 2;
        }

        private enum LimitType
        {
            MobilePartySizeLimit,
            GarrisonPartySizeLimit,
            PrisonerSizeLimit,
        }
    }
}
