// Copyright Epic Games, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using AutomationTool;

using EpicGames.Core;

namespace CitySample.Automation
{
	public class CitySample_CheckAssetReferences : BuildCommand
	{
		public override void ExecuteBuild()
		{
			try
			{
				ExecuteBuildInner();
			}
			finally
			{
				//CommandUtils.DeleteFile_NoExceptions(FileReference.Combine(CommandUtils.RootDirectory, "Path/FileToDelete").FullName);
			}
		}

		public void ExecuteBuildInner()
		{
			int ThisCLInt = 0;
			int PrevCL = 0;
			bool CheckOpenedFiles = ParseParam("opened");
			bool CheckShelvedFiles = ParseParam("shelved");
			string CommandletArgs = "";
			bool RunCommandlet = false;
			string PrevCLFilePath = "";
			bool SkipPrevCLFileExport = ParseParam("SkipPrevCLFileExport");

			string ExtensionTypeListParam = ParseParamValue("ExtTypeList", ".uasset,.umap,.cpp,.c,.h,.inl,.ini,.uproject,.uplugin,.json");
			List<string> ExtensionTypeList = new List<string>();
			if (string.IsNullOrEmpty(ExtensionTypeListParam))
			{
				CommandUtils.LogInformation("No extensions were passed in, defaulting to always run.  Set -ExtTypeList to the extension typelist for triggering the commandlet");
				RunCommandlet = true;
			}
			else
			{
				ExtensionTypeList = ExtensionTypeListParam.Split(',').ToList();
			}

			// if not checking open files, use CL ranges
			if (CheckOpenedFiles)
			{
				// Quickly check if the CL has any files we're interested in. This is significantly faster than firing up the editor to do
				// nothing
				RunCommandlet = AreFileTypesModifiedInOpenChangelists(ExtensionTypeList);

				// save anyone who forgot to run this without -p4
				if (!P4Enabled)
				{
					throw new AutomationException("This script must be executed with -p4");
				}
				CommandletArgs = "-P4Opened -P4Client=" + P4Env.Client;
			}
			else
			{
				string ThisCL = ParseParamValue("CL");
				if (string.IsNullOrEmpty(ThisCL))
				{
					throw new AutomationException("-CL=<num> or -opened must be specified.");
				}

				if (!int.TryParse(ThisCL, out ThisCLInt))
				{
					throw new AutomationException("-CL must be a number.");
				}

				if (CheckShelvedFiles)
				{
					// Quickly check if the CL has any files we're interested in. This is significantly faster than firing up the editor to do
					// nothing
					RunCommandlet = AreFileTypesModifiedInShelvedChangelist(ThisCL, ExtensionTypeList);
					// filter is what's passed to p4 files. If shelved we'll use the syntax that pulls the shelved file list
					CommandletArgs = String.Format("-P4Filter=@={0}", ThisCL);
				}
				else
				{
					string Branch = ParseParamValue("Branch");
					if (string.IsNullOrEmpty(Branch))
					{
						throw new AutomationException("-Branch must be specified to check a CL range when -opened or -shelved are not present");
					}

					string LastGoodContentCLPath = ParseParamValue("LastGoodContentCLPath");
					if (string.IsNullOrEmpty(LastGoodContentCLPath))
					{
						// Default to local storage for this file (Legacy behavior)
						LastGoodContentCLPath = CombinePaths(CmdEnv.LocalRoot, "Engine", "Saved");
					}

					string PrevCLFileName = "PrevCL_" + Branch.Replace("/", "+") + ".txt";
					PrevCLFilePath = CombinePaths(LastGoodContentCLPath, PrevCLFileName);
					PrevCL = ReadPrevCLFile(PrevCLFilePath);

					if (PrevCL <= 0)
					{
						CommandUtils.LogInformation("Previous CL file didn't exist. Defaulting to none!");
						RunCommandlet = true;
					}
					else if (PrevCL >= ThisCLInt)
					{
						CommandUtils.LogInformation("Previous CL file shows a CL equal or newer than the current CL. This content was already checked. Skipping.");
						RunCommandlet = false;
					}
					else
					{
						// +1 to the previous cl so it won't use content from the previous change
						PrevCL++;
						CommandletArgs = String.Format("-P4Filter={0}/Samples/Showcases/CitySample/...@{1},{2}", Branch, PrevCL, ThisCL);
						CommandUtils.LogInformation("Generated Filter: {0}", CommandletArgs);

						RunCommandlet = WereFileTypesModifiedInChangelistRange(Branch, PrevCL, ThisCL, ExtensionTypeList);
					}

					if (!RunCommandlet)
					{
						CommandUtils.LogInformation("No files in CL Range {0} -> {1} contained any files ending with extensions {2}, or they were already checked in a previous job, skipping commandlet run", PrevCL, ThisCL, ExtensionTypeListParam);
					}
				}
			}

			if (RunCommandlet)
			{
				string EditorExe = "UnrealEditor-Cmd.exe";
				EditorExe = AutomationTool.HostPlatform.Current.GetUnrealExePath(EditorExe);

				CommandletArgs += " -TargetCitySampleReleaseVersion=MaxVersion -ini:Engine:[Core.System]:AssetLogShowsDiskPath=True  -LogCmds=\"LogHttp Error\"";

				string MaxPackagesToLoad = ParseParamValue("MaxPackagesToLoad", "2000");
				CommandletArgs += String.Format(" -MaxPackagesToLoad={0}", MaxPackagesToLoad);

				string ExcludedDirectoriesArg = ParseParamValue("ExcludedDirectories");
				if (!string.IsNullOrEmpty(ExcludedDirectoriesArg))
				{
					CommandletArgs += " -ExcludedDirectories=" + ExcludedDirectoriesArg;
				}

				CommandUtils.RunCommandlet(new FileReference(CombinePaths(CmdEnv.LocalRoot, "Samples/Showcases/CitySample", "CitySample.uproject")), EditorExe, "CitySampleContentValidation", CommandletArgs);
			}

			// Read the previous CL file one more time before writing to it, in case it changed while we were running
			if (ThisCLInt > 0)
			{
				if (PrevCL < ThisCLInt)
				{
					PrevCL = ReadPrevCLFile(PrevCLFilePath);
				}

				if (PrevCL < ThisCLInt && !SkipPrevCLFileExport)
				{
					CommandUtils.LogInformation("Writing PrevCLFile {0}...", PrevCLFilePath);
					WritePrevCLFile(PrevCLFilePath, ThisCLInt.ToString());
				}
				else
				{
					CommandUtils.LogInformation("Not writing PrevCLFile {0}. The current CL was not newer or -SkipPrevCLFileExport was specified", PrevCLFilePath);
				}
			}
		}

