using System.DirectoryServices.Protocols;
using System.Net;
using Lestaly;
#nullable enable

var vwSettings = new
{
    // Vaultwarden service
    Service = new
    {
        // Vaultwarden URL
        Url = "http://localhost:8180",
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
        },
    },

    TestEntitiesFile = ThisSource.RelativeFile(".vw-test-entities.json"),

    // LDAP settings
    Directory = new
    {
        // Users search filter
        UsersFilter = "(&(objectClass=inetOrgPerson)(mail=*))",
    },
};

record TestOrganization(string Id, string ClientId, string ClientSecret);
record TestConfirmer(string Id, string ClientId, string ClientSecret);
record TestEntities(TestOrganization Organization, TestConfirmer Confirmer);
