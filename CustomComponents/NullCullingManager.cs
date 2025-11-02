using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NullCullingManager : MonoBehaviour
{
	// Map a renderer to all cells that reference it
	private readonly Dictionary<Renderer, HashSet<Cell>> _rendererToCells = [];

	[SerializeField]
	internal CullingManager cullMan;

	// Recalculate all known renderers' visibility
	public void CheckAllChunks()
	{
		foreach (var kv in _rendererToCells)
			UpdateRendererVisibility(kv.Key);
	}

	// Associate a renderer with a cell
	public void AddRendererToCell(Cell cell, Renderer newRend)
	{
		if (!_rendererToCells.TryGetValue(newRend, out var cellSet))
		{
			cellSet = [];
			_rendererToCells[newRend] = cellSet;
		}
		cellSet.Add(cell);

		// Immediately update renderer visibility
		UpdateRendererVisibility(newRend);
	}

	// Determine whether a renderer should be enabled:
	// enabled if any of its associated cells has a chunk and that chunk is Rendering.
	private void UpdateRendererVisibility(Renderer renderer)
	{
		if (!renderer) return;

		if (!_rendererToCells.TryGetValue(renderer, out var cells) || cells.Count == 0)
		{
			// No associated cells -> disable renderer to be safe
			renderer.enabled = false;
			return;
		}

		bool shouldBeEnabled = cells.Any(c => c.hasChunk && c.Chunk.Rendering);
		renderer.enabled = shouldBeEnabled;
	}
}