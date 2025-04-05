#!/usr/bin/env dotnet-script
#r "nuget: Lestaly, 0.81.0"
#r "nuget: Kokuban, 0.2.0"
#load ".ldap-settings.csx"
#nullable enable
using System.DirectoryServices.Protocols;
using System.Net;
using Kokuban;
using Lestaly;

return await Paved.ProceedAsync(async () =>
{
    // Bind to LDAP server
    WriteLine("Bind to LDAP server");
    using var ldap = CreateLdapConnection(ldapSettings.Credentials.DirectoryConfigurator, bind: true);

    // Check group unit entry.
    while (true)
    {
        WriteLine("Enter a group name.");
        Write(">");
        var group = ReadLine();
        if (group.IsWhite()) break;

        var groupDn = $"cn={group},{ldapSettings.Directory.GroupUnitDn}";
        var groupEntry = await ldap.GetEntryOrDefaultAsync(groupDn);
        if (groupEntry == null)
        {
            WriteLine(Chalk.Yellow["no group"]);
            continue;
        }

        while (true)
        {
            try
            {
                var uid = ConsoleWig.Write("uid=").ReadLine();
                if (uid.IsWhite()) break;

                var personDn = $"uid={uid},{ldapSettings.Directory.PersonUnitDn}";
                await ldap.AddAttributeAsync(groupDn, "member", [personDn]);
                WriteLine(Chalk.Green[$"Added: {uid} to {group}"]);
            }
            catch (Exception ex)
            {
                WriteLine(Chalk.Red[$"Error: {ex.Message}"]);
            }
        }
        WriteLine();
    }
});
