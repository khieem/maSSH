using System.Text;
using System.Text.RegularExpressions;
using Renci.SshNet;

public class SSHc : IDisposable
{
	private readonly SshClient client;
	private ShellStream? shellStream;
	private readonly ConnectionInfo connectioninfo;
	private readonly string usrn;
	private readonly string pswd;
	private readonly string host;

   public SSHc(string host, string usrn, string pswd)
	{
		this.host = host;
		this.usrn = usrn;
		this.pswd = pswd;
		connectioninfo = new ConnectionInfo(host, usrn, new PasswordAuthenticationMethod(usrn, pswd));
		client = new SshClient(connectioninfo);

		Connect();
	}
	private void Connect()
	{
		client.Connect();
		Console.WriteLine($"Connected to {usrn}@{host}.");

		// string uid = client.RunCommand("id -u").Result;
		// (uid == "0") ? 
		if (usrn == "root") return;

		IDictionary<Renci.SshNet.Common.TerminalModes, uint> modes = new Dictionary<Renci.SshNet.Common.TerminalModes, uint>
		{
			{ Renci.SshNet.Common.TerminalModes.ECHO, 53 }
		};
		shellStream = client.CreateShellStream("xterm", 80,24, 800, 600, 1024, modes);

		// // Console.WriteLine("sudo");
		// SwithToRoot("khiemvn", shellStream);
		Sudo();
	}

   public string Execute(string command)
	{
		string result = usrn == "root" ? ExecAsRoot(command) : ExecAsUser(command);
		// client.Disconnect();
		return result;
	}

	private string ExecAsRoot(string command)
	{
		return client.RunCommand(command).Result;
	}

	private string ExecAsUser(string command)
	{
		WriteStream(command);
		return ReadStream();
	}

	private void Sudo()
	{
		_ = shellStream.Expect(new Regex(@"[$>]"));
		shellStream.WriteLine("sudo -v");
		string prompt = shellStream.Expect(new Regex(@"([$#>:])"));

		if (prompt.Contains(":"))
		{
			shellStream.WriteLine(pswd);
			_ = shellStream.Expect(new Regex(@"[$#>]"));
		}
	}

	private void WriteStream(string cmd)
	{
		var stream = shellStream;
			stream.WriteLine(cmd + "; echo kkkkkkkk");
			while (stream.Length == 0)
				Thread.Sleep(500);
	}

	private string ReadStream()
	{
		StringBuilder result = new StringBuilder();
		string line;
		while ((line = shellStream.ReadLine()) != "kkkkkkkk")
				result.AppendLine(line);

		string answer = result.ToString();
		int index = answer.IndexOf(System.Environment.NewLine);
		answer = answer[(index + System.Environment.NewLine.Length)..];
		return "[Out]: " + answer.Trim();
	}

	public void Dispose()
	{
		// Console.WriteLine("Terminated successfully.");
		GC.SuppressFinalize(this);
	}

	internal void Exec(string command)
	{
		WriteStream(command);

		string answer = ReadStream();
		int index = answer.IndexOf(System.Environment.NewLine);
		answer = answer.Substring(index + System.Environment.NewLine.Length);
		Console.WriteLine(answer.Trim());
	}
}