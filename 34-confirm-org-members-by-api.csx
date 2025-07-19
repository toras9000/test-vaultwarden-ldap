#!/usr/bin/env dotnet-script
#r "nuget: Lestaly.General, 0.100.0"
#r "nuget: Kokuban, 0.2.0"
#load ".ldap-settings.csx"
#load ".vw-settings.csx"
#load ".vw-helper.csx"
#nullable enable
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using Kokuban;
using Lestaly;
using Lestaly.Cx;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    using var signal = new SignalCancellationPeriod();

    WriteLine("Load test entities info");
    var testEntities = await vwSettings.TestEntitiesFile.ReadJsonAsync<TestEntities>();
    if (testEntities == null) throw new PavedMessageException("Cannot load entities info");

    WriteLine("Import to vaultwarden users");
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
    var orgProfile = userProfile.organizations.First(o => o.id == testEntities.Organization.Id);

    var stretchKey = helper.Utility.CreateStretchKey(vwSettings.Setup.TestUser.Mail, vwSettings.Setup.TestUser.Password, userToken.ToKdfConfig());
    var userKey = SymmetricCryptoKey.From(helper.Utility.Decrypt(stretchKey.EncKey, EncryptedData.Parse(userProfile.key)));
    var userPrivateKey = helper.Utility.Decrypt(userKey.EncKey, EncryptedData.Parse(userProfile.privateKey));
    var orgKey = helper.Utility.Decrypt(userPrivateKey, EncryptedData.Parse(orgProfile.key));

    var orgMembers = await helper.Organization.GetMembers(userToken, orgProfile.id, new(true, true), signal.Token);
    foreach (var member in orgMembers.data)
    {
        WriteLine($"{member.email}: {member.id}");
        if (member.status != MembershipStatus.Accepted)
        {
            WriteLine($".. Skip: Not target");
            continue;
        }

        var memberPubKey = await helper.User.GetPublicKey(userToken, member.userId, signal.Token);
        var pubkeyBytes = memberPubKey.publicKey.DecodeBase64();
        var confirmKey = helper.Utility.EncryptRsa(pubkeyBytes!, orgKey);
        await helper.Organization.ConfirmMember(userToken, orgProfile.id, member.id, new(confirmKey.BuildString()), signal.Token);
        WriteLine($"..  Confirmed");
    }

});
