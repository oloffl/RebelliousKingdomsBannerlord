# Shattered Kingdoms

Every week the game rolls a die to see if a rebellion will happen within larger kingdoms. The larger the kingdom, the more likely a rebellion will strike. 

Help is always welcome, check out the known issues and make a pull request or report a new issue. 


### Changelog

1.3.0
* Only tested for Bannerlord Beta 1.3.0.
* Should be save game compatible, but it's not guaranteed.
* The mod has been split into two different files. One that ONLY shatters the world at a new campaign, and one that ONLY makes cities and castles rebel. 
* You can now adjust how often castles will rebel, if they can only rebel in cities that have a different culture, and if they can only rebel against castles. You can change this in RebelliousKingdoms/RebelliousConfig.json. Make sure you ONLY change the values, otherwise the configuration might become unreadable. See below. 
* Kingdoms that lose all their fortifications will now either join the weakest kingdom or become destroyed. I've put some measures in so as not to overwhelm the world with nobles. 
* Made rebellions possible in leader settlements as well.
* Rebel leader faces are now more randomized. 
* Rebel army now consists of a mix of recruits and elite units. 

1.0.3
* Modified chance to rebel to be lower but exponential. At 10 castles there's a 3% chance each week, 20 = 15%, 30 = 45%. Also removed the rebellion to be only for one castle/town, instead of all of them. 

1.0.2
* Dynamic rebellions now available, every week a die is rolled for each large kingdom. The larger the kingdom, the more likely a rebellion will strike. Multiple randomly generated kingdoms spawn and attack castles around the kingdom. They might win and become a new contender, or they will fail and will have weakend the kingdom's armies. 


### Modifying the RebellionConfig.json

Make sure you ONLY change the values, otherwise the configuration might become unreadable. ONLY use integers, no 10.5 for example. Do NOT remove or move the file.

Changes you make WILL take affect in previous saves, so you can change it, and load a save if you're not happy with the behavior. 

The more fortifications a kingdom has, the higher the risk of rebellion.

The formula is as follows, where x is the amount of fortifications a kingdom has.
rebelChance = ((x - FortificationRebellionLimit) * (x - FortificationRebellionLimit) / RebellionChanceModifier) + MinimumChanceModifier

Decreasing the RebellionChanceModifier will increase the growth chance of rebellion.

OnlyRebelInDifferentCultureForts can either be true or false

OnlySiegeCastles can either be true or false

### Installation

Drop the RebelliousKingdoms folder containing the SubModule.xml and bin folder in your M&B Bannerlord Modules folder, enable it in the launcher and create a new campaign.

Also check if the .dll in the bin folder is not blocked by windows by right clicking on it and checking properties.


### Known Issues

1. Annoying popups when clans are destroyed. 

### Reported Issues


### Thanks

Thanks to calsev for his template mod that got me started. https://github.com/calsev/bannerlord_smith_forever. 
Thanks to Etiennep for testing and coming with valuable feedback