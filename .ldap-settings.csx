using System.DirectoryServices.Protocols;
using System.Net;
#nullable enable

var ldapSettings = new
{
    // LDAP server settings
    Server = new
    {
        // Host name or ip
        Host = "localhost",

        // Port number
        Port = 389,

        // Use SSL
        Ssl = false,

        // LDAP protocol version
        ProtocolVersion = 3,
    },

    Credentials = new
    {
        // Config Admin credential
        ConfigAdmin = new NetworkCredential("cn=config-admin,cn=config", "config-admin-pass"),

        // Configurator credential
        DirectoryConfigurator = new NetworkCredential("uid=configurator,ou=operators,dc=myserver,o=home", "configurator-pass"),
    },

    Directory = new
    {
        // Person manage unit DN
        PersonUnitDn = "ou=persons,ou=accounts,dc=myserver,o=home",

        // Group manage unit DN
        GroupUnitDn = "ou=groups,dc=myserver,o=home",
    },
};

LdapConnection CreateLdapConnection(NetworkCredential? credential = null, bool bind = false)
{
    var server = new LdapDirectoryIdentifier(ldapSettings.Server.Host, ldapSettings.Server.Port);
    var ldap = new LdapConnection(server);
    ldap.SessionOptions.SecureSocketLayer = ldapSettings.Server.Ssl;
    ldap.SessionOptions.ProtocolVersion = ldapSettings.Server.ProtocolVersion;
    if (credential == null)
    {
        ldap.AuthType = AuthType.Anonymous;
    }
    else
    {
        ldap.AuthType = AuthType.Basic;
        ldap.Credential = credential;
    }
    if (bind) ldap.Bind();
    return ldap;
}