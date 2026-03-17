// Copyright Epic Games, Inc. All Rights Reserved.

using AutomationTool;
using EpicGames.Core;
using Gauntlet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnrealBuildBase;
using UnrealBuildTool;
using Log = EpicGames.Core.Log;

namespace CitySampleTest
{
	/// <summary>
	/// CI testing
	/// </summary>
	public class PerformanceReport : AutoTest
	{
		string ReportCacheDirRoot_BuildMachine => @"\\epicgames.net\Root\Builds-Global\CitySample\PerfCache";
		string ReportCacheDirRoot_LocalMachine => Path.Combine(Unreal.RootDirectory.FullName, "GauntletTemp", "PerfCache");

		public PerformanceReport(UnrealTestContext InContext)
			: base(InContext)
		{
		}

		public override CitySampleTestConfig GetConfiguration()
		{
			CitySampleTestConfig Config = base.GetConfiguration();
			UnrealTestRole ClientRole = Config.RequireRole(UnrealTargetRole.Client);
			ClientRole.CommandLineParams.Add("csvGpuStats");
			ClientRole.CommandLineParams.Add("csvMetadata", "\"testname=CitySample\"");
			ClientRole.CommandLineParams.AddOrAppendParamValue("execcmds", "t.FPSChart.DoCSVProfile 1");
			ClientRole.CommandLineParams.Add("CitySampleTest.FPSChart");

			return Config;
		}

		protected override void InitHandledErrors()
        {
			base.InitHandledErrors();
		}

		/// <summary>
		/// Produces a detailed csv report using PerfReportTool.
		/// Also, stores perf data in the perf cache, and generates a historic report using the data the cache contains.
		/// </summary>
		private void GeneratePerfReport(UnrealTargetPlatform Platform, string ArtifactPath)
		{
			var ReportCacheDir = Automation.IsBuildMachine
				? ReportCacheDirRoot_BuildMachine
				: ReportCacheDirRoot_LocalMachine;

			var ToolPath = FileReference.Combine(Unreal.EngineDirectory, "Binaries", "DotNET", "CsvTools", "PerfreportTool.exe");
			if (!FileReference.Exists(ToolPath))
			{
				Log.TraceError($"Failed to find perf report utility at this path: \"{ToolPath}\".");
				return;
			}

			var ReportConfigDir = Path.Combine(Unreal.RootDirectory.FullName, "Samples", "Showcases", "CitySample", "Scripts", "PerfReport");
			var ReportPath = Path.Combine(ArtifactPath, "Reports", "Performance");

			// Csv files may have been output in one of two places.
			// Check both...
			var CsvsPaths = new[]
			{
				Path.Combine(ArtifactPath, "Client", "Profiling", "FPSChartStats"),
				Path.Combine(ArtifactPath, "Client", "Settings", "CitySample", "Saved", "Profiling", "FPSChartStats")
			};

			var DiscoveredCsvs = new List<string>();
			foreach (var CsvsPath in CsvsPaths)
			{
				if (Directory.Exists(CsvsPath))
				{
					DiscoveredCsvs.AddRange(
						from CsvFile in Directory.GetFiles(CsvsPath, "*.csv", SearchOption.AllDirectories)
						where CsvFile.Contains("csvprofile", StringComparison.InvariantCultureIgnoreCase)
						select CsvFile);
				}
			}

			if (DiscoveredCsvs.Count == 0)
			{
				Log.TraceError($"Test completed successfully but no csv profiling results were found. Searched paths were:\r\n  {string.Join("\r\n  ", CsvsPaths.Select(s => $"\"{s}\""))}");
				return;
			}

			// Find the newest csv file and get its directory
			// (PerfReportTool will only output cached data in -csvdir mode)
			var NewestFile =
				(from CsvFile in DiscoveredCsvs
				 let Timestamp = File.GetCreationTimeUtc(CsvFile)
				 orderby Timestamp descending
				 select CsvFile).First();
			var NewestDir = Path.GetDirectoryName(NewestFile);

			Log.TraceInformation($"Using perf report cache directory \"{ReportCacheDir}\".");
			Log.TraceInformation($"Using perf report output directory \"{ReportPath}\".");
			Log.TraceInformation($"Using csv results directory \"{NewestDir}\". Generating historic perf report data...");

