#!/usr/bin/env dotnet-script
#r "nuget: VwConnector, 1.34.1-rev.6"
#r "nuget: Lestaly.General, 0.100.0"
#r "nuget: Kokuban, 0.2.0"
#load ".ldap-settings.csx"
#load ".vw-settings.csx"
#nullable enable
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
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

    WriteLine("Confirm org members");
    var userInfo = vwSettings.Setup.TestUser;
    using var agent = await VaultwardenAgent.CreateAsync(new Uri(vwSettings.Service.Url), new(userInfo.Mail, userInfo.Password), signal.Token);
    var orgId = testEntities.Organization.Id;
    var orgMembers = await agent.Connector.Organization.GetMembersAsync(agent.Token, orgId, new(true, true), signal.Token);
    foreach (var member in orgMembers.data)
    {
        WriteLine($"{member.email}: {member.id}");
        if (member.status != MembershipStatus.Accepted)
        {
            WriteLine($".. Skip: Not target");
            continue;
        }

        await agent.Affect.ConfirmMemberAsync(orgId, new(member.id, member.userId), signal.Token);
        WriteLine($"..  Confirmed");
    }

});
