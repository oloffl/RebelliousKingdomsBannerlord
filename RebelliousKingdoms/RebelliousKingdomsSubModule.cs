using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using RebelliousKingdoms.Behaviors;
using RebelliousKingdoms.Models;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.SandBox.GameComponents.Party;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace RebelliousKingdoms
{
    public class RebelliousKingdomsSubModule : MBSubModuleBase
    {
	    protected static readonly NLog.Logger Log = NLog.LogManager.GetCurrentClassLogger();

	    protected override void OnSubModuleLoad()
	    {
		    NLog.Config.LoggingConfiguration logConfig = new NLog.Config.LoggingConfiguration();
		    NLog.Targets.FileTarget logFile = new NLog.Targets.FileTarget(LogFileTarget()) { FileName = LogFilePath() };

		    logConfig.AddRule(NLog.LogLevel.Debug, NLog.LogLevel.Fatal, logFile);
		    NLog.LogManager.Configuration = logConfig;
	    }

	    protected virtual string LogFileTarget()
	    {
		    return "RebelliousKingdomsLogFile";
	    }

	    protected virtual string LogFilePath()
	    {
		    // The default, relative path will place the log in $(GameFolder)\bin\Win64_Shipping_Client\
		    return "RebelliousKingdomsLog.txt";
	    }

	    protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
	    {
		    try
		    {
			    if (!(game.GameType is Campaign))
			    {
				    return;
			    }

				//Log.Info("OnGameStart");

				AddModels(gameStarterObject);

				Config config;
				using (StreamReader reader = new StreamReader(@"..\..\Modules\RebelliousKingdoms\RebelliousConfig.json")) // ../../Modules/RebelliousKingdoms/
				{
					//Log.Info("Loaded configuration");
				    string json = reader.ReadToEnd();
				    config = JsonConvert.DeserializeObject<Config>(json);
			    }

			    CampaignGameStarter initializer = (CampaignGameStarter) gameStarterObject;

			    initializer.AddBehavior(new RebelliousBehavior(config));
				initializer.AddBehavior(new CleanupBehavior());
			}
		    catch (Exception e)
		    {
				Log.Info("Exception on OnGameStart");
				Log.Error(e);
		    }
	    }

	    protected virtual void AddModels(IGameStarter gameStarterObject)
	    {
		    ReplaceModel<DefaultPartySizeLimitModel, FixedPartySizeLimitModel>(gameStarterObject);
	    }

	    protected void ReplaceModel<TBaseType, TChildType>(IGameStarter gameStarterObject)
		    where TBaseType : GameModel
		    where TChildType : TBaseType
	    {
		    if (!(gameStarterObject.Models is IList<GameModel> models))
		    {
				//Log.Error("Models was not a list");
				return;
		    }

		    bool found = false;
		    for (int index = 0; index < models.Count; ++index)
		    {
			    if (models[index] is TBaseType)
			    {
				    found = true;
				    if (!(models[index] is TChildType))
				    {
						//Log.Info($"Base model {typeof(TBaseType).Name} found. Replacing with child model {typeof(TChildType).Name}");
						models[index] = Activator.CreateInstance<TChildType>();
						
				    }
					//else
					//{
					//	Log.Info($"Child model {typeof(TChildType).Name} found, skipping.");
					//}
				}
		    }

		    if (!found)
		    {
				//Log.Info($"Base model {typeof(TBaseType).Name} was not found. Adding child model {typeof(TChildType).Name}");
				gameStarterObject.AddModel(Activator.CreateInstance<TChildType>());
		    }
	    }
	}

    public class Config
    {
		public int FortificationRebellionLimit;
		public int RebellionChanceModifier;
		public int MinimumChanceModifier;
		public bool OnlyRebelInDifferentCultureForts;
		public bool OnlySiegeCastles;
    }
}
