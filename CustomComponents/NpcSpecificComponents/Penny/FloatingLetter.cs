/*
using System.Collections;
using MTM101BaldAPI;
using TMPro;
using UnityEngine;

namespace BBTimes.CustomComponents.NpcSpecificComponents.Penny
{
	public class FloatingLetter : MonoBehaviour, IClickable<int>
	{
		public void Initialize(CustomContent.NPCs.Penny pen, EnvironmentController ec)
		{
			this.pen = pen;
			this.ec = ec;
		}
		public void PickLetter(char letter)
		{
			assignedChar = letter;
			renderer.text = $"{letter}";
		}
		public void DisableClickTemporarily(float delay)
		{
			selected = 0;
			if (disableClickCor != null)
				StopCoroutine(disableClickCor);
			disableClickCor = StartCoroutine(TemporarilyDisableClick(delay));
		}
		IEnumerator TemporarilyDisableClick(float delay)
		{
			disabledClick = true;
			yield return new WaitForSecondsEnvironmentTimescale(ec, delay);
			disabledClick = false;
		}

		void Update()
		{
			if (ec && Time.timeScale != 0)
			{
				if (selected > 0)
				{
					renderer.transform.localScale += (selectedSize - renderer.transform.localScale) * 12f * ec.EnvironmentTimeScale * Time.deltaTime;
					return;
				}
				renderer.transform.localScale += (Vector3.one - renderer.transform.localScale) * 12f * ec.EnvironmentTimeScale * Time.deltaTime;
			}
		}

		public void Clicked(int player)
		{
			if (!pen || disabledClick || char.IsWhiteSpace(assignedChar)) return;
			pen.TakeLetter(assignedChar);
		}
		public void ClickableSighted(int player)
		{
			if (!disabledClick)
				selected++;
		}
		public void ClickableUnsighted(int player)
		{
			if (!disabledClick)
				selected--;
		}
		public bool ClickableHidden() => disabledClick;
		public bool ClickableRequiresNormalHeight() => false;

		public string Text { get => renderer.text; set => renderer.text = value; }
		bool disabledClick;

		[SerializeField]
		internal TextMeshPro renderer;

		[SerializeField]
		internal Vector3 selectedSize = Vector3.one * 1.5f;

		EnvironmentController ec;
		CustomContent.NPCs.Penny pen;
		char assignedChar = ' ';
		Coroutine disableClickCor;
		int selected = 0;
	}
}
*/