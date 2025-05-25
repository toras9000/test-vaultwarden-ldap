#!/usr/bin/env dotnet-script
#r "nuget: Microsoft.Playwright, 1.52.0"
#r "nuget: Lestaly, 0.82.0"
#r "nuget: Kokuban, 0.2.0"
#load ".vw-settings.csx"
#load ".vw-helper.csx"
#nullable enable
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using Microsoft.Playwright;
using Kokuban;
using Lestaly;
using Lestaly.Cx;
using DocumentFormat.OpenXml.Spreadsheet;

return await Paved.ProceedAsync(noPause: Args.RoughContains("--no-pause"), async () =>
{
    using var signal = new SignalCancellationPeriod();

    WriteLine("Load test entities info");
    var testEntities = await vwSettings.TestEntitiesFile.ReadJsonAsync<TestEntities>();
    if (testEntities == null) throw new PavedMessageException("Cannot load entities info");

    WriteLine("Prepare playwright");
    var packageVer = typeof(Microsoft.Playwright.Program).Assembly.GetName()?.Version?.ToString(3) ?? "*";
    var packageDir = SpecialFolder.UserProfile().FindPathDirectory([".nuget", "packages", "Microsoft.Playwright", packageVer], MatchCasing.CaseInsensitive);
    Environment.SetEnvironmentVariable("PLAYWRIGHT_DRIVER_SEARCH_PATH", packageDir?.FullName);
    Microsoft.Playwright.Program.Main(["install", "chromium", "--with-deps"]);

    WriteLine("Login owner user");
    using var playwright = await Playwright.CreateAsync();
    await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true, });
    var page = await browser.NewPageAsync();
    {
        var userInfo = vwSettings.Setup.TestUser;
        var response = await page.GotoAsync(vwSettings.Service.Url) ?? throw new PavedMessageException("Cannot access register page");
        await page.Locator("app-login input[type='email']").FillAsync(userInfo.Mail);
        await page.Locator("app-login button[type='submit']").Filter(new() { Visible = true }).ClickAsync();
        await page.Locator("app-login input[type='password']").FillAsync(userInfo.Password);
        await page.Locator("app-login button[type='submit']").Filter(new() { Visible = true }).ClickAsync();
    }

    WriteLine("Confirm users");
    {
        await page.GotoAsync($"http://localhost:8180/#/organizations/{testEntities.Organization.Id}/members");
        await page.Locator("main bit-toggle-group bit-toggle:nth-of-type(3)").ClickAsync();
        await page.Locator("#selectAll").CheckAsync(); ;
        await page.Locator("main bit-table table thead tr button[biticonbutton='bwi-ellipsis-v']").ClickAsync();
        await page.Locator("#cdk-overlay-0.bit-menu-panel button:nth-of-type(1)").ClickAsync(); ;
        await page.Locator("#cdk-dialog-0 footer button:nth-of-type(1)").ClickAsync(); ;
    }

});
