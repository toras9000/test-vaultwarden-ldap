#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Playwright, 1.52.0"
#r "nuget: Lestaly, 0.82.0"
#r "nuget: Kokuban, 0.2.0"
#load ".ldap-settings.csx"
#load ".vw-settings.csx"
#load ".vw-helper.csx"
#nullable enable
using Microsoft.Playwright;
using Kokuban;
using Lestaly;
using Lestaly.Cx;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    using var signal = new SignalCancellationPeriod();

    WriteLine("Load test entities info");
    var testEntities = await vwSettings.TestEntitiesFile.ReadJsonAsync<TestEntities>();
    if (testEntities == null) throw new PavedMessageException("Cannot load entities info");

    WriteLine("Get invite members");
    using var helper = new VaultwardenHelper(new(vwSettings.Service.Url));
    var userCredential = new ClientCredentialsConnectTokenModel(
        scope: "api",
        client_id: testEntities.Confirmer.ClientId,
        client_secret: testEntities.Confirmer.ClientSecret,
        device_type: ClientDeviceType.OperaBrowser,
        device_name: Environment.MachineName,
        device_identifier: Environment.MachineName
    );
    var userToken = await helper.ConnectTokenAsync(userCredential, signal.Token);
    var orgMembers = await helper.GetOrgMembers(userToken, testEntities.Organization.Id, new(true, true), signal.Token);

    WriteLine("Detection invite mail");
    var mailDir = ThisSource.RelativeDirectory("maildump");
    var joinUrls = mailDir.GetFiles("*-text.txt").OrderByDescending(f => f.Name)
        .Select(file => file.ReadAllLines().FirstOrDefault(l => l.StartsWith("Click here to join:"))?.SkipFirstToken(':').Trim().ToString())
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

    WriteLine("Prepare playwright");
    var packageVer = typeof(Microsoft.Playwright.Program).Assembly.GetName()?.Version?.ToString(3) ?? "*";
    var packageDir = SpecialFolder.UserProfile().FindPathDirectory([".nuget", "packages", "Microsoft.Playwright", packageVer], MatchCasing.CaseInsensitive);
    Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", packageDir?.FullName);
    Microsoft.Playwright.Program.Main(["install", "chromium", "--with-deps"]);

    WriteLine("Register invite users");
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, });
    foreach (var invite in userInvites)
    {
        WriteLine($"  User: {invite.member.email}");
        var password = $"{invite.member.email.TakeFirstToken('@')}-password";
        var page = await browser.NewPageAsync();
        var response = await page.GotoAsync(invite.joinUrl!);
        var registerForm = page.Locator("app-register-form");
        await registerForm.Locator("input[id='register-form_input_name']").FillAsync(invite.member.email);
        await registerForm.Locator("input[id='register-form_input_master-password']").FillAsync(password);
        await registerForm.Locator("input[id='register-form_input_confirm-master-password']").FillAsync(password);
        await registerForm.Locator("button[type='submit']").ClickAsync();
        await page.Locator("app-login input[type='email']").FillAsync(invite.member.email);
        await page.Locator("app-login button[type='submit']").Filter(new() { Visible = true }).ClickAsync();
        await page.Locator("app-login input[type='password']").FillAsync(password);
        await page.Locator("app-login button[type='submit']").Filter(new() { Visible = true }).ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
});
