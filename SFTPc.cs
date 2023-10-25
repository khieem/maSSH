using Renci.SshNet;
using Renci.SshNet.Common;

public class SFTPc : Sc
{
   private readonly SftpClient client;
   private static FileStream fs;
   private readonly string path;
   private readonly string fpath;

   public SFTPc(string name, string host, string usrn, string pswd, string fpath) : base(name, host, usrn, pswd)
   {
      path = usrn == "root" ? "/root" : $"/home/{usrn}";
      this.fpath = fpath;
      fs = new FileStream(fpath, FileMode.Open, FileAccess.Read);
      client = new SftpClient(connectioninfo);

      try
		{
			client.Connect();
		}
		catch (SshAuthenticationException)
		{
			passwordDenied = true;
			return;
		}
      // client.Connect();
   }

   public SFTPc(Session ss, string fpath) : this(ss.name, ss.host, ss.usrn, ss.pswd, fpath) {}

   new void Connect() {
      client.Connect();
   }

   public void UploadSingleFile()
   {
      if (passwordDenied)
      {
         Console.WriteLine(name + " NOT completed. PASSWORD DENIED!");
      }
      else
      {
         // Console.WriteLine(name + " start");
         client.UploadFile(fs, fpath);
         Console.WriteLine(name + " completed.");
      }
      fs.Close();
   }

   public new void Dispose()
   {
      fs.Close();
      client.Disconnect();
      base.Dispose();
   }
}