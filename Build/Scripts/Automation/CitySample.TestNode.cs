// Copyright Epic Games, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EpicGame;
using Gauntlet;

namespace CitySampleTest
{
	public class CitySampleTestConfig : UnrealTestConfiguration
	{
		/// <summary>
		/// Total number of loops to run during the test.
		/// </summary>
		[AutoParamWithNames(1, "CitySampleTest.MaxRunCount")]
		public int MaxRunCount;

		/// <summary>
		/// If true, skips the sandbox intro sequence at the start of a run.
		/// @note Has no effect in shipping builds.
		/// </summary>
		[AutoParamWithNames(false, "CitySampleTest.SkipIntro")]
		public bool SkipIntro;

		/// <summary>
		/// How long to spend in the sandbox, in seconds.
		/// </summary>
		[AutoParamWithNames(30.0f, "CitySampleTest.SoakTime")]
		public float SoakTime;

		/// <summary>
		/// Whether to run the test sequence after the sandbox soak or not.
		/// </summary>
		[AutoParamWithNames(false, "CitySampleTest.SkipTestSequence")]
		public bool SkipTestSequence;

		/// <summary>
		/// If true, appends the stat fps and unitgraph to ExecCmds.
		/// </summary>
		[AutoParamWithNames(false, "CitySampleTest.StatCommands")]
		public bool StatCommands;

		/// <summary>
		/// If true, FPSChart start/end is automatically called for each run.
		/// </summary>
		[AutoParamWithNames(false, "CitySampleTest.FPSChart")]
		public bool FPSChart;

		/// <summary>
		/// If true, MemReport is automatically started each run and captured periodically.
		/// @note Use the CitySampleTest.MemReportInterval CVar to set the interval.
		/// </summary>
		[AutoParamWithNames(false, "CitySampleTest.MemReport")]
		public bool MemReport;

		/// <summary>
		/// If true, video captures are automatically started/ended each run.
		/// @note Requires the platform has an available gameplay media encoder.
		/// </summary>
		[AutoParamWithNames(false, "CitySampleTest.VideoCapture")]
		public bool VideoCapture;
	}

	public abstract class CitySampleTestNode : UnrealTestNode<CitySampleTestConfig>
	{
		public CitySampleTestNode(UnrealTestContext InContext) : base(InContext)
		{
			TestGuid = Guid.NewGuid();
			Gauntlet.Log.Info("Your Test GUID is :\n" + TestGuid.ToString() + '\n');

			InitHandledErrors();

			CitySampleTestLastLogCount = 0;
		}

		public override bool StartTest(int Pass, int InNumPasses)
		{
			CitySampleTestLastLogCount = 0;
			return base.StartTest(Pass, InNumPasses);
		}

		public class HandledError
		{
			public string ClientErrorString;
			public string GauntletErrorString;

			/// <summary>
			/// String name for the log category that should be used to filter errors. Defaults to null, i.e. no filter.
			/// </summary>
			public string CategoryName;

			// If error is verbose, will output debugging information such as state
			public bool Verbose;

			public HandledError(string ClientError, string GauntletError, string Category, bool VerboseIn = false)
			{
				ClientErrorString = ClientError;
				GauntletErrorString = GauntletError;
				CategoryName = Category;
				Verbose = VerboseIn;
			}
		}

		/// <summary>
		/// List of errors with special-cased gauntlet messages.
		/// </summary>
		public List<HandledError> HandledErrors { get; set; }

		/// <summary>
		/// Guid associated with each test run for ease of differentiation between different runs on same build.
		/// </summary>
		public Guid TestGuid { get; protected set; }

		/// <summary>
		/// Absolute path to CitySample data required during testing and report generation
		/// </summary>
		public static string DataPath { get { return Path.Combine(Globals.UnrealRootDir, "CitySampleGame/Test/Gauntlet/Data"); } }

		/// <summary>
		/// Set up the base list of possible expected errors, plus the messages to deliver if encountered.
		/// </summary>
		protected virtual void InitHandledErrors()
		{
			HandledErrors = new List<HandledError>();
		}

		/// <summary>
		/// Line count of the client log messages that have been written to the test logs.
		/// </summary>
		private int CitySampleTestLastLogCount;

		/// <summary>
		/// Periodically called while test is running. Updates logs.
		/// </summary>
		public override void TickTest()
		{
			IAppInstance App = null;

			if (TestInstance.ClientApps == null)
			{
				App = TestInstance.ServerApp;
			}
			else
			{
				if (TestInstance.ClientApps.Length > 0)
				{
					App = TestInstance.ClientApps.First();
				}
			}

			if (App != null)
			{
				UnrealLogParser Parser = new UnrealLogParser(App.StdOut);

				List<string> TestLines = Parser.GetLogChannel("CitySampleTest").ToList();
				
				for (int i = CitySampleTestLastLogCount; i < TestLines.Count; i++)
				{
					if (TestLines[i].StartsWith("LogCitySampleTest: Error:"))
					{
						ReportError(TestLines[i]);
					}
					else if (TestLines[i].StartsWith("LogCitySampleTest: Warning:"))
					{
						ReportWarning(TestLines[i]);
					}
					else
					{
						Log.Info(TestLines[i]);
					}
				}

				CitySampleTestLastLogCount = TestLines.Count;
			}

			base.TickTest();
		}

