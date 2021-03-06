﻿using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using NuGet.Common;
using NuGet.Frameworks;

namespace NuGet.Build.Packaging.Tasks
{
	public class ApiIntersect : ToolTask
	{
		[Required]
		public ITaskItem Framework { get; set; }

		[Required]
		public ITaskItem[] IntersectionAssembly { get; set; }

		[Required]
		public ITaskItem RootOutputDirectory { get; set; }

		[Output]
		public ITaskItem OutputDirectory { get; set; }

		protected override string ToolName
		{
			get { return "ApiIntersect"; }
		}

		protected override string GenerateFullPathToTool()
		{
			return Path.Combine(ToolPath, ToolExe);
		}

		protected override MessageImportance StandardErrorLoggingImportance
		{
			get { return MessageImportance.High; }
		}

		protected override string GenerateCommandLineCommands()
		{
			var builder = new CommandLineBuilder();

			var nugetFramework = NuGetFramework.Parse(Framework.ItemSpec);

			builder.AppendSwitch("-o");
			CreateOutputDirectoryItem(nugetFramework);
			builder.AppendFileNameIfNotNull(OutputDirectory.ItemSpec);

			string frameworkPath = GetFrameworkPath(nugetFramework);
			builder.AppendSwitch("-f");
			builder.AppendFileNameIfNotNull(frameworkPath);

			foreach (var assembly in IntersectionAssembly.NullAsEmpty())
			{
				builder.AppendSwitch("-i");
				builder.AppendFileNameIfNotNull(assembly.ItemSpec);
			}

			return builder.ToString();
		}

		void CreateOutputDirectoryItem(NuGetFramework framework)
		{
			string fullOutputPath = GetApiIntersectTargetPaths.GetOutputDirectory(RootOutputDirectory.ItemSpec, framework);
			OutputDirectory = new TaskItem(fullOutputPath + Path.DirectorySeparatorChar);
			OutputDirectory.SetMetadata("TargetFrameworkVersion", "v" + framework.Version.ToString(2));
			OutputDirectory.SetMetadata("TargetFrameworkProfile", framework.Profile);
			OutputDirectory.SetMetadata(MetadataName.TargetFrameworkMoniker, Framework.ItemSpec);
		}

		string GetFrameworkPath(NuGetFramework framework)
		{
			if (framework.IsPCL)
			{
				return GetPortableLibraryPath(framework);
			}

			throw new NotImplementedException("Only PCL profiles supported.");
		}

		string GetPortableLibraryPath(NuGetFramework framework)
		{
			return Path.Combine(
				GetPortableRootDirectory(),
				"v" + framework.Version.ToString(2),
				"Profile",
				framework.Profile);
		}

		static string GetPortableRootDirectory()
		{
			if (RuntimeEnvironmentHelper.IsMono)
			{
				string macPath = "/Library/Frameworks/Mono.framework/Versions/Current/lib/mono/xbuild-frameworks/.NETPortable/";
				if (Directory.Exists(macPath))
					return macPath;

				return Path.Combine(Path.GetDirectoryName(typeof(Object).Assembly.Location), "../xbuild-frameworks/.NETPortable/");
			}

			return Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86, Environment.SpecialFolderOption.DoNotVerify),
				@"Reference Assemblies\Microsoft\Framework\.NETPortable");
		}
	}
}