#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

using ShaderDecompiler.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShaderDecompiler.Decompilers {
	public class DecompilationSettings {
		public bool MinimumSimplifications = false;
		public int ComplexityThreshold = int.MaxValue;
		public Regex? ShaderPathFilter;
	}
}
