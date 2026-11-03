using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Pockets.Ui;

/// <summary>
/// Makes the small picture shown on an inventory slot for a pocketed valuable. Valuables have no prebaked
/// icon (items do), so the picture is rendered once per valuable type from its meshes with a throwaway camera
/// placed far from the level, and cached. If anything goes wrong a generic gem is used instead.
/// </summary>
internal static class ValuableIconRenderer
{
	private const int Size = 128;
	private const float FieldOfView = 30f;
	private static readonly Vector3 FarOrigin = new Vector3(0f, 5000f, 0f);
	private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
	private static Sprite? fallback;

	/// <summary>The generic icon used when a picture cannot be rendered.</summary>
	internal static Sprite Fallback
	{
		get
		{
			if (fallback == null)
			{
				fallback = CreateFallback();
			}

			return fallback;
		}
	}

	/// <summary>Renders and caches the icon of this valuable type if it is not cached yet. Call it before the valuable is hidden.</summary>
	internal static void Ensure(GameObject valuable)
	{
		string key = KeyOf(valuable);
		if (Cache.TryGetValue(key, out Sprite? cached) && cached != null)
		{
			return;
		}

		Sprite? sprite = null;
		try
		{
			sprite = Render(valuable);
		}
		catch (Exception exception)
		{
			PocketsPlugin.Logger.LogWarning($"Could not render an icon for {key}; using the generic one. {exception.Message}");
		}

		if (sprite == null)
		{
			PocketsPlugin.Logger.LogInfo($"Using the generic icon for {key}");
		}

		Cache[key] = sprite != null ? sprite : Fallback;
	}

	/// <summary>The cached icon for this valuable type, or the generic one.</summary>
	internal static Sprite Get(GameObject valuable)
	{
		return Cache.TryGetValue(KeyOf(valuable), out Sprite? sprite) && sprite != null ? sprite : Fallback;
	}

	private static string KeyOf(GameObject valuable)
	{
		return valuable.name.Replace("(Clone)", string.Empty).Trim();
	}

	private static Sprite? Render(GameObject valuable)
	{
		List<MeshRenderer> renderers = new List<MeshRenderer>();
		Bounds bounds = default;
		bool hasBounds = false;
		foreach (MeshRenderer renderer in valuable.GetComponentsInChildren<MeshRenderer>())
		{
			if (!renderer.enabled || !renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
			{
				continue;
			}

			renderers.Add(renderer);
			if (hasBounds)
			{
				bounds.Encapsulate(renderer.bounds);
			}
			else
			{
				bounds = renderer.bounds;
				hasBounds = true;
			}
		}

		if (!hasBounds)
		{
			return null;
		}

		float radius = Mathf.Max(bounds.extents.magnitude, 0.01f);
		float distance = radius / Mathf.Sin(FieldOfView * 0.5f * Mathf.Deg2Rad) * 1.1f;
		Vector3 direction = new Vector3(0.6f, 0.45f, -1f).normalized;
		Vector3 cameraPosition = FarOrigin + direction * distance;

		RenderTexture target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
		GameObject cameraObject = new GameObject("PocketsIconCamera") { hideFlags = HideFlags.HideAndDontSave };
		GameObject lightObject = new GameObject("PocketsIconLight") { hideFlags = HideFlags.HideAndDontSave };
		RenderTexture previous = RenderTexture.active;
		try
		{
			Camera camera = cameraObject.AddComponent<Camera>();
			camera.enabled = false;
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
			camera.cullingMask = 1;
			camera.nearClipPlane = 0.01f;
			camera.farClipPlane = distance + radius * 4f;
			camera.fieldOfView = FieldOfView;
			camera.allowHDR = false;
			camera.allowMSAA = false;
			camera.targetTexture = target;
			camera.transform.position = cameraPosition;
			camera.transform.LookAt(FarOrigin);

			Light light = lightObject.AddComponent<Light>();
			light.type = LightType.Directional;
			light.intensity = 1.1f;
			light.shadows = LightShadows.None;
			light.cullingMask = 1;
			light.transform.rotation = Quaternion.LookRotation((FarOrigin - (cameraPosition + Vector3.up * distance * 0.5f)).normalized);

			foreach (MeshRenderer renderer in renderers)
			{
				Mesh mesh = renderer.GetComponent<MeshFilter>().sharedMesh;
				Material[] materials = renderer.sharedMaterials;
				if (materials.Length == 0)
				{
					continue;
				}

				Matrix4x4 matrix = Matrix4x4.Translate(FarOrigin - bounds.center) * renderer.localToWorldMatrix;
				for (int sub = 0; sub < mesh.subMeshCount; sub++)
				{
					Graphics.DrawMesh(mesh, matrix, materials[Mathf.Min(sub, materials.Length - 1)], 0, camera, sub);
				}
			}

			camera.Render();

			RenderTexture.active = target;
			Texture2D texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
			texture.ReadPixels(new Rect(0f, 0f, Size, Size), 0, 0);
			texture.Apply();

			if (!HasVisiblePixels(texture))
			{
				Object.Destroy(texture);
				return null;
			}

			return Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f));
		}
		finally
		{
			RenderTexture.active = previous;

			// Switch the light off right away: destruction is deferred and the light must not touch this frame's real scene.
			Light? createdLight = lightObject.GetComponent<Light>();
			if (createdLight != null)
			{
				createdLight.enabled = false;
			}

			lightObject.SetActive(false);
			Object.Destroy(lightObject);
			Object.Destroy(cameraObject);
			target.Release();
			Object.Destroy(target);
		}
	}

	private static bool HasVisiblePixels(Texture2D texture)
	{
		Color32[] pixels = texture.GetPixels32();
		int visible = 0;
		foreach (Color32 pixel in pixels)
		{
			if (pixel.a > 12)
			{
				visible++;
			}
		}

		// A real render covers a decent part of the square; an empty or near-empty one means it failed.
		return visible > pixels.Length / 50;
	}

	private static Sprite CreateFallback()
	{
		const int size = 64;
		Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
		Color clear = new Color(0f, 0f, 0f, 0f);
		Color gold = new Color(1f, 0.82f, 0.25f, 1f);
		Color edge = new Color(0.65f, 0.45f, 0.1f, 1f);
		float center = (size - 1) * 0.5f;
		for (int y = 0; y < size; y++)
		{
			for (int x = 0; x < size; x++)
			{
				float distance = Mathf.Abs(x - center) + Mathf.Abs(y - center);
				texture.SetPixel(x, y, distance < center * 0.8f ? gold : distance < center * 0.95f ? edge : clear);
			}
		}

		texture.Apply();
		return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
	}
}
