using System.Net;
using Lestaly;
#nullable enable

var vwSettings = new
{
    // Vaultwarden service
    Service = new
    {
        // Vaultwarden URL
        Url = new Uri("http://localhost:8180"),
    },

    Setup = new
    {
        Admin = new
        {
            Password = "admin-pass",
        },

        TestUser = new
        {
            Mail = "tester@myserver.home",
            Password = "tester-password",
        },

        TestOrg = new
        {
            Name = "TestOrg",
            Collections = new[]
            {
                "Collec1",
                "Collec2",
            },
        },
    },
};

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
