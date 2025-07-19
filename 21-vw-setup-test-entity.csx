#!/usr/bin/env dotnet-script
#r "nuget: VwConnector, 1.34.1-rev.5"
#r "nuget: Lestaly.General, 0.100.0"
#r "nuget: Kokuban, 0.2.0"
#load ".ldap-settings.csx"
#load ".vw-settings.csx"
#nullable enable
using System.Text.Json;
using System.Threading;
using System.Web;
using Kokuban;
using Lestaly;
using Lestaly.Cx;
using VwConnector;
using VwConnector.Agent;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    using var signal = new SignalCancellationPeriod();

    WriteLine("Invite test user");
    var userInfo = vwSettings.Setup.TestUser;
    var orgInfo = vwSettings.Setup.TestOrg;
    using var helper = new VaultwardenConnector(new(vwSettings.Service.Url));
    var adminToken = await helper.Admin.GetTokenAsync(vwSettings.Setup.Admin.Password, signal.Token);
    var users = await helper.Admin.UsersAsync(adminToken, signal.Token);
    if (users.Any(u => u.email == userInfo.Mail))
    {
        WriteLine(".. Already exists");
        return;
    }
    var testUser = await helper.Admin.InviteAsync(adminToken, userInfo.Mail, signal.Token);

    WriteLine("Detection invite mail");
    var joinUri = default(Uri);
    var mailDir = ThisSource.RelativeDirectory("maildump");
    using (var breaker = signal.Token.CreateLink(TimeSpan.FromSeconds(30)))
    {
        var encUser = Uri.EscapeDataString(userInfo.Mail);
        while (true)
        {
            var lastMail = mailDir.GetFiles("*-text.txt").OrderByDescending(f => f.Name).FirstOrDefault();
            var joinLine = Try.Func(() => lastMail?.ReadAllLines().FirstOrDefault(l => l.StartsWith("Click here to join:") && l.Contains(encUser)), _ => default);
            if (joinLine != null) { joinUri = new Uri(joinLine.SkipToken(':').Trim().ToString()); break; }
            await Task.Delay(TimeSpan.FromMilliseconds(500), breaker.Token);
        }
    }

    WriteLine("Register test user");
    var joinQuery = HttpUtility.ParseQueryString(joinUri.AbsoluteUri.SkipToken('?').ToString());
    var orgUserId = joinQuery["organizationUserId"] ?? "";
    var inviteToken = joinQuery["token"] ?? "";
    await helper.Account.RegisterUserInviteAsync(new(userInfo.Mail, userInfo.Password), orgUserId, inviteToken, signal.Token);

    WriteLine("Create test organization");
    var agent = await VaultwardenAgent.CreateAsync(helper, new(userInfo.Mail, userInfo.Password), signal.Token);
    var org = await agent.Affect.CreateOrganizationAsync(orgInfo.Name, "DefaultCollection", signal.Token);

    WriteLine("Get test entities information");
    var prelogin = await agent.Connector.Identity.PreloginAsync(new(userInfo.Mail), signal.Token);
    var passHash = agent.Connector.Utility.CreatePasswordHash(userInfo.Mail, userInfo.Password, prelogin).EncodeBase64();
    var userApiKey = await helper.User.GetApiKeyAsync(agent.Token, new(masterPasswordHash: passHash), signal.Token);
    var userProfile = default(VwUserProfile);
    for (var i = 0; i < 3; i++)
    {
        var user = await helper.User.GetProfileAsync(agent.Token, signal.Token);
        if (0 < user.organizations?.Length)
        {
            userProfile = user;
            break;
        }
        await Task.Delay(TimeSpan.FromSeconds(1));
    }
    if (userProfile == null) throw new PavedMessageException("Cannot access register page");

    var orgProfile = userProfile.organizations.First(o => o.name == orgInfo.Name);
    var orgApiKey = await helper.Organization.GetApiKeyAsync(agent.Token, orgProfile.id, new(masterPasswordHash: passHash), signal.Token);

    WriteLine("Create test collections");
    var collections = new List<TestCollection>();
    foreach (var colName in vwSettings.Setup.TestOrg.Collections)
    {
        var collection = await agent.Affect.CreateCollectionAsync(org.Id, colName, signal.Token);
        collections.Add(new(collection.Id, collection.Name));
    }

    WriteLine("Save test entities info");
    var testEntities = new TestEntities(
        new TestOrganization(
            Id: orgProfile.id,
            ClientId: $"organization.{orgProfile.id}",
            ClientSecret: orgApiKey.apiKey
        ),
        collections.ToArray(),
        new TestConfirmer(
            Id: userProfile.id,
            ClientId: $"user.{userProfile.id}",
            ClientSecret: userApiKey.apiKey
        )
    );
    await vwSettings.TestEntitiesFile.WriteJsonAsync(testEntities, new JsonSerializerOptions { WriteIndented = true, });
});