		private int ReadPrevCLFile(string PrevCLFilePath)
		{
			int RetVal = 0;
			if (File.Exists(PrevCLFilePath))
			{
				string PrevCLString = "";
				int RetryCount = 10;
				bool bProceed = false;
				do
				{
					try
					{
						PrevCLString = File.ReadAllText(PrevCLFilePath);
						bProceed = true;
					}
					catch (Exception Ex)
					{
						if (RetryCount > 0)
						{
							CommandUtils.LogInformation("Failed to read PrevCLFilePath {0}. Retrying in a few seconds. Ex:{1}", PrevCLFilePath, Ex.Message);
							RetryCount--;
							Thread.Sleep(TimeSpan.FromSeconds(5));
						}
						else
						{
							CommandUtils.LogError("Failed to read PrevCLFilePath {0}. All Retries exhausted, skipping. Ex:{1}", PrevCLFilePath, Ex.Message);
							bProceed = true;
						}
					}
				} while (!bProceed);

				if (int.TryParse(PrevCLString, out RetVal))
				{
					// Read the file successfully, and it was a number
				}
				else
				{
					CommandUtils.LogWarning("Couldn't parse out the changelist number from the saved PrevCLFilePath file. " + PrevCLFilePath);
				}
			}

			return RetVal;
		}

