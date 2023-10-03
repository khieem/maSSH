
namespace massh
{
	class Program
	{
		private static void Main(string[] args)
		{
			string host = "127.0.0.1";
			// string port = "22";
			string usrn = "khiemvn";
			string pswd = "khiemvn";

			using (var client = new SSHc(host, usrn, pswd))
			{
				List<string> commands = new List<string>()
				{
					"pwd",
					"systemctl is-active sshd"
				};

				foreach (string command in commands) {
					Console.WriteLine("[In]: " + command);
					Console.WriteLine(client.Execute(command));         
				}
			}
		}
	}
}
