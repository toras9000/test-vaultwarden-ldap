#!/usr/bin/env dotnet-script
#r "nuget: Lestaly, 0.84.0"
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
        WriteLine("Enter the uid of the person to be created.");
        Write(">");
        var uid = ReadLine();
        if (uid.IsWhite()) break;

        try
        {
            var cn = ConsoleWig.Write("cn=").ReadLine();
            var sn = ConsoleWig.Write("sn=").ReadLine();
            var mail = ConsoleWig.Write("mail=").ReadLine();
            var passwd = ConsoleWig.Write("password(no echo)=").ReadLineIntercepted();
            var hash = LdapExtensions.MakePasswordHash.SSHA256(passwd);
            WriteLine();

            var personDn = $"uid={uid},{ldapSettings.Directory.PersonUnitDn}";
            await ldap.CreateEntryAsync(personDn,
            [
                new("objectClass", ["inetOrgPerson", "extensibleObject"]),
                new("cn", cn),
                new("sn", sn),
                new("mail", mail),
                new("userPassword", hash),
            ]);
            WriteLine(Chalk.Green[$"Created: {personDn}"]);
        }
        catch (Exception ex)
        {
            WriteLine(Chalk.Red[$"Error: {ex.Message}"]);
        }
        WriteLine();
    }
});
