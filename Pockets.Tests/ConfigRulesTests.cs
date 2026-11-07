using System;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pockets.Tests;

/// <summary>
/// Configuration types rule (see REPO_MODS_WORKSPACE.md): every numeric setting declares the values it accepts. The check
/// itself lives in RepoKit (<c>scripts/check-config-types.ps1</c>) so all mods share one implementation; this test runs
/// it on the mod's sources. If it fails, add an <c>AcceptableValueRange</c> (or justify with a <c>// config-range-ok:</c>
/// comment); do not delete the test.
/// </summary>
[TestClass]
public class ConfigRulesTests
{
	[TestMethod]
	public void NumericSettings_DeclareValueRanges()
	{
		string root = FindModRoot();
		string script = Path.Combine(root, "external", "RepoKit", "scripts", "check-config-types.ps1");
		if (!File.Exists(script))
		{
			Assert.Inconclusive("external/RepoKit has no scripts/check-config-types.ps1: update the RepoKit submodule.");
		}

		ProcessStartInfo start = new ProcessStartInfo("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -Path \"{root}\"")
		{
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		using Process process = Process.Start(start)!;
		string output = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
		process.WaitForExit();
		Assert.AreEqual(0, process.ExitCode, "Numeric settings without a value range:\n" + output);
	}

	private static string FindModRoot()
	{
		DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
		while (directory != null && !File.Exists(Path.Combine(directory.FullName, "manifest.json")))
		{
			directory = directory.Parent;
		}

		return directory?.FullName ?? throw new InvalidOperationException("manifest.json was not found above the test assembly.");
	}
}