		/// <summary>
		/// Override this function for CitySample as we want to be able to use a per-branch config to ignore certain issues
		/// that were inherited from Main and will be addressed there
		/// </summary>
		/// <param name="InArtifacts"></param>
		/// <returns></returns>
		protected override UnrealLog CreateLogSummaryFromArtifact(UnrealRoleArtifacts InArtifacts)
		{
			UnrealLog LogSummary = base.CreateLogSummaryFromArtifact(InArtifacts);


			IgnoredIssueConfig IgnoredIssues = new IgnoredIssueConfig();

			string IgnoredIssuePath = string.Format(@"\\epicgames.net\root\Builds\Automation\CitySample\BranchSettings\{0}\IgnoredIssueList.json", Context.BuildInfo.Branch.Replace("/", "+"));

			if (!File.Exists(IgnoredIssuePath))
			{
				Log.Info("No IgnoredIssue Config found at {0}", IgnoredIssuePath);
			}
			else if (IgnoredIssues.LoadFromFile(IgnoredIssuePath))
			{
				Log.Info("Loaded IgnoredIssue config from {0}", IgnoredIssuePath);

				IEnumerable<UnrealLog.CallstackMessage> IgnoredEnsures = LogSummary.Ensures.Where(E => IgnoredIssues.IsEnsureIgnored(this.Name, E.Message));
				IEnumerable<UnrealLog.LogEntry> IgnoredWarnings = LogSummary.LogEntries.Where(E => E.Level == UnrealLog.LogLevel.Warning && IgnoredIssues.IsWarningIgnored(this.Name, E.Message));
				IEnumerable<UnrealLog.LogEntry> IgnoredErrors = LogSummary.LogEntries.Where(E => E.Level == UnrealLog.LogLevel.Error && IgnoredIssues.IsErrorIgnored(this.Name, E.Message));

				if (IgnoredEnsures.Any())
				{
					Log.Info("Ignoring {0} ensures.", IgnoredEnsures.Count());
					Log.Info("\t{0}", string.Join("\n\t", IgnoredEnsures.Select(E => E.Message)));
					LogSummary.Ensures = LogSummary.Ensures.Except(IgnoredEnsures).ToArray();
				}
				if (IgnoredWarnings.Any())
				{
					Log.Info("Ignoring {0} warnings.", IgnoredWarnings.Count());
					Log.Info("\t{0}", string.Join("\n\t", IgnoredWarnings.Select(E => E.Message)));
					LogSummary.LogEntries = LogSummary.LogEntries.Except(IgnoredWarnings).ToArray();
				}
				if (IgnoredErrors.Any())
				{
					Log.Info("Ignoring {0} errors.", IgnoredErrors.Count());
					Log.Info("\t{0}", string.Join("\n\t", IgnoredErrors.Select(E => E.Message)));
					LogSummary.LogEntries = LogSummary.LogEntries.Except(IgnoredErrors).ToArray();
				}
			}


			return LogSummary;
		}

		protected override UnrealProcessResult GetExitCodeAndReason(StopReason InReason, UnrealLog InLogSummary, UnrealRoleArtifacts InArtifacts, out string ExitReason, out int ExitCode)
		{
			// Check for login failure
			UnrealLogParser Parser = new UnrealLogParser(InArtifacts.AppInstance.StdOut);

			ExitReason = "";
			ExitCode = -1;

			foreach (HandledError ErrorToCheck in HandledErrors)
			{
				string[] MatchingErrors = Parser.GetErrors(ErrorToCheck.CategoryName).Where(E => E.Contains(ErrorToCheck.ClientErrorString)).ToArray();
				if (MatchingErrors.Length > 0)
				{
					ExitReason = string.Format("Test Error: {0} {1}", ErrorToCheck.GauntletErrorString, ErrorToCheck.Verbose ? "\"" + MatchingErrors[0] + "\"" : "");
					ExitCode = -1;
					return UnrealProcessResult.TestFailure;
				}
			}

			return base.GetExitCodeAndReason(InReason, InLogSummary, InArtifacts, out ExitReason, out ExitCode);
		}


		/// <summary>
		/// CreateReport() happens near the end of StopTest after SaveArtifacts(). Override this function within your test to set up external reporting.
		/// Include a base call so that ReportToDashboard() is called appropriately.
		/// </summary>
		public override ITestReport CreateReport(TestResult Result, UnrealTestContext Context, UnrealBuildSource Build, IEnumerable<UnrealRoleResult> Artifacts, string ArtifactPath)
		{
			return base.CreateReport(Result, Context, Build, RoleResults, ArtifactPath);
		}
	}
}
