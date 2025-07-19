#!/usr/bin/env dotnet-script
#r "nuget: Lestaly.General, 0.100.0"
#r "nuget: Kokuban, 0.2.0"
#load ".ldap-settings.csx"
#nullable enable
using System.DirectoryServices.Protocols;
using System.Net;
using Kokuban;
using Lestaly;

return await Paved.ProceedAsync(async () =>
{
    // Default user in group
    var defaultMemberDn = "uid=authenticator,ou=operators,dc=myserver,o=home";

    // Bind to LDAP server
    WriteLine("Bind to LDAP server");
    using var ldap = CreateLdapConnection(ldapSettings.Credentials.DirectoryConfigurator, bind: true);

    // Check group unit entry.
    while (true)
    {
        WriteLine("Enter the name of the group to be created.");
        Write(">");
        var name = ReadLine();
        if (name.IsWhite()) break;

        try
        {
            var groupDn = $"cn={name},{ldapSettings.Directory.GroupUnitDn}";
            await ldap.CreateEntryAsync(groupDn,
            [
                new("objectClass", "groupOfNames"),
                new("cn", name),
                new("member", defaultMemberDn),
            ]);
            WriteLine(Chalk.Green[$"Created: {groupDn}"]);
        }
        catch (Exception ex)
        {
            WriteLine(Chalk.Red[$"Error: {ex.Message}"]);
        }
        WriteLine();
    }
});
