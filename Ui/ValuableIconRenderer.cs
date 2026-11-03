using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Pockets.Ui;

/// <summary>
/// Provides the small picture shown on an inventory slot for a pocketed valuable. Valuables have no prebaked
/// icon (items do), so the picture comes from, in order: an icon packaged inside the mod, a picture rendered
/// at runtime from the valuable's meshes with a throwaway camera and studio lighting, or a generic gem.
/// Results are cached per valuable type.
/// </summary>
internal static class ValuableIconRenderer
{
	internal const int Size = 128;

	private const float FieldOfView = 30f;
	private const string EmbeddedPrefix = "Pockets.Icons.";
	private static readonly Vector3 FarOrigin = new Vector3(0f, 5000f, 0f);
	private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>();
	private static Sprite? fallback;

	/// <summary>The generic icon used when no picture is available.</summary>
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

	internal static string KeyOf(GameObject valuable)
	{
		return valuable.name.Replace("(Clone)", string.Empty).Trim();
	}

	/// <summary>Makes sure this valuable type has an icon in the cache. For a runtime render, call it while the valuable is still full size.</summary>
	internal static void Ensure(GameObject valuable)
	{
		string key = KeyOf(valuable);
		if (Cache.TryGetValue(key, out Sprite? cached) && cached != null)
		{
			return;
		}

		Sprite? sprite = LoadEmbedded(key);
		string source = "packaged";
		if (sprite == null)
		{
			source = "rendered";
			try
			{
				Texture2D? texture = RenderTexture2D(valuable);
				sprite = texture != null ? Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f)) : null;
			}
			catch (Exception exception)
			{
				PocketsPlugin.Logger.LogWarning($"Could not render an icon for {key}: {exception.Message}");
			}
		}

		if (sprite == null)
		{
			source = "generic";
		}

		PocketsPlugin.Logger.LogInfo($"Icon for {key}: {source}");
		Cache[key] = sprite != null ? sprite : Fallback;
	}

	/// <summary>The cached icon for this valuable type, or the generic one.</summary>
	internal static Sprite Get(GameObject valuable)
	{
		return Cache.TryGetValue(KeyOf(valuable), out Sprite? sprite) && sprite != null ? sprite : Fallback;
	}

	private static Sprite? LoadEmbedded(string key)
	{
		try
		{
			using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedPrefix + key + ".png");
			if (stream == null)
			{
				return null;
			}

			byte[] bytes = new byte[stream.Length];
			int read = 0;
			while (read < bytes.Length)
			{
				int count = stream.Read(bytes, read, bytes.Length - read);
				if (count <= 0)
				{
					break;
				}

				read += count;
			}

			Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
			if (!TryDecodePng(texture, bytes))
			{
				Object.Destroy(texture);
				return null;
			}

			return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
		}
		catch (Exception exception)
		{
			PocketsPlugin.Logger.LogWarning($"Could not load the packaged icon for {key}: {exception.Message}");
			return null;
		}
	}

	/// <summary>
	/// Decodes PNG bytes into a texture through <c>ImageConversion.LoadImage(Texture2D, byte[])</c>, called by reflection:
	/// a direct call makes the compiler look at its <c>ReadOnlySpan</c> overload, which this net48 build cannot reference.
	/// </summary>
	private static bool TryDecodePng(Texture2D texture, byte[] bytes)
	{
		MethodInfo? method = typeof(ImageConversion).GetMethod(
			"LoadImage", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(Texture2D), typeof(byte[]) }, null);
		return method != null && method.Invoke(null, new object[] { texture, bytes }) is true;
	}

	/// <summary>One mesh of a valuable with its transform relative to the valuable's root.</summary>
	private readonly struct Part
	{
		internal Part(Mesh mesh, Material[] materials, Matrix4x4 matrix)
		{
			Mesh = mesh;
			Materials = materials;
			Matrix = matrix;
		}

		internal Mesh Mesh { get; }

		internal Material[] Materials { get; }

		internal Matrix4x4 Matrix { get; }
	}

	/// <summary>
	/// Renders a valuable (a scene instance or a prefab asset) to a square transparent texture with studio lighting.
	/// Returns null when there is nothing to draw or the picture came out empty. Also used by the icon baking tool.
	/// </summary>
	internal static Texture2D? RenderTexture2D(GameObject source)
	{
		List<Part> parts = new List<Part>();
		Bounds bounds = default;
		bool hasBounds = false;
		Matrix4x4 toRoot = source.transform.worldToLocalMatrix;
		foreach (MeshRenderer renderer in source.GetComponentsInChildren<MeshRenderer>())
		{
			if (!renderer.enabled || !renderer.gameObject.activeSelf || !renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
			{
				continue;
			}

			Material[] materials = renderer.sharedMaterials;
			if (materials.Length == 0)
			{
				continue;
			}

			Matrix4x4 matrix = toRoot * renderer.transform.localToWorldMatrix;
			parts.Add(new Part(filter.sharedMesh, materials, matrix));
			Bounds partBounds = TransformBounds(filter.sharedMesh.bounds, matrix);
			if (hasBounds)
			{
				bounds.Encapsulate(partBounds);
			}
			else
			{
				bounds = partBounds;
				hasBounds = true;
			}
		}

		if (!hasBounds)
		{
			return null;
		}

		float radius = Mathf.Max(bounds.extents.magnitude, 0.001f);
		Vector3 direction = new Vector3(0.6f, 0.45f, -1f).normalized;
		float distance = FitDistance(bounds, direction, radius);
		Vector3 cameraPosition = FarOrigin + direction * distance;

		AmbientMode previousAmbientMode = RenderSettings.ambientMode;
		Color previousAmbientLight = RenderSettings.ambientLight;
		bool previousFog = RenderSettings.fog;
		RenderTexture target = new RenderTexture(Size, Size, 24, RenderTextureFormat.ARGB32);
		GameObject cameraObject = new GameObject("PocketsIconCamera") { hideFlags = HideFlags.HideAndDontSave };
		GameObject lightObject = new GameObject("PocketsIconLights") { hideFlags = HideFlags.HideAndDontSave };
		RenderTexture previousActive = RenderTexture.active;
		try
		{
			// Neutral studio lighting for this render only; everything is restored before the real frame renders.
			RenderSettings.ambientMode = AmbientMode.Flat;
			RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
			RenderSettings.fog = false;

			Camera camera = cameraObject.AddComponent<Camera>();
			camera.enabled = false;
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
			camera.cullingMask = 1;
			camera.nearClipPlane = 0.001f;
			camera.farClipPlane = distance + radius * 4f;
			camera.fieldOfView = FieldOfView;
			camera.allowHDR = false;
			camera.allowMSAA = false;
			camera.targetTexture = target;
			camera.transform.position = cameraPosition;
			camera.transform.LookAt(FarOrigin);

			AddLight(lightObject, "Key", 1.0f, Quaternion.LookRotation(new Vector3(-0.5f, -0.7f, 0.6f)));
			AddLight(lightObject, "Fill", 0.45f, Quaternion.LookRotation(new Vector3(0.7f, -0.2f, 0.5f)));

			foreach (Part part in parts)
			{
				Matrix4x4 matrix = Matrix4x4.Translate(FarOrigin - bounds.center) * part.Matrix;
				for (int sub = 0; sub < part.Mesh.subMeshCount; sub++)
				{
					Graphics.DrawMesh(part.Mesh, matrix, part.Materials[Mathf.Min(sub, part.Materials.Length - 1)], 0, camera, sub);
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

			return texture;
		}
		finally
		{
			RenderTexture.active = previousActive;
			RenderSettings.ambientMode = previousAmbientMode;
			RenderSettings.ambientLight = previousAmbientLight;
			RenderSettings.fog = previousFog;

			// Switch the lights off right away: destruction is deferred and they must not touch this frame's real scene.
			foreach (Light light in lightObject.GetComponentsInChildren<Light>())
			{
				light.enabled = false;
			}

			lightObject.SetActive(false);
			Object.Destroy(lightObject);
			Object.Destroy(cameraObject);
			target.Release();
			Object.Destroy(target);
		}
	}

	/// <summary>
	/// The closest camera distance (along <paramref name="direction"/> from the bounds center) at which every corner of the
	/// bounds still fits the view, so every icon fills its square instead of leaving wide margins.
	/// </summary>
	private static float FitDistance(Bounds bounds, Vector3 direction, float radius)
	{
		float tangent = Mathf.Tan(FieldOfView * 0.5f * Mathf.Deg2Rad) * 0.94f;
		Vector3 forward = -direction;
		Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;
		Vector3 up = Vector3.Cross(forward, right);

		float distance = radius * 0.5f;
		for (int corner = 0; corner < 8; corner++)
		{
			Vector3 offset = new Vector3(
				(corner & 1) == 0 ? -bounds.extents.x : bounds.extents.x,
				(corner & 2) == 0 ? -bounds.extents.y : bounds.extents.y,
				(corner & 4) == 0 ? -bounds.extents.z : bounds.extents.z);
			float x = Vector3.Dot(offset, right);
			float y = Vector3.Dot(offset, up);
			float depth = Vector3.Dot(offset, forward);
			distance = Mathf.Max(distance, Mathf.Abs(x) / tangent - depth, Mathf.Abs(y) / tangent - depth);
		}

		return distance * 1.04f;
	}

	private static void AddLight(GameObject parent, string name, float intensity, Quaternion rotation)
	{
		GameObject child = new GameObject(name) { hideFlags = HideFlags.HideAndDontSave };
		child.transform.SetParent(parent.transform, false);
		child.transform.rotation = rotation;
		Light light = child.AddComponent<Light>();
		light.type = LightType.Directional;
		light.intensity = intensity;
		light.shadows = LightShadows.None;
		light.cullingMask = 1;
	}

	private static Bounds TransformBounds(Bounds local, Matrix4x4 matrix)
	{
		Vector3 center = matrix.MultiplyPoint3x4(local.center);
		Vector3 extents = local.extents;
		Vector3 axisX = matrix.MultiplyVector(new Vector3(extents.x, 0f, 0f));
		Vector3 axisY = matrix.MultiplyVector(new Vector3(0f, extents.y, 0f));
		Vector3 axisZ = matrix.MultiplyVector(new Vector3(0f, 0f, extents.z));
		Vector3 worldExtents = new Vector3(
			Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
			Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
			Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z));
		return new Bounds(center, worldExtents * 2f);
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
