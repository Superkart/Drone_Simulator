// Copyright Epic Games, Inc. All Rights Reserved.

using EpicGame;
using Gauntlet;
using System.Collections.Generic;
using UnrealBuildTool;

namespace CitySampleTest
{
	/// <summary>
	/// CI testing
	/// </summary>
	public class AutoTest : CitySampleTestNode
	{
		public AutoTest(Gauntlet.UnrealTestContext InContext)
			: base(InContext)
		{
		}

		public override CitySampleTestConfig GetConfiguration()
		{
			CitySampleTestConfig Config = base.GetConfiguration();
			Config.MaxDuration = Context.TestParams.ParseValue("MaxDuration", 60 * 60);  // 1 hour max

			UnrealTestRole ClientRole = Config.RequireRole(UnrealTargetRole.Client);
            ClientRole.Controllers.Add("AutoTest");

			ClientRole.CommandLineParams.AddOrAppendParamValue("execcmds", "CitySampleTest.MaxRunCount " + Config.MaxRunCount);

			if (Config.SkipIntro)
			{
				ClientRole.CommandLineParams.Add("DisableSandboxIntro");
			}

			ClientRole.CommandLineParams.AddOrAppendParamValue("execcmds", "CitySampleTest.SoakTime " + Config.SoakTime);

			if (Config.SkipTestSequence)
			{
				ClientRole.CommandLineParams.AddOrAppendParamValue("execcmds", "CitySampleTest.SkipTestSequence true");
			}

			if (Config.StatCommands)
			{
				ClientRole.CommandLineParams.AddOrAppendParamValue("execcmds", "stat fps, stat unitgraph");
			}
			
			if (Config.FPSChart)
			{
				ClientRole.CommandLineParams.Add("CitySampleTest.FPSChart");
			}
			
			if (Config.MemReport)
			{
				ClientRole.CommandLineParams.Add("CitySampleTest.MemReport");
			}

			if (Config.VideoCapture)
			{
				ClientRole.CommandLineParams.Add("CitySampleTest.VideoCapture");
			}

			ClientRole.CommandLineParams.AddOrAppendParamValue("logcmds", "LogHttp Verbose, LogCitySample Verbose, LogCitySampleUI Verbose");

			return Config;
		}

		protected override void InitHandledErrors()
        {
			base.InitHandledErrors();
			HandledErrors.Add(new HandledError("AutoTest", "AutoTest failure:", "LogCitySampleTest", true));
		}

		public override ITestReport CreateReport(TestResult Result, UnrealTestContext Context, UnrealBuildSource Build, IEnumerable<UnrealRoleResult> Artifacts, string ArtifactPath)
		{
			return base.CreateReport(Result, Context, Build, Artifacts, ArtifactPath);
		}
	}
}
