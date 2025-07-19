#!/usr/bin/env dotnet-script
#r "nuget: VwConnector, 1.34.1-rev.5"
#r "nuget: Lestaly.General, 0.100.0"
#r "nuget: Kokuban, 0.2.0"
#load ".ldap-settings.csx"
#load ".vw-settings.csx"
#nullable enable
using System.Text.Json;
using System.Threading;
using Kokuban;
using Lestaly;
using Lestaly.Cx;
using VwConnector;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    using var signal = new SignalCancellationPeriod();

    WriteLine("Load test entities info");
    var testEntities = await vwSettings.TestEntitiesFile.ReadJsonAsync<TestEntities>();
    if (testEntities == null) throw new PavedMessageException("Cannot load entities info");

    WriteLine("Get user info.");
    using var helper = new VaultwardenConnector(new(vwSettings.Service.Url));
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
    var userProfile = await helper.User.GetProfileAsync(userToken, signal.Token);
    var stretchKey = helper.Utility.CreateStretchKey(vwSettings.Setup.TestUser.Mail, vwSettings.Setup.TestUser.Password, userToken.ToKdfConfig());
    var userKey = SymmetricCryptoKey.From(helper.Utility.Decrypt(stretchKey.EncKey, EncryptedData.Parse(userProfile.key)));
    var userPrivateKey = helper.Utility.Decrypt(userKey.EncKey, EncryptedData.Parse(userProfile.privateKey));

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

        var orgKey = SymmetricCryptoKey.From(helper.Utility.Decrypt(userPrivateKey, EncryptedData.Parse(org.key)));
        var collections = await helper.Organization.GetCollectionsAsync(userToken, org.id, signal.Token);
        foreach (var coll in collections.data.Index())
        {
            var name = helper.Utility.Decrypt(orgKey.EncKey, EncryptedData.Parse(coll.Item.name)).DecodeUtf8();
            WriteLine($"    - Collection[{coll.Index}]:");
            WriteLine($"        - Name : {name}");
            WriteLine($"        - Id   : {coll.Item.id}");
        }
    }
});
