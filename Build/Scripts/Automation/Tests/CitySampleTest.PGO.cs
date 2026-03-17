// Copyright Epic Games, Inc. All Rights Reserved.

using System;
using System.Collections.Generic;
using System.Text;
using Gauntlet;

namespace CitySampleTest
{
	public class PGO : PGONode<PGOConfig>
	{
		public PGO(UnrealTestContext InContext)
			: base(InContext)
		{}

		public override PGOConfig GetConfiguration()
		{
			var Config = base.GetConfiguration();

			var ClientRole = Config.RequireRole(UnrealTargetRole.Client);
			ClientRole.Controllers.Add("AutoTest");
			ClientRole.CommandLineParams.Add("-deterministic");
			ClientRole.CommandLineParams.Add("-novsync");

			// Max 6 hours
			Config.MaxDuration = 6 * 60 * 60;

			return Config;
		}
	}
}
