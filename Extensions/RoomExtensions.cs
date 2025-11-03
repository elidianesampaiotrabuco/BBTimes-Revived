using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BBTimes.Extensions
{
	public static class RoomExtensions
	{
		// Random regex function made by gpt to convert any name in captions easier inside the editor
		// Like: securitycamera -> Security Camera
		public static string ToFriendlyName(this string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				return string.Empty;

			// Trim and replace separators
			var s = input.Trim();
			s = Regex.Replace(s, "[_\\-]+", " ");

			// Insert space before camel-case transitions: "SecurityCamera" -> "Security Camera"
			s = Regex.Replace(s, "(?<=[a-z])(?=[A-Z])", " ");

			// Collapse multiple spaces
			s = Regex.Replace(s, "\\s+", " ").Trim();

			// If already contains spaces (or was split by camelCase), just capitalize each word
			if (s.Contains(" "))
				return string.Join(" ", s.Split(' ')
					.Where(w => !string.IsNullOrWhiteSpace(w))
					.Select(w => Capitalize(w)));

			// Otherwise try to split concatenated lowercase words using a small known-word list
			var lower = s.ToLowerInvariant();
			var tokens = new List<string>();
			var knownWords = KnownWords.OrderByDescending(w => w.Length).ToArray(); // longest-first greedy

			int i = 0;
			while (i < lower.Length)
			{
				bool matched = false;
				foreach (var w in knownWords)
				{
					if (i + w.Length <= lower.Length && lower.Substring(i, w.Length) == w)
					{
						tokens.Add(w);
						i += w.Length;
						matched = true;
						break;
					}
				}

				if (!matched)
				{
					// No known word matched at current position: take characters until the next known word or end
					int j = -1;
					for (int k = i + 1; k <= lower.Length; k++)
					{
						foreach (var w in knownWords)
						{
							if (k + w.Length <= lower.Length && lower.Substring(k, w.Length) == w)
							{
								j = k;
								break;
							}
						}
						if (j != -1) break;
					}

					if (j == -1)
					{
						// Nothing else; take the remainder
						tokens.Add(lower.Substring(i));
						break;
					}
					else
					{
						tokens.Add(lower.Substring(i, j - i));
						i = j;
					}
				}
			}

			// Capitalize tokens and join
			return string.Join(" ", tokens.Where(t => !string.IsNullOrWhiteSpace(t)).Select(Capitalize));
		}

		private static string Capitalize(string w)
		{
			if (string.IsNullOrEmpty(w)) return string.Empty;
			if (w.Length == 1) return w.ToUpperInvariant();
			return char.ToUpperInvariant(w[0]) + w.Substring(1).ToLowerInvariant();
		}

		// A small list of common words to help split concatenated room names. Extend as needed.
		private static readonly string[] KnownWords =
		[
			"security", "camera", "computer", "bathroom", "toilet", "kitchen", "living", "dining",
			"bed", "bedroom", "room", "office", "hall", "garage", "lobby", "study", "basement", "attic",
			"storage", "closet", "pantry", "balcony", "corridor", "stair", "entry", "foyer"
		];
	}
}
