#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Playwright, 1.52.0"
#r "nuget: Lestaly, 0.84.0"
#r "nuget: Kokuban, 0.2.0"
#load ".ldap-settings.csx"
#load ".vw-settings.csx"
#load ".vw-helper.csx"
#nullable enable
using System.Text.Json;
using Microsoft.Playwright;
using Kokuban;
using Lestaly;
using Lestaly.Cx;
using System.Threading;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    using var signal = new SignalCancellationPeriod();

    WriteLine("Load test entities info");
    var testEntities = await vwSettings.TestEntitiesFile.ReadJsonAsync<TestEntities>();
    if (testEntities == null) throw new PavedMessageException("Cannot load entities info");

    WriteLine("Get user info.");
    using var helper = new VaultwardenHelper(new(vwSettings.Service.Url));
    var userInfo = testEntities.Confirmer;
    var userCredential = new ClientCredentialsConnectTokenModel(
        scope: "api",
        client_id: userInfo.ClientId,
        client_secret: userInfo.ClientSecret,
        device_type: ClientDeviceType.OperaBrowser,
        device_name: Environment.MachineName,
        device_identifier: Environment.MachineName
    );
    var userToken = await helper.Identity.ConnectTokenAsync(userCredential, signal.Token);
    var userProfile = await helper.User.GetProfile(userToken, signal.Token);
    WriteLine($"User");
    WriteLine($"- Name : {userProfile.name}");
    WriteLine($"- Id   : {userProfile.id}");
    WriteLine($"- Mail : {userProfile.email}");
    WriteLine($"Organizations");
    foreach (var org in userProfile.organizations)
    {
        WriteLine($"- Name : {org.name}");
        WriteLine($"    - Id       : {org.id}");
        WriteLine($"    - MemberId : {org.organizationUserId}");
        WriteLine($"    - UserId   : {org.userId}");

        var collections = await helper.Organization.GetCollections(userToken, org.id, signal.Token);
        foreach (var coll in collections.data.Index())
        {
            WriteLine($"    - Collection[{coll.Index}]:");
            WriteLine($"        - Name : {coll.Item.name}");
            WriteLine($"        - Id   : {coll.Item.id}");
        }
    }
});
