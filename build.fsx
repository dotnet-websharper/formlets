#load "tools/includes.fsx"
open IntelliFactory.Build

let bt =
    BuildTool().PackageId("WebSharper.Formlets", "3.0")
    |> fun bt -> bt.WithFramework(bt.Framework.Net40)

let if_formlets =
    bt.WebSharper.Library("IntelliFactory.Formlets")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.NuGet("IntelliFactory.Reactive").Reference()
            ])

let ws_formlets =
    bt.WebSharper.Library("WebSharper.Formlets")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.NuGet("IntelliFactory.Reactive").Reference()
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
    bt.WebSharper.Library("WebSharper.Formlets.Tests")
        .SourcesFromProject()
        .References(fun r ->
            [
                r.NuGet("IntelliFactory.Reactive").Reference()
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