		private void WritePrevCLFile(string PrevCLFilePath, string ThisCL)
		{
			int RetryCount = 10;
			bool bProceed = false;
			do
			{
				try
				{
					Directory.CreateDirectory(Path.GetDirectoryName(PrevCLFilePath));
					File.WriteAllText(PrevCLFilePath, ThisCL);
					bProceed = true;
				}
				catch (Exception Ex)
				{
					if (RetryCount > 0)
					{
						CommandUtils.LogInformation("Failed to write PrevCLFilePath {0}. Retrying in a few seconds. Ex:{1}", PrevCLFilePath, Ex.Message);
						RetryCount--;
						Thread.Sleep(TimeSpan.FromSeconds(5));
					}
					else
					{
						CommandUtils.LogError("Failed to write PrevCLFilePath {0}. All Retries exhausted, skipping. Ex:{1}", PrevCLFilePath, Ex.Message);
						bProceed = true;
					}
				}
			} while (!bProceed);
		}

		/// <summary>
		/// Returns true if files with extensions in the provided list were modified in the specified changelist range
		/// </summary>
		/// <param name="Branch"></param>
		/// <param name="PrevCL"></param>
		/// <param name="ThisCL"></param>
		/// <param name="ExtensionTypeList"></param>
		/// <returns></returns>
		private bool WereFileTypesModifiedInChangelistRange(string Branch, int PrevCL, string ThisCL, List<string> ExtensionTypeList)
		{
			// we don't need to do any of this if there was no extensions typelist passed in
			if (ExtensionTypeList.Count != 0)
			{
				// check all the changes in FN from PrevCL to now
				List<P4Connection.ChangeRecord> ChangeRecords;
				CommandUtils.P4.Changes(out ChangeRecords, string.Format("{0}/...@{1},{2}", Branch, PrevCL, ThisCL), false);
				foreach (P4Connection.ChangeRecord Record in ChangeRecords)
				{
					P4Connection.DescribeRecord DescribeRecord;
					CommandUtils.P4.DescribeChangelist(Record.CL, out DescribeRecord, false);
					// check all the files in each cl record
					foreach (P4Connection.DescribeRecord.DescribeFile File in DescribeRecord.Files)
					{
						// if any of them end in extensions in our typelist, we need to build
						foreach (string Extension in ExtensionTypeList)
						{
							if (File.File.EndsWith(Extension))
							{
								return true;
							}
						}
					}
				}
			}

			return false;
		}

		/// <summary>
		/// Returns true if files with extensions in the provided list are in the specified shelved changelist
		/// </summary>
		/// <param name="ShelvedChangelist"></param>
		/// <param name="ExtensionTypeList"></param>
		/// <returns></returns>
		private bool AreFileTypesModifiedInShelvedChangelist(string ShelvedChangelist, List<string> ExtensionTypeList)
		{
			// we don't need to do any of this if there was no extensions typelist passed in
			if (ExtensionTypeList.Count != 0)
			{
				// Get all files in this changelist
				IEnumerable<string> FileList = CommandUtils.P4.Files("@=" + ShelvedChangelist);

				return FileList.Any(F => ExtensionTypeList.Contains(Path.GetExtension(F), StringComparer.OrdinalIgnoreCase));
			}

			return false;
		}

		/// <summary>
		/// Returns true if files with extensions in the provided list are  open locally
		/// </summary>
		/// <param name="ExtensionTypeList"></param>
		/// <returns></returns>
		private bool AreFileTypesModifiedInOpenChangelists(List<string> ExtensionTypeList)
		{
			// we don't need to do any of this if there was no extensions typelist passed in
			if (ExtensionTypeList.Count != 0)
			{
				// Get all checked out files in the current workspace
				IEnumerable<string> FileList = CommandUtils.P4.Opened("");

				return FileList.Any(F => ExtensionTypeList.Contains(Path.GetExtension(F), StringComparer.OrdinalIgnoreCase));
			}

			return false;
		}
	}

