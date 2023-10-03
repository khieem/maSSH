
using System.Collections.Concurrent;

namespace massh
{
	class Program
	{
		public static void Main(string[] args)
		{
			Session ss = new()
			{
				host = "127.0.0.1",
			// string port = "22";
				usrn = "khiemvn",
				pswd = "khiemvn"
			};

			Session _ss = new()
			{
				host = "127.0.0.1",
				// string port = "22";
				usrn = "khiemvn",
				pswd = "khiemvn",
			};

			Session __ss = new()
			{
				host = "127.0.0.1",
				// string port = "22";
				usrn = "root",
				pswd = "root",
			};

			int maxConnections = System.Environment.ProcessorCount;
			var opts = new ParallelOptions { MaxDegreeOfParallelism = maxConnections };
			Session[] sessions = { ss, _ss, __ss };
			
			Parallel.ForEach(sessions, opts, ss => {
				using (var client = new SSHc(ss))
				{
					List<string> commands = new List<string>()
					{
						"w",
						"date",
						"sleep 5"
					};

					foreach (string command in commands) {
						// Console.WriteLine("[In]: " + command);
						Console.WriteLine(client.Execute(command));         
					}
				}
			});
		}
	}
}
