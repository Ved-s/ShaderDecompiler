#region License
/*
  ShaderDecompiler - Direct3D shader decompiler

  Released under Microsoft Public License
  See LICENSE for details
*/
#endregion

namespace ShaderDecompiler.CommandLine {
	public class ConsoleCaller : CommandCaller {
		public override void Respond(string response) => Console.WriteLine(response);
	}
}