	[Help("Updates the Audit_InCook collections")]
	[RequireP4]
	class CitySample_UpdateInCookCollection : BuildCommand
	{
		public override void ExecuteBuild()
		{
			LogInformation("************************* UpdateInCookCollection");

			bool SkipCheckin = ParseParam("skipcheckin");
			bool WithNotInCookCollection = ParseParam("withnotincook");

			// Now update what is in the InCook audit collection
			string CollectionName = "Audit_InCook";
			string NotInCookCollectionName = "Audit_NotInCook";

			// Attempt to find the UFS file list. Default to the log folder
			string DefaultUfsFilename = CommandUtils.CombinePaths(CommandUtils.CmdEnv.LogFolder, "..", "Manifest_UFSFiles_Win64.txt");
			string FallbackUfsFilename = CommandUtils.CombinePaths(CommandUtils.CmdEnv.LogFolder, "..", "..", "Manifest_UFSFiles_Win64.txt");
			string UseUfsFilename = ParseParamValue("ufsmanifest", DefaultUfsFilename);
			if (!CommandUtils.FileExists_NoExceptions(UseUfsFilename))
			{
				LogInformation("UFS Path {0} did not exist, using {1} instead.", UseUfsFilename, DefaultUfsFilename);
				UseUfsFilename = FallbackUfsFilename;
			}

			UpdateInCookAuditCollection(CollectionName, NotInCookCollectionName, UseUfsFilename, SkipCheckin, WithNotInCookCollection);
		}

		protected List<string> GetProjectRelativePathParts(DirectoryReference RootDirectory, DirectoryReference ProjectRootDirectory)
		{
			List<string> PathParts = new List<string>();

#nullable enable
			DirectoryReference? TempDirectory = ProjectRootDirectory;
			while (TempDirectory != null && TempDirectory != RootDirectory)
			{
				PathParts.Add(TempDirectory.GetDirectoryName());
				TempDirectory = TempDirectory.ParentDirectory;
			}
#nullable disable

			PathParts.Reverse();
			return PathParts;
		}

		protected void AddFileToP4(int WorkingCl, string LocalFileName, string[] Paths)
		{
			var CollectionFilenameP4 = CommandUtils.CombinePaths(PathSeparator.Slash, Paths);
			if (!CommandUtils.FileExists_NoExceptions(LocalFileName))
			{
				CommandUtils.P4.Add(WorkingCl, CollectionFilenameP4);
			}
			else
			{
				CommandUtils.P4.Edit(WorkingCl, CollectionFilenameP4);
			}
		}

		protected void WritePathToStream(string ObjectPath, StreamWriter Writer)
		{
			string ObjectName = Path.GetFileNameWithoutExtension(ObjectPath);
			ObjectPath = Path.GetDirectoryName(ObjectPath) + "/" + ObjectName + "." + ObjectName;

			ObjectPath = ObjectPath.Replace('\\', '/');

			Writer.WriteLine(ObjectPath);
		}

