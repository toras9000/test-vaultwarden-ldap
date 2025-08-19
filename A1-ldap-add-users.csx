#!/usr/bin/env dotnet-script
#r "nuget: Lestaly.General, 0.102.0"
#r "nuget: Lestaly.Ldap, 0.100.0"
#r "nuget: Kokuban, 0.2.0"
#load ".settings.csx"
#nullable enable
using System.DirectoryServices.Protocols;
using System.Net;
using Kokuban;
using Lestaly;

var settings = new
{
    Group = "cn=vaultwarden,ou=groups,dc=myserver,o=home",
};

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    WriteLine("Bind to LDAP server");
    var server = new LdapDirectoryIdentifier(ldapSettings.Server.Host, ldapSettings.Server.Port);
    using var ldap = new LdapConnection(server);
    ldap.SessionOptions.SecureSocketLayer = ldapSettings.Server.Ssl;
    ldap.SessionOptions.ProtocolVersion = ldapSettings.Server.ProtocolVersion;
    ldap.AuthType = AuthType.Basic;
    ldap.Credential = ldapSettings.Credentials.DirectoryConfigurator;
    ldap.Bind();
    WriteLine(Chalk.Green[$".. OK"]);

    WriteLine("Enter user UID");
    while (true)
    {
        Write(">");
        var uid = ReadLine()?.Trim();
        if (uid == null) break;
        if (uid.IsEmpty()) continue;

        WriteLine($"User: {uid}");
        var userDn = $"uid={uid},{ldapSettings.Directory.PersonUnitDn}";
        var userEntry = await ldap.GetEntryOrDefaultAsync(userDn);
        if (userEntry != null)
        {
            WriteLine($".. Already exists");
            continue;
        }

        var cn = uid;
        var sn = uid;
        var mail = $"{uid}@myserver.home";
        var passwd = $"{uid}-pass";
        WriteLine(Chalk.Blue[$".. Entity:"]);
        WriteLine(Chalk.Blue[$"     DN={userDn}"]);
        WriteLine(Chalk.Blue[$"     cn={cn}"]);
        WriteLine(Chalk.Blue[$"     sn={sn}"]);
        WriteLine(Chalk.Blue[$"     mail={mail}"]);
        WriteLine(Chalk.Blue[$"     password={passwd}"]);

        var hash = LdapExtensions.MakePasswordHash.SSHA256(passwd);
        await ldap.CreateEntryAsync(userDn,
        [
            new("objectClass", ["inetOrgPerson", "extensibleObject"]),
                new("cn", cn),
                new("sn", sn),
                new("mail", mail),
                new("userPassword", hash),
            ]);
        WriteLine(Chalk.Green[$".. Created"]);

        WriteLine(Chalk.Green[$".. Add to group '{settings.Group}'"]);
        await ldap.AddAttributeAsync(settings.Group, "member", [userDn]);
        WriteLine(Chalk.Green[$".. Added"]);
        WriteLine();
    }
});
