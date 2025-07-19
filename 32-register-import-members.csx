#!/usr/bin/env dotnet-script
#r "nuget: VwConnector, 1.34.1-rev.6"
#r "nuget: Lestaly.General, 0.100.0"
#r "nuget: Kokuban, 0.2.0"
#load ".ldap-settings.csx"
#load ".vw-settings.csx"
#nullable enable
using System.Web;
using Kokuban;
using Lestaly;
using Lestaly.Cx;
using VwConnector;
using VwConnector.Agent;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    using var signal = new SignalCancellationPeriod();

    WriteLine("Load test entities info");
    var testEntities = await vwSettings.TestEntitiesFile.ReadJsonAsync<TestEntities>();
    if (testEntities == null) throw new PavedMessageException("Cannot load entities info");

    WriteLine("Get invite members");
    using var helper = new VaultwardenConnector(new(vwSettings.Service.Url));
    var userCredential = new ClientCredentialsConnectTokenModel(
        scope: "api",
        client_id: testEntities.Confirmer.ClientId,
        client_secret: testEntities.Confirmer.ClientSecret,
        device_type: ClientDeviceType.OperaBrowser,
        device_name: Environment.MachineName,
        device_identifier: Environment.MachineName
    );
    var userToken = await helper.Identity.ConnectTokenAsync(userCredential, signal.Token);
    var orgMembers = await helper.Organization.GetMembersAsync(userToken, testEntities.Organization.Id, new(true, true), signal.Token);

    WriteLine("Detection invite mail");
    var mailDir = ThisSource.RelativeDirectory("maildump");
    var joinUrls = mailDir.GetFiles("*-text.txt").OrderByDescending(f => f.Name)
        .Select(file => file.ReadAllLines().FirstOrDefault(l => l.StartsWith("Click here to join:"))?.SkipToken(':').Trim().ToString())
        .Where(url => url != null).Select(u => u!)
        .ToArray();
    var userInvites = orgMembers.data
        .Where(member => member.status == MembershipStatus.Invited)
        .Select(member =>
        {
            var encMail = Uri.EscapeDataString(member.email);
            var joinUrl = joinUrls.FirstOrDefault(url => url.Contains(encMail));
            return new { member, joinUrl, };
        })
        .Where(o => o.joinUrl != null)
        .ToArray();

    WriteLine("Register invite users");
    foreach (var invite in userInvites)
    {
        WriteLine($"  User: {invite.member.email}");
        var password = $"{invite.member.email.TakeToken('@')}-password";
        var joinQuery = HttpUtility.ParseQueryString(invite.joinUrl?.SkipToken('?').ToString() ?? "");
        var orgId = joinQuery["organizationId"] ?? "";
        var orgUserId = joinQuery["organizationUserId"] ?? "";
        var inviteToken = joinQuery["token"] ?? "";
        await helper.Account.RegisterUserInviteAsync(new(invite.member.email, password), orgUserId, inviteToken, signal.Token);

        WriteLine($"  .. accept org");
        using var userAgent = await VaultwardenAgent.CreateAsync(new Uri(vwSettings.Service.Url), new(invite.member.email, password), signal.Token);
        await userAgent.Connector.Organization.AcceptInviteAsync(userAgent.Token, orgId, orgUserId, new(inviteToken), signal.Token);
    }
});
