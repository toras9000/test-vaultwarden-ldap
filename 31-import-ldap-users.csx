#!/usr/bin/env dotnet-script
#r "nuget: Lestaly, 0.82.0"
#r "nuget: Kokuban, 0.2.0"
#load ".ldap-settings.csx"
#load ".vw-settings.csx"
#load ".vw-helper.csx"
#nullable enable
using System.DirectoryServices.Protocols;
using Kokuban;
using Lestaly;
using Lestaly.Cx;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    using var signal = new SignalCancellationPeriod();

    WriteLine("Load test entities info");
    var testEntities = await vwSettings.TestEntitiesFile.ReadJsonAsync<TestEntities>();
    if (testEntities == null) throw new PavedMessageException("Cannot load entities info");

    WriteLine("Search LDAP users");
    using var ldap = CreateLdapConnection(ldapSettings.Credentials.DirectoryConfigurator, bind: true);
    var searchResult = await ldap.SearchAsync(ldapSettings.Directory.PersonUnitDn, SearchScope.OneLevel, vwSettings.Directory.UsersFilter, signal.Token);
    var users = searchResult.Entries.OfType<SearchResultEntry>()
        .Select(e => new
        {
            dn = e.DistinguishedName,
            uid = e.GetAttributeFirstValue("uid")?.ToString(),
            mails = e.EnumerateAttributeValues("mail").Select(m => m.ToString()).ToArray(),
        })
        .Where(u => u.uid != null)
        .ToArray();

    WriteLine("Import to vaultwarden users");
    using var helper = new VaultwardenHelper(new(vwSettings.Service.Url));
    helper.Timeout = TimeSpan.FromMinutes(5);

    var orgInfo = testEntities.Organization;
    var orgCredential = new ClientCredentialsConnectTokenModel(
        scope: "api.organization",
        client_id: orgInfo.ClientId,
        client_secret: orgInfo.ClientSecret,
        device_type: ClientDeviceType.UnknownBrowser,
        device_name: Environment.MachineName,
        device_identifier: Environment.MachineName
    );
    var orgToken = await helper.ConnectTokenAsync(orgCredential, signal.Token);

    var importData = new ImportOrgArgs(
        overwriteExisting: false,
        members: users
            .Select(u => new ImportOrgMember(externalId: u.dn, deleted: false, email: u.mails.FirstOrDefault()))
            .ToArray(),
        groups: []
    );
    await helper.PublicOrgImportAsync(orgToken, importData, signal.Token);
});
