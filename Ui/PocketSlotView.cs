using System.Globalization;
using Pockets.Api;
using Pockets.Net;
using TMPro;
using UnityEngine;

namespace Pockets.Ui;

/// <summary>
/// Draws the local player's pocketed valuable on its inventory slot: its picture in place of the item icon and
/// its current value as a small label. Attached to each <see cref="InventorySpot"/>; vanilla does not know about it.
/// </summary>
internal sealed class PocketSlotView : MonoBehaviour
{
	private const float LabelRefreshSeconds = 0.25f;

	private InventorySpot spot = null!;
	private TextMeshProUGUI? valueLabel;
	private float nextLabelRefresh;

	internal static PocketSlotView GetOrAdd(InventorySpot spot)
	{
		PocketSlotView? view = spot.GetComponent<PocketSlotView>();
		if (view == null)
		{
			view = spot.gameObject.AddComponent<PocketSlotView>();
		}

		return view;
	}

	private void Awake()
	{
		spot = GetComponent<InventorySpot>();
	}

	private void Update()
	{
		if (Time.unscaledTime < nextLabelRefresh)
		{
			return;
		}

		nextLabelRefresh = Time.unscaledTime + LabelRefreshSeconds;
		ValuableObject? valuable = CurrentValuable();
		if (valuable != null)
		{
			ShowLabel(valuable);
		}
	}

	/// <summary>Called after vanilla draws the slot: replaces the empty look with the pocketed valuable, or hides our label.</summary>
	internal void Refresh()
	{
		ValuableObject? valuable = CurrentValuable();
		if (valuable == null)
		{
			HideLabel();
			return;
		}

		spot.SetEmoji(ValuableIconRenderer.Get(valuable.gameObject));
		ShowLabel(valuable);
	}

	private ValuableObject? CurrentValuable()
	{
		if (spot == null || !ConfigurationController.IsEnabled || !SemiFunc.RunIsLevel())
		{
			return null;
		}

		int playerKey = PlayerKeys.Local();
		if (playerKey == PlayerKeys.NoPlayer)
		{
			return null;
		}

		PlayerPockets? pockets = PocketsApi.Find(playerKey);
		int index = spot.inventorySpotIndex;
		if (pockets == null || index < 0 || index >= pockets.Valuables.Length)
		{
			return null;
		}

		return pockets.Valuables[index];
	}

	private void ShowLabel(ValuableObject valuable)
	{
		if (!ConfigurationController.ShowSlotValue)
		{
			HideLabel();
			return;
		}

		TextMeshProUGUI? label = EnsureLabel();
		if (label == null)
		{
			return;
		}

		label.text = FormatValue(valuable.dollarValueCurrent);
		label.enabled = true;
	}

	private void HideLabel()
	{
		if (valueLabel != null)
		{
			valueLabel.enabled = false;
		}
	}

	private TextMeshProUGUI? EnsureLabel()
	{
		if (valueLabel != null)
		{
			return valueLabel;
		}

		TextMeshProUGUI? template = spot.noItem;
		if (template == null)
		{
			return null;
		}

		// Reuse the slot's own text style, then park the copy under the icon.
		valueLabel = Instantiate(template, template.transform.parent);
		valueLabel.name = "PocketValue";
		valueLabel.alignment = TextAlignmentOptions.Center;
		valueLabel.enableAutoSizing = false;
		valueLabel.fontSize = template.fontSize * 0.55f;
		valueLabel.color = Color.white;
		valueLabel.raycastTarget = false;
		valueLabel.outlineWidth = 0.3f;
		valueLabel.outlineColor = new Color32(0, 0, 0, 255);

		if (spot.inventoryIcon != null)
		{
			// Sit inside the lower edge of the picture: the slots are at the very bottom of the screen,
			// so a label under the icon would be cut off.
			RectTransform iconRect = spot.inventoryIcon.rectTransform;
			float inset = valueLabel.fontSize * 0.7f;
			valueLabel.rectTransform.anchoredPosition = iconRect.anchoredPosition + new Vector2(0f, iconRect.rect.yMin + inset);
		}

		return valueLabel;
	}

	private static string FormatValue(float value)
	{
		return value >= 1000f
			? "$" + (value / 1000f).ToString("0.#", CultureInfo.InvariantCulture) + "K"
			: "$" + value.ToString("0", CultureInfo.InvariantCulture);
	}
}
