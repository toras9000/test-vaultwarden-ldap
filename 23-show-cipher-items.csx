#!/usr/bin/env dotnet-script
#r "nuget: VwConnector, 1.34.1-rev.6"
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

    var orgKeys = userProfile.organizations.ToDictionary(
        o => o.id,
        o => SymmetricCryptoKey.From(helper.Utility.Decrypt(userPrivateKey, EncryptedData.Parse(o.key)))
    );

    var items = await helper.Cipher.GetItemsAsync(userToken, signal.Token);
    foreach (var item in items.data)
    {
        var name = item.name;
        if (item.organizationId.IsWhite())
        {
            name = helper.Utility.Decrypt(userKey.EncKey, EncryptedData.Parse(item.name)).DecodeUtf8();
        }
        else if (orgKeys.TryGetValue(item.organizationId, out var orgKey))
        {
            name = helper.Utility.Decrypt(orgKey.EncKey, EncryptedData.Parse(item.name)).DecodeUtf8();
        }
        WriteLine($"- Name : {name}");
        WriteLine($"  - ID   : {item.id}");
        WriteLine($"  - Type : {item.type}");
    }

});
