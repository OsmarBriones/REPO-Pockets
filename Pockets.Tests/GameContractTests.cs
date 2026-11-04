using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Pockets.Tests;

/// <summary>
/// Tier 2: every game member the mod patches or reads still exists with the shape the code expects. The game's
/// assembly is read as plain metadata (nothing is loaded), so a game update that renames or removes something fails
/// here with a clear message instead of silently breaking the mod. Skipped when the game is not installed.
/// </summary>
[TestClass]
public class GameContractTests
{
	private static readonly Dictionary<string, TypeShape> Types = LoadTypes();

	private sealed class TypeShape
	{
		internal HashSet<string> Fields { get; } = new HashSet<string>();

		internal HashSet<string> Methods { get; } = new HashSet<string>();
	}

	// ---- Harmony targets (name/parameter count) ----

	[TestMethod]
	public void InventorySpot_HasTheMethodsWePatch()
	{
		RequireMethod("InventorySpot", "HandleInput", 0);
		RequireMethod("InventorySpot", "IsOccupied", 0);
		RequireMethod("InventorySpot", "UpdateUI", 0);
		RequireMethod("InventorySpot", "SetEmoji", 1);
	}

	[TestMethod]
	public void ValuableObject_HasTheMethodsWePatch()
	{
		RequireMethod("ValuableObject", "Start", 0);
		RequireMethod("ValuableObject", "AddToDollarHaulList", 0);
		RequireMethod("ValuableObject", "RemoveFromDollarHaulList", 0);
	}

	[TestMethod]
	public void PlayerAvatar_HasTheDeathRpcWePatch()
	{
		RequireMethod("PlayerAvatar", "PlayerDeathRPC", 2);
	}

	[TestMethod]
	public void EnemyDirector_HasStart()
	{
		RequireMethod("EnemyDirector", "Start", 0);
	}

	// ---- Members we read or call ----

	[TestMethod]
	public void InventorySpot_HasTheFieldsWeRead()
	{
		RequireField("InventorySpot", "inventorySpotIndex");
		RequireField("InventorySpot", "noItem");
		RequireField("InventorySpot", "inventoryIcon");
	}

	[TestMethod]
	public void ValuableObject_HasTheFieldsWeRead()
	{
		RequireField("ValuableObject", "volumeType");
		RequireField("ValuableObject", "dollarValueCurrent");
		RequireField("ValuableObject", "committedToExtraction");
	}

	[TestMethod]
	public void PlayerAvatar_HasTheFieldsWeRead()
	{
		RequireField("PlayerAvatar", "deadSet");
		RequireField("PlayerAvatar", "isDisabled");
		RequireField("PlayerAvatar", "physGrabber");
		RequireField("PlayerAvatar", "instance");
	}

	[TestMethod]
	public void PhysGrabber_HasTheMembersWeUse()
	{
		RequireField("PhysGrabber", "instance");
		RequireField("PhysGrabber", "grabbed");
		RequireField("PhysGrabber", "grabbedPhysGrabObject");
		RequireMethod("PhysGrabber", "OverrideGrabRelease", 2);
		RequireMethod("PhysGrabber", "OverrideGrab", 3);
	}

	[TestMethod]
	public void PhysGrabObject_HasTheMembersWeUse()
	{
		RequireField("PhysGrabObject", "playerGrabbing");
		RequireMethod("PhysGrabObject", "OverrideDeactivate", 1);
		RequireMethod("PhysGrabObject", "OverrideDeactivateReset", 0);
		RequireMethod("PhysGrabObject", "Teleport", 2);
	}

	[TestMethod]
	public void SemiFunc_HasTheHelpersWeUse()
	{
		RequireMethod("SemiFunc", "IsMultiplayer", 0);
		RequireMethod("SemiFunc", "IsMasterClient", 0);
		RequireMethod("SemiFunc", "IsMasterClientOrSingleplayer", 0);
		RequireMethod("SemiFunc", "RunIsLevel", 0);
		RequireMethod("SemiFunc", "PlayerAvatarGetFromPhotonID", 1);
		RequireMethod("SemiFunc", "MasterOnlyRPC", 1);
		RequireMethod("SemiFunc", "MasterAndOwnerOnlyRPC", 2);
		RequireMethod("SemiFunc", "InputUp", 1);
		RequireMethod("SemiFunc", "LayerMaskGetVisionObstruct", 0);
	}

	[TestMethod]
	public void InventoryAndPlayerController_HaveTheMembersWeUse()
	{
		RequireField("Inventory", "instance");
		RequireMethod("Inventory", "GetSpotByIndex", 1);
		RequireField("PlayerController", "instance");
		RequireField("PlayerController", "InputDisableTimer");
		RequireField("RoundDirector", "dollarHaulList");
	}

	// ---- Helpers ----

	private static void RequireMethod(string type, string method, int parameterCount)
	{
		TypeShape shape = RequireType(type);
		Assert.IsTrue(shape.Methods.Contains(method + "/" + parameterCount), $"{type}.{method}({parameterCount} parameters) was not found in the game assembly.");
	}

	private static void RequireField(string type, string field)
	{
		TypeShape shape = RequireType(type);
		Assert.IsTrue(shape.Fields.Contains(field), $"{type}.{field} was not found in the game assembly.");
	}

	private static TypeShape RequireType(string type)
	{
		if (Types.Count == 0)
		{
			Assert.Inconclusive("The game assembly was not found. Install the game or set REPO_GAME_DIR to run the contract tests.");
		}

		Assert.IsTrue(Types.TryGetValue(type, out TypeShape? shape), $"Type {type} was not found in the game assembly.");
		return shape!;
	}

	private static Dictionary<string, TypeShape> LoadTypes()
	{
		Dictionary<string, TypeShape> types = new Dictionary<string, TypeShape>();
		string root = Environment.GetEnvironmentVariable("REPO_GAME_DIR") ?? @"C:\Program Files (x86)\Steam\steamapps\common\REPO";
		string path = Path.Combine(root, "REPO_Data", "Managed", "Assembly-CSharp.dll");
		if (!File.Exists(path))
		{
			return types;
		}

		using FileStream stream = File.OpenRead(path);
		using PEReader peReader = new PEReader(stream);
		MetadataReader reader = peReader.GetMetadataReader();
		foreach (TypeDefinitionHandle handle in reader.TypeDefinitions)
		{
			TypeDefinition definition = reader.GetTypeDefinition(handle);
			if (!definition.GetDeclaringType().IsNil || reader.GetString(definition.Namespace).Length != 0)
			{
				continue;
			}

			TypeShape shape = new TypeShape();
			foreach (FieldDefinitionHandle field in definition.GetFields())
			{
				shape.Fields.Add(reader.GetString(reader.GetFieldDefinition(field).Name));
			}

			foreach (MethodDefinitionHandle method in definition.GetMethods())
			{
				MethodDefinition methodDefinition = reader.GetMethodDefinition(method);
				int parameters = methodDefinition.GetParameters().Count(parameter => reader.GetParameter(parameter).SequenceNumber > 0);
				shape.Methods.Add(reader.GetString(methodDefinition.Name) + "/" + parameters);
			}

			types[reader.GetString(definition.Name)] = shape;
		}

		return types;
	}
}