		public void UpdateInCookAuditCollection(string CollectionName, string NotInCookCollectionName, string UfsFilename, bool SkipCheckin, bool WithNotInCookCollection)
		{
			if (!CommandUtils.FileExists_NoExceptions(UfsFilename))
			{
				LogWarning("Could not update audit collection, missing file: " + UfsFilename);
				return;
			}

			FileReference Project = ParseProjectParam();

			string ProjectName = Project.GetFileNameWithoutExtension();
			string AssetExtension = ".uasset";
			string MapExtension = ".umap";
			string GameFolderName = ProjectName + "/Content";
			string EngineFolderName = "Engine/Content";
			string GamePluginFolderName = ProjectName + "/Plugins/";
			string EnginePluginFolderName = "Engine/Plugins/";
			string CollectionNameWithPath = CollectionName + ".collection";
			string NotInCookCollectionNameWithPath = NotInCookCollectionName + ".collection";

			DirectoryReference LocalRootDirectory = new DirectoryReference(CommandUtils.CmdEnv.LocalRoot);
			DirectoryReference ProjectRootDirectory = Project.Directory;
			List<string> PathParts = GetProjectRelativePathParts(LocalRootDirectory, ProjectRootDirectory);

			PathParts.Add("Content");
			PathParts.Add("Collections");

			int WorkingCL = -1;
			if (CommandUtils.P4Enabled)
			{
				WorkingCL = CommandUtils.P4.CreateChange(CommandUtils.P4Env.Client, String.Format("Updated " + CollectionName + " collection using CL {0}", P4Env.Changelist));
			}

			var CollectionFilenameLocal = CommandUtils.CombinePaths(PathParts.Append(CollectionNameWithPath).ToArray());
			var NotInCookCollectionFilenameLocal = CommandUtils.CombinePaths(PathParts.Append(NotInCookCollectionNameWithPath).ToArray());

			if (WorkingCL > 0)
			{
				List<string> P4PathParts = PathParts.Prepend(CommandUtils.P4Env.Branch).ToList();
				AddFileToP4(WorkingCL, CollectionFilenameLocal, P4PathParts.Append(CollectionNameWithPath).ToArray());

				if (WithNotInCookCollection)
				{
					AddFileToP4(WorkingCL, NotInCookCollectionFilenameLocal, P4PathParts.Append(NotInCookCollectionNameWithPath).ToArray());
				}
			}

			StreamReader ManifestFile = null;
			StreamWriter CollectionFile = null;
			StreamWriter NotInCookCollectionFile = null;

			int TotalAssetCount = 0;
			int TotalUsedAssetCount = 0;

			HashSet<string> UnusedGameAssets = new HashSet<string>();
			if (WithNotInCookCollection)
			{
				CommandUtils.LogInformation("Discovering all game assets. This could take a while...");
				DirectoryReference ContentDir = DirectoryReference.Combine(ProjectRootDirectory, "Content");
				foreach (FileReference AssetFile in CommandUtils.FindFiles("*.*", true, ContentDir))
				{
					bool IsAsset = AssetFile.GetExtension() == AssetExtension;
					bool IsMap = !IsAsset && AssetFile.GetExtension() == MapExtension;
					if (!(IsAsset || IsMap))
					{
						continue;
					}

					string Asset = "/Game/" + AssetFile.FullName.Substring(ContentDir.FullName.Length + 1);
					UnusedGameAssets.Add(Asset.Replace("\\", "/"));
				}

				CommandUtils.LogInformation("Finished discovering all game assets! {0}", UnusedGameAssets.Count.ToString());
				TotalAssetCount = UnusedGameAssets.Count;

				CommandUtils.LogInformation("Discovering all game plugin assets. This could take a while...");
				DirectoryReference PluginDir = DirectoryReference.Combine(ProjectRootDirectory, "Plugins");
				foreach (FileReference PluginFile in CommandUtils.FindFiles("*.uplugin", true, PluginDir))
				{
					DirectoryReference PluginContentDir = DirectoryReference.Combine(PluginFile.Directory, "Content");
					string PluginName = PluginFile.GetFileNameWithoutExtension();
					foreach (FileReference PluginAssetFile in CommandUtils.FindFiles("*.*", true, PluginContentDir))
					{
						bool IsAsset = PluginAssetFile.GetExtension() == AssetExtension;
						bool IsMap = !IsAsset && PluginAssetFile.GetExtension() == MapExtension;
						if (!(IsAsset || IsMap))
						{
							continue;
						}

						string Asset = "/" + PluginName + "/" + PluginAssetFile.FullName.Substring(PluginContentDir.FullName.Length + 1);
						UnusedGameAssets.Add(Asset.Replace("\\", "/"));
					}
				}
				CommandUtils.LogInformation("Finished discovering all game plugin assets! {0}", (UnusedGameAssets.Count - TotalAssetCount).ToString());
				TotalAssetCount = UnusedGameAssets.Count;
			}

			try
			{
				CollectionFile = new StreamWriter(CollectionFilenameLocal);
				CollectionFile.WriteLine("FileVersion:1");
				CollectionFile.WriteLine("Type:Static");
				CollectionFile.WriteLine("");

				string Line = "";
				ManifestFile = new StreamReader(UfsFilename);
				while ((Line = ManifestFile.ReadLine()) != null)
				{
					string[] Tokens = Line.Split('\t');
					if (Tokens.Length > 1)
					{
						string UFSPath = Tokens[0];
						UFSPath = UFSPath.Trim('\"');
						bool bIsAsset = UFSPath.EndsWith(AssetExtension, StringComparison.InvariantCultureIgnoreCase);
						bool bIsMap = !bIsAsset && UFSPath.EndsWith(MapExtension, StringComparison.InvariantCultureIgnoreCase);
						if (bIsAsset || bIsMap)
						{
							bool bIsGame = UFSPath.StartsWith(GameFolderName);
							bool bIsEngine = UFSPath.StartsWith(EngineFolderName);
							bool bIsGamePlugin = UFSPath.StartsWith(GamePluginFolderName);
							bool bIsEnginePlugin = UFSPath.StartsWith(EnginePluginFolderName);
							if (bIsGame || bIsEngine || bIsGamePlugin || bIsEnginePlugin)
							{
								string ObjectPath = UFSPath;

								bool bValidPath = true;
								if (bIsGame)
								{
									string PathFromContentDir = ObjectPath.Substring(GameFolderName.Length + 1);
									ObjectPath = "/Game/" + PathFromContentDir;
									if (WithNotInCookCollection)
									{
										if (UnusedGameAssets.Remove(ObjectPath))
										{
											++TotalUsedAssetCount;
										}
									}
								}
								else if (bIsEngine)
								{
									ObjectPath = "/Engine/" + ObjectPath.Substring(EngineFolderName.Length + 1);
								}
								else if (bIsGamePlugin || bIsEnginePlugin)
								{
									int ContentIdx = ObjectPath.IndexOf("/Content/");
									if (ContentIdx != -1)
									{
										int PluginIdx = ObjectPath.LastIndexOf("/", ContentIdx - 1);
										if (PluginIdx == -1)
										{
											PluginIdx = 0;
										}
										else
										{
											// Skip the leading "/"
											PluginIdx++;
										}

										DirectoryReference PluginRoot = new DirectoryReference(ObjectPath.Substring(0, ContentIdx));
										string PluginName = "";
										foreach (FileReference PluginFile in CommandUtils.FindFiles("*.uplugin", false, PluginRoot))
										{
											PluginName = PluginFile.GetFileNameWithoutAnyExtensions();
											break;
										}
										if (string.IsNullOrEmpty(PluginName))
										{
											// Fallback to the directory name if the uplugin file doesnt exist
											PluginName = ObjectPath.Substring(PluginIdx, ContentIdx - PluginIdx);
										}
										if (PluginName.Length > 0)
										{
											int PathStartIdx = ContentIdx + "/Content/".Length;
											string PathFromContentDir = ObjectPath.Substring(PathStartIdx);
											ObjectPath = "/" + PluginName + "/" + PathFromContentDir;

											if (WithNotInCookCollection && bIsGamePlugin)
											{
												if (UnusedGameAssets.Remove(ObjectPath))
												{
													++TotalUsedAssetCount;
												}
											}
										}
										else
										{
											LogWarning("Could not add asset to collection. No plugin name. Path:" + UFSPath);
											bValidPath = false;
										}
									}
									else
									{
										LogWarning("Could not add asset to collection. No content folder. Path:" + UFSPath);
										bValidPath = false;
									}
								}

								if (bValidPath)
								{
									WritePathToStream(ObjectPath, CollectionFile);
								}
							}
						}
					}
				}

				if (WithNotInCookCollection)
				{
					NotInCookCollectionFile = new StreamWriter(NotInCookCollectionFilenameLocal);
					NotInCookCollectionFile.WriteLine("FileVersion:1");
					NotInCookCollectionFile.WriteLine("Type:Static");
					NotInCookCollectionFile.WriteLine("");

					foreach (string ObjectPath in UnusedGameAssets)
					{
						WritePathToStream(ObjectPath, NotInCookCollectionFile);
					}
				}
			}
			catch (Exception Ex)
			{
				CommandUtils.LogInformation("Did not update InCook collection. {0}", Ex.Message);
			}
			finally
			{
				if (ManifestFile != null)
				{
					ManifestFile.Close();
				}

				if (CollectionFile != null)
				{
					CollectionFile.Close();
				}

				if (NotInCookCollectionFile != null)
				{
					NotInCookCollectionFile.Close();
				}

				CommandUtils.LogInformation("Total Used Assets = {0}, Total Assets = {1}", TotalUsedAssetCount.ToString(), TotalAssetCount.ToString());
			}

			if (WorkingCL > 0 && !SkipCheckin)
			{
				// Check in the collection
				int SubmittedCL;
				CommandUtils.P4.Submit(WorkingCL, out SubmittedCL, true, true);
			}

		}
	}
}