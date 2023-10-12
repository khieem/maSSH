using Renci.SshNet;

public class Sc : IDisposable
{
   protected readonly string name;
	protected readonly string usrn;
	protected readonly string pswd;
	protected readonly string host;
	protected readonly ConnectionInfo connectioninfo;

   public Sc(string name, string host, string usrn, string pswd)
   {
      this.name = name;
      this.host = host;
      this.usrn = usrn;
      this.pswd = pswd;

      connectioninfo = new ConnectionInfo(host, usrn, new PasswordAuthenticationMethod(usrn, pswd));
   }

   public Sc(Session ss) : this(ss.name, ss.host, ss.usrn, ss.pswd) {}

   public void Connect() {}

	public void Dispose()
	{
		// GC.SuppressFinalize(this);
	}
}