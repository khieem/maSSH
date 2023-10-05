using System.Text;
using System.Text.RegularExpressions;
using Renci.SshNet;

public class SSHc : IDisposable
{
	private readonly SshClient client;
	private ShellStream? shellStream;
	private readonly ConnectionInfo connectioninfo;
	private readonly string name;
	private readonly string usrn;
	private readonly string pswd;
	private readonly string host;

   public SSHc(string name, string host, string usrn, string pswd)
	{
		this.name = name;
		this.host = host;
		this.usrn = usrn;
		this.pswd = pswd;
		connectioninfo = new ConnectionInfo(host, usrn, new PasswordAuthenticationMethod(usrn, pswd));
		client = new SshClient(connectioninfo);

		Connect();
	}

	public SSHc(Session ss) : this(ss.name, ss.host, ss.usrn, ss.pswd) {}
	private void Connect()
	{
		client.Connect();
		// Console.WriteLine($"Connected to {usrn}@{name} ({host}).");

		if (usrn == "root") return;

		IDictionary<Renci.SshNet.Common.TerminalModes, uint> modes = new Dictionary<Renci.SshNet.Common.TerminalModes, uint>
		{
			{ Renci.SshNet.Common.TerminalModes.ECHO, 53 }
		};
		shellStream = client.CreateShellStream("xterm", 80,24, 800, 600, 1024, modes);

		Sudo();	// chạy sudo interactive lần đầu, cho phép các lần sudo sau chạy non-interactive
	}

   public string Execute(string command)
	{
		// root sử dụng phiên non-interactive cho đơn giản
		// non-root sử dụng ShellStream cho phép gửi mật khẩu khi sudo yêu cầu
		// có thể chuyển non-root sang non-interactive bằng cách đọc mk sudo từ stdin nhưng không sử dụng vì có thể đọc mk
		string result = usrn == "root" ? ExecAsRoot(command) : ExecAsUser(command);
		return result; // result chỉ chứa kết quả gốc, không có định dạng gì ở đây
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
		_ = shellStream.Expect(new Regex(@"[$>]"));						// prompt trắng
		shellStream.WriteLine("sudo -v");
		string prompt = shellStream.Expect(new Regex(@"([$#>:])"));	// có thể là prompt root hoặc yêu cầu mk

		if (prompt.Contains(":"))												// hỏi mật khẩu (":" ở cuối)		
		{
			shellStream.WriteLine(pswd);
			_ = shellStream.Expect(new Regex(@"[$#>]"));					// prompt mới, không bỏ phần này
		}
	}

	private void WriteStream(string cmd)
	{
		var stream = shellStream;
			stream.WriteLine(cmd + "; echo kkkkkkkk");					// tự đánh dấu kết thúc vì đang sử dụng stream
			while (stream.Length == 0)
				Thread.Sleep(500);
	}

	private string ReadStream()
	{
		StringBuilder result = new StringBuilder();
		string line;
		while ((line = shellStream.ReadLine()) != "kkkkkkkk")			// tìm lại output của lệnh, dừng khi gặp đánh dấu
				result.AppendLine(line);

		string answer = result.ToString();
		int index = answer.IndexOf(System.Environment.NewLine);
		answer = answer[(index + System.Environment.NewLine.Length)..];
		return answer.Trim() + Environment.NewLine;
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