#!/usr/bin/env dotnet-script
#r "nuget: Lestaly, 0.84.0"
#r "nuget: Kokuban, 0.2.0"
#load ".ldap-settings.csx"
#nullable enable
using System.DirectoryServices.Protocols;
using System.Net;
using Kokuban;
using Lestaly;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    // Read the access definition to be added
    var defineFile = ThisSource.RelativeFile("12-ldap-setup-config-access-data.txt");
    var defineAccesses = defineFile.EnumerateTextBlocks().ToArray();

    // Bind to LDAP server
    WriteLine("Bind to LDAP server");
    using var ldap = CreateLdapConnection(ldapSettings.Credentials.ConfigAdmin, bind: true);

    // Read the existing access definition.
    WriteLine("Search existing access");
    var configDn = "olcDatabase={2}mdb,cn=config";
    var searchResult = await ldap.SearchAsync(configDn, SearchScope.Base);
    var configEntry = searchResult.Entries[0];
    var accessExists = configEntry.EnumerateAttributeValues("olcAccess").ToArray();

    // Remove all existing access.
    if (0 < accessExists.Length)
    {
        WriteLine("Delete all access");
        var deleteRsp = await ldap.DeleteAttributeAsync(configDn, "olcAccess", defineAccesses);
        if (deleteRsp.ResultCode != 0) throw new PavedMessageException($"failed to modify: {deleteRsp.ErrorMessage}");
    }

    // Add defined access.
    WriteLine("Request to add access.");
    {
        var modifyRsp = await ldap.AddAttributeAsync(configDn, "olcAccess", defineAccesses);
        if (modifyRsp.ResultCode != 0) throw new PavedMessageException($"failed to modify: {modifyRsp.ErrorMessage}");
    }

    WriteLine("Completed.");
});

public static IEnumerable<string> EnumerateTextBlocks(this FileInfo self)
{
    var buffer = new StringBuilder();
    foreach (var line in self.ReadLines())
    {
        if (line.IsWhite())
        {
            if (0 < buffer.Length)
            {
                yield return buffer.ToString();
                buffer.Clear();
            }
            continue;
        }

        buffer.AppendLine(line);
    }

    if (0 < buffer.Length)
    {
        yield return buffer.ToString();
    }
}
