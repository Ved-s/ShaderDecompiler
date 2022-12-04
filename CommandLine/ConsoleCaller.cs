namespace ShaderDecompiler.CommandLine {
	public class ConsoleCaller : CommandCaller {
		public override void Respond(string response) => Console.WriteLine(response);
	}
}
