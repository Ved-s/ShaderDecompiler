#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using System.Text.RegularExpressions;

namespace ShaderDecompiler.CommandLine {
	public class CommandReader {
		private string String;
		private int Position;

		public bool HasData => Position < String.Length;
		public string RemainingData => String.Substring(Position);

		public CommandReader(string @string) {
			String = @string;
		}

		public Match Match(Regex regex, bool endPosition = false) {
			Match match = regex.Match(String, Position);
			if (match.Success && endPosition)
				Position += match.Length;
			return match;
		}

		public bool TryMatch(Regex regex, out Match match, bool endPosition = false) {
			match = Match(regex, endPosition);
			return match.Success && match.Index == Position - match.Length;
		}

		public void SkipWhitespaces() {
			while (HasData && char.IsWhiteSpace(String[Position]))
				Position++;
		}
	}
}
