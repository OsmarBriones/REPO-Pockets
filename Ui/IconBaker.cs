using System.Collections;
using System.IO;
using BepInEx;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Pockets.Ui;

/// <summary>
/// Developer-only tool (Debug.EnableIconBake). Press F10 inside a level to render a picture of every valuable
/// prefab in the game with the same studio lighting and save them as PNG files in BepInEx/PocketIcons.
/// The PNGs are then copied into the repository's Assets/Icons folder and packaged inside the mod.
/// </summary>
internal sealed class IconBaker : MonoBehaviour
{
	private static readonly string[] Folders =
	{
		"valuables/01 tiny", "valuables/02 small", "valuables/03 medium", "valuables/04 big",
		"valuables/05 wide", "valuables/06 tall", "valuables/07 very tall",
	};

	private bool running;

	private void Update()
	{
		Keyboard? keyboard = Keyboard.current;
		if (keyboard != null && keyboard.f10Key.wasPressedThisFrame && !running)
		{
			StartCoroutine(Bake());
		}
	}

	private IEnumerator Bake()
	{
		running = true;
		string outputFolder = Path.Combine(Paths.BepInExRootPath, "PocketIcons");
		Directory.CreateDirectory(outputFolder);
		PocketsPlugin.Logger.LogInfo($"[Icon bake] started, writing to {outputFolder}");

		int saved = 0;
		int failed = 0;
		foreach (string folder in Folders)
		{
			GameObject[] prefabs = Resources.LoadAll<GameObject>(folder);
			PocketsPlugin.Logger.LogInfo($"[Icon bake] {folder}: {prefabs.Length} prefabs");
			foreach (GameObject prefab in prefabs)
			{
				string key = ValuableIconRenderer.KeyOf(prefab);
				Texture2D? texture = null;
				try
				{
					texture = ValuableIconRenderer.RenderTexture2D(prefab);
					if (texture != null)
					{
						File.WriteAllBytes(Path.Combine(outputFolder, key + ".png"), texture.EncodeToPNG());
						saved++;
					}
					else
					{
						failed++;
						PocketsPlugin.Logger.LogWarning($"[Icon bake] nothing to draw for {key}");
					}
				}
				catch (System.Exception exception)
				{
					failed++;
					PocketsPlugin.Logger.LogWarning($"[Icon bake] {key} failed: {exception.Message}");
				}
				finally
				{
					if (texture != null)
					{
						Destroy(texture);
					}
				}

				yield return null;
			}
		}

		PocketsPlugin.Logger.LogInfo($"[Icon bake] done: {saved} saved, {failed} failed");
		running = false;
	}
}
