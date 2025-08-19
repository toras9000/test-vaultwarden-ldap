#!/usr/bin/env dotnet-script
#r "nuget: Lestaly.General, 0.102.0"
#r "nuget: Lestaly.Ldap, 0.100.0"
#r "nuget: Kokuban, 0.2.0"
#load ".settings.csx"
#nullable enable
using System.DirectoryServices.Protocols;
using System.Net;
using System.Text.RegularExpressions;
using Kokuban;
using Lestaly;
using Lestaly.Cx;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    // Read the access definition to be added
    var defineFile = ThisSource.RelativeFile("./assets/ldap/data/ldap-access-config.txt");
    var defineContents = await defineFile.ReadAllTextAsync();
    var defineAccesses = defineContents.MatchSplit(@"^[\r\n]+", RegexOptions.Multiline);

    // Bind to LDAP server
    WriteLine("Bind to LDAP server");
    var server = new LdapDirectoryIdentifier(ldapSettings.Server.Host, ldapSettings.Server.Port);
    using var ldap = new LdapConnection(server);
    ldap.SessionOptions.SecureSocketLayer = ldapSettings.Server.Ssl;
    ldap.SessionOptions.ProtocolVersion = ldapSettings.Server.ProtocolVersion;
    ldap.AuthType = AuthType.Basic;
    ldap.Credential = ldapSettings.Credentials.ConfigAdmin;
    ldap.Bind();

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
