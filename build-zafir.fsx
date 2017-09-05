#load "tools/includes.fsx"
open IntelliFactory.Build

let bt =
    BuildTool().PackageId("WebSharper.Formlets")
        .VersionFrom("WebSharper")
        .WithFSharpVersion(FSharpVersion.FSharp30)
        .WithFramework(fun fw -> fw.Net40)

let if_formlets =
    bt.WebSharper4.Library("IntelliFactory.Formlets")
        .SourcesFromProject()
        .WithSourceMap()
        .References(fun r ->
            [
                r.NuGet("WebSharper.Reactive").Latest(true).ForceFoundVersion().Reference()
            ])

let ws_formlets =
    bt.WebSharper4.Library("WebSharper.Formlets")
        .SourcesFromProject()
        .WithSourceMap()
        .References(fun r ->
            [
                r.NuGet("WebSharper.Reactive").Latest(true).ForceFoundVersion().Reference()
                r.NuGet("WebSharper.Html").Latest(true).ForceFoundVersion().Reference()
                r.Project if_formlets
            ])
        .Embed(
            [
                "styles/Formlet.css"
                "images/ActionAdd.png"
                "images/ActionCheck.png"
                "images/ActionDelete.png"
                "images/ErrorIcon.png"
                "images/InfoIcon.png"
            ])

let tests =
    bt.WebSharper4.Library("WebSharper.Formlets.Tests")
        .SourcesFromProject()
        .WithSourceMap()
        .References(fun r ->
            [
                r.NuGet("WebSharper.Reactive").Latest(true).ForceFoundVersion().Reference()
                r.NuGet("WebSharper.Html").Latest(true).ForceFoundVersion().Reference()
                r.Project if_formlets
                r.Project ws_formlets
            ])

bt.Solution [
    if_formlets
    ws_formlets
    tests

    bt.NuGet.CreatePackage()
        .Configure(fun c ->
            { c with
                Title = Some "WebSharper.Formlets"
                LicenseUrl = Some "http://websharper.com/licensing"
                ProjectUrl = Some "https://github.com/intellifactory/websharper.formlets"
                Description = "WebSharper Formlets"
                RequiresLicenseAcceptance = true })
        .Add(if_formlets)
        .Add(ws_formlets)
]
|> bt.Dispatch