			// Make sure the cache and output directories exist
			if (!Directory.Exists(ReportCacheDir))
			{
				try { Directory.CreateDirectory(ReportCacheDir); }
				catch (Exception Ex)
				{
					Log.TraceError($"Failed to create perf report cache directory \"{ReportCacheDir}\". {Ex}");
					return;
				}
			}
			if (!Directory.Exists(ReportPath))
			{
				try { Directory.CreateDirectory(ReportPath); }
				catch (Exception Ex)
				{
					Log.TraceError($"Failed to create perf report output directory \"{ReportPath}\". {Ex}");
					return;
				}
			}

			// Win64 is actually called "Windows" in csv profiles
			var PlatformNameFilter = Platform == UnrealTargetPlatform.Win64 ? "Windows" : $"{Platform}";

			// Produce the detailed report, and update the perf cache
			CommandUtils.RunAndLog(ToolPath.FullName, $"-csvdir \"{NewestDir}\" -o \"{ReportPath}\" -reportxmlbasedir \"{ReportConfigDir}\" -summaryTableCache \"{ReportCacheDir}\" -searchpattern csvprofile* -metadatafilter platform=\"{PlatformNameFilter}\"", out int ErrorCode);
			if (ErrorCode != 0)
			{
				Log.TraceError($"PerfReportTool returned error code \"{ErrorCode}\" while generating detailed report.");
			}

			// Now generate the all-time historic summary report
			HistoricReport("HistoricReport_AllTime", new[]
			{
				$"platform={PlatformNameFilter}"
			});

			// 14 days historic report
			HistoricReport($"HistoricReport_14Days", new[]
			{
				$"platform={PlatformNameFilter}",
				$"starttimestamp>={DateTimeOffset.Now.ToUnixTimeSeconds() - (14 * 60L * 60L * 24L)}"
			});

			// 7 days historic report
			HistoricReport($"HistoricReport_7Days", new[]
			{
				$"platform={PlatformNameFilter}",
				$"starttimestamp>={DateTimeOffset.Now.ToUnixTimeSeconds() - (7 * 60L * 60L * 24L)}"
			});

			void HistoricReport(string Name, IEnumerable<string> Filter)
			{
				var Args = new[]
				{
					$"-summarytablecachein \"{ReportCacheDir}\"",
					$"-summaryTableFilename \"{Name}.html\"",
					$"-reportxmlbasedir \"{ReportConfigDir}\"",
					$"-o \"{ReportPath}\"",
					$"-metadatafilter \"{string.Join(" and ", Filter)}\"",
					"-summaryTable autoPerfReportStandard",
					"-condensedSummaryTable autoPerfReportStandard",
					"-emailtable",
					"-recurse"
				};

				var ArgStr = string.Join(" ", Args);

				CommandUtils.RunAndLog(ToolPath.FullName, ArgStr, out ErrorCode);
				if (ErrorCode != 0)
				{
					Log.TraceError($"PerfReportTool returned error code \"{ErrorCode}\" while generating historic report.");
				}
			}
		}

		public override ITestReport CreateReport(TestResult Result, UnrealTestContext Context, UnrealBuildSource Build, IEnumerable<UnrealRoleResult> Artifacts, string ArtifactPath)
		{
			if (Result == TestResult.Passed)
			{
				Log.TraceInformation($"Generating performance reports using PerfReportTool.");
				GeneratePerfReport(Context.GetRoleContext(UnrealTargetRole.Client).Platform, ArtifactPath);
			}
			else
			{
				Log.TraceWarning($"Skipping performance report generation because the perf report test failed.");
			}

			return base.CreateReport(Result, Context, Build, Artifacts, ArtifactPath);
		}
	}

	//
	// Horrible hack to repeat the CitySample perf tests 3 times...
	// There is no way to pass "-repeat=N" to Gauntlet via the standard engine build scripts, nor is
	// it possible to override the number of iterations per-test via the GetConfiguration() function.
	//
	// In theory we can pass the "CitySampleTest.PerformanceReport" test name to Gauntlet 3 times via Horde scripts,
	// but the standard build scripts will attempt to define 3 nodes all with the same name, which won't work.
	//
	// These three classes allow us to run 3 copies of the PerformanceReport test, but ensures they all have 
	// different names to fit into the build script / Gauntlet structure.
	//

	public class PerformanceReport_1 : PerformanceReport
	{
		public PerformanceReport_1(UnrealTestContext InContext) : base(InContext) { }
	}

	public class PerformanceReport_2 : PerformanceReport
	{
		public PerformanceReport_2(UnrealTestContext InContext) : base(InContext) { }
	}

	public class PerformanceReport_3 : PerformanceReport
	{
		public PerformanceReport_3(UnrealTestContext InContext) : base(InContext) { }
	}
}
