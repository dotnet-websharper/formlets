#load "tools/includes.fsx"
open IntelliFactory.Build

let bt =
    BuildTool().PackageId("Zafir.Formlets")
        .VersionFrom("Zafir")
        .WithFSharpVersion(FSharpVersion.FSharp30)
        .WithFramework(fun fw -> fw.Net40)

let if_formlets =
    bt.Zafir.Library("IntelliFactory.Formlets")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.NuGet("Zafir.Reactive").Latest(true).ForceFoundVersion().Reference()
            ])

let ws_formlets =
    bt.Zafir.Library("WebSharper.Formlets")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.NuGet("Zafir.Reactive").Latest(true).ForceFoundVersion().Reference()
                r.NuGet("Zafir.Html").Latest(true).ForceFoundVersion().Reference()
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
    bt.Zafir.Library("WebSharper.Formlets.Tests")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.NuGet("Zafir.Reactive").Latest(true).ForceFoundVersion().Reference()
                r.NuGet("Zafir.Html").Latest(true).ForceFoundVersion().Reference()
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
                Title = Some "Zafir.Formlets"
                LicenseUrl = Some "http://websharper.com/licensing"
                ProjectUrl = Some "https://github.com/intellifactory/websharper.formlets"
                Description = "Zafir Formlets"
                RequiresLicenseAcceptance = true })
        .Add(if_formlets)
        .Add(ws_formlets)
]
|> bt.Dispatch
