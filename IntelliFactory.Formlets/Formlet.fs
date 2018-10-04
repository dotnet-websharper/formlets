// $begin{copyright}
//
// This file is part of WebSharper
//
// Copyright (c) 2008-2018 IntelliFactory
//
// Licensed under the Apache License, Version 2.0 (the "License"); you
// may not use this file except in compliance with the License.  You may
// obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or
// implied.  See the License for the specific language governing
// permissions and limitations under the License.
//
// $end{copyright}

namespace IntelliFactory.Formlets.Base

open WebSharper
open System
open IntelliFactory.Reactive

[<JavaScript>]
type IFormlet<'B, 'T> =
    [<Name "LayoutI">]
    abstract member Layout : Layout<'B>
    [<Name "BuildI">]
    abstract member Build : unit -> Form<'B, 'T>
    // The type of the returned value should
    // be equal to the type of the object itself.
    [<Name "MapResultI">]
    abstract member MapResult<'U> : (Result<'T> -> Result<'U>) -> IFormlet<'B,'T>

// Specify IReactive implementation and default layout.
type Utils<'B> =
    {
        Reactive : IReactive
        DefaultLayout : Layout<'B>
    }

// Generic formlet type.
[<JavaScript>]
type private Formlet<'B,'T> =
    {
        /// The layout manager.
        Layout : Layout<'B>
        /// Builds the form without applying the layout.
        Build : unit -> Form<'B,'T>
        Utils : Utils<'B>
    }
    interface IFormlet<'B,'T> with
        member this.Layout = this.Layout
        member this.Build () = this.Build ()
        member this.MapResult (f : Result<'T> -> Result<'U>) =
            {
                Layout = this.Layout
                Build = fun () ->
                    let form = this.Build()
                    let state = this.Utils.Reactive.Select form.State (fun x -> f x)
                    let state = form.State
                    {
                        Body = form.Body
                        Dispose = form.Dispose
                        Notify = form.Notify
                        State =  state
                    }
                Utils = this.Utils
            } :> IFormlet<'B,'T>


/// Defines formlets and their operations.
[<JavaScript>]
type FormletProvider<'B> (U: Utils<'B>) =

    let L = new LayoutUtils({Reactive = U.Reactive})

    /// Builds the form and applies the layout.
    member this.BuildForm(formlet: IFormlet<'B,'T>) =
        let form = formlet.Build()
        match formlet.Layout.Apply form.Body with
        | None ->
            form
        | Some (body, d) ->
            { form with
                Body = U.Reactive.Return (Tree.Set body)
                Dispose = fun () -> form.Dispose(); d.Dispose()
            }

    /// Creates a new formlet with the default layout.
    member this.New build =
        { Build = build; Layout = L.Default<'B>(); Utils = U } :> IFormlet<_,_>

    member this.FromState<'T> (state: IObservable<Result<'T>>) : IFormlet<'B,'T> =
        this.New <| fun () ->
            {
                Body = U.Reactive.Never ()
                Dispose = ignore
                Notify = ignore
                State = state
            }

    /// Specifies the layout.
    member this.WithLayout layout (formlet : IFormlet<'B,'T>) : IFormlet<'B, 'T> =
        {
            Layout = layout
            Build = formlet.Build
            Utils = U
        }
        :> IFormlet<_,_>

    member this.InitWith (value: 'T) (formlet: IFormlet<'B,'T>) : IFormlet<'B,'T> =
        this.New <| fun () ->
            let form = formlet.Build ()
            let state =
                U.Reactive.Concat (U.Reactive.Return(Success value)) form.State
            {form with State = state}
        |> this.WithLayout formlet.Layout

    member this.ReplaceFirstWithFailure (formlet: IFormlet<'B,'T>) : IFormlet<'B,'T> =
        this.New <| fun () ->
            let form = formlet.Build ()
            let state =
                let state = U.Reactive.Drop form.State 1
                U.Reactive.Concat (U.Reactive.Return(Failure [])) state
            {form with State = state}

        |> this.WithLayout formlet.Layout

    member this.InitWithFailure (formlet: IFormlet<'B,'T>) : IFormlet<'B,'T> =
        this.New <| fun () ->
            let form = formlet.Build ()
            let state =
                U.Reactive.Concat (U.Reactive.Return(Failure [])) form.State
            {form with State = state}

        |> this.WithLayout formlet.Layout

    /// Maps the body.
    member this.ApplyLayout (formlet: IFormlet<'B,'T>) : IFormlet<'B,'T> =
        this.New <| fun () ->
            let form = formlet.Build ()
            let body =
                match formlet.Layout.Apply form.Body with
                | Some (body, disp) ->
                    U.Reactive.Return (Tree.Set body)
                | None              ->
                    form.Body
            {form with Body = body}


    member this.AppendLayout layout (formlet: IFormlet<'B,'T>) : IFormlet<'B,'T> =
        this.ApplyLayout formlet
        |> this.WithLayout layout

    /// Maps the body.
    member this.MapBody (f : 'B -> 'B) (formlet: IFormlet<'B,'T>) : IFormlet<'B,'T> =
        let layout =
            {
                Apply  =  fun o ->
                    match formlet.Layout.Apply(o) with
                    | Some (body, d) -> Some (f body, d)
                    | None ->
                        match U.DefaultLayout.Apply(o) with
                        | Some (body, d)    ->  Some (f body, d)
                        | None              -> None
            }
        this.WithLayout layout formlet

    member this.WithLayoutOrDefault (formlet: IFormlet<'B,'T>) : IFormlet<'B,'T> =
        this.MapBody id formlet

    /// Maps the result.
    member this.MapResult<'T,'U> (f: Result<'T> -> Result<'U>)
                            (formlet: IFormlet<'B,'T>) : IFormlet<'B,'U>  =
        {
            Build = fun () ->
                let form = formlet.Build()
                let state = U.Reactive.Select form.State (fun x -> f x)
                {
                    Body = form.Body
                    Dispose = form.Dispose
                    Notify = form.Notify
                    State =  state
                }
            Layout = formlet.Layout
            Utils = U
        } :> IFormlet<_,_>

    /// Maps the value.
    member this.Map (f: 'T -> 'U) (formlet: IFormlet<'B, 'T>) : IFormlet<'B, 'U> =
        this.MapResult (Result<_>.Map f) formlet

    /// Applicative style of application for formlets.
    member this.Apply (f: IFormlet<'B, 'T -> 'U>) (x: IFormlet<'B,'T>) : IFormlet<'B,'U> =
        this.New <| fun () ->
            let f = this.BuildForm(f)
            let x = this.BuildForm(x)
            let body =
                let left = U.Reactive.Select f.Body Tree.Left
                let right = U.Reactive.Select x.Body Tree.Right
                U.Reactive.Merge left right
            let state =
                U.Reactive.CombineLatest x.State f.State ( fun r f ->
                    Result<_>.Apply f r
                )
            {
                Body    = body
                Dispose = fun () -> x.Dispose(); f.Dispose()
                Notify  = fun o -> x.Notify o; f.Notify o
                State   = state
            }

    member this.Return<'T> (x: 'T) : IFormlet<'B,'T>  =
        this.New <| fun () ->
            {
                Body    = U.Reactive.Never()
                Dispose = id
                Notify  = ignore
                State   = U.Reactive.Return (Success x)
            }


    member this.Fail<'T> fs : Form<'B, 'T> =
        {
            Body    = U.Reactive.Never ()
            Dispose = id
            Notify  = ignore
            State   = U.Reactive.Return (Failure fs)
        }

    member this.FailWith<'T> fs : IFormlet<'B,'T> =
        this.New <| fun () -> this.Fail<'T> fs

    member this.ReturnEmpty (x: 'T) =
        this.New <| fun () ->
            {
                Body    = U.Reactive.Return (Tree.Delete<'B> ())
                Dispose = id
                Notify  = ignore
                State   = U.Reactive.Return (Success x)
            }

    member this.Never<'T> () : IFormlet<'B,'T> =
        this.New <| fun () ->
            {
                Body    = U.Reactive.Never()
                Dispose = ignore
                Notify  = ignore
                State   = U.Reactive.Never()
            }

    member this.Empty<'T> () : IFormlet<'B,'T> =
        this.New <| fun () ->
            {
                Body    = U.Reactive.Return (Tree.Delete<'B> ())
                Dispose = ignore
                Notify  = ignore
                State   = U.Reactive.Never()
            }


    /// Constructs an empty form.
    member private this.EmptyForm () : Form<'B, 'T> =
        {
            Body    = U.Reactive.Never()
            Dispose = ignore
            Notify  = ignore
            State   = U.Reactive.Never ()
        }

    member this.Join (formlet: IFormlet<'B, IFormlet<'B, 'T>>) : IFormlet<'B, 'T> =
        this.New <| fun () ->
            let form1 = this.BuildForm(formlet)
            let formStream =
                U.Reactive.Select form1.State (fun res ->
                    match res with
                    | Success innerF ->
                        this.BuildForm innerF
                    | Failure fs ->
                        this.Fail fs
                )
                |> U.Reactive.Heat

            let body =
                let right =
                    let value =
                        U.Reactive.Select formStream (fun f ->
                            let delete = U.Reactive.Return (Tree.Delete())
                            U.Reactive.Concat delete f.Body
                        )
                    U.Reactive.Select (U.Reactive.Switch(value)) Tree.Right
                U.Reactive.Merge (U.Reactive.Select (form1.Body) Tree.Left) right

            let state =
                U.Reactive.Switch (U.Reactive.Select formStream (fun f -> f.State))
            let notify o =
                form1.Notify o

            let dispose () =
                form1.Dispose()

            { Body = body; Notify = notify; Dispose = dispose; State = state}

    member this.Switch (formlet: IFormlet<'B, IFormlet<'B, 'T>>) : IFormlet<'B, 'T> =
        this.New <| fun () ->
            let formlet =
                this.WithLayoutOrDefault formlet
                |> this.ApplyLayout

            let form1 = this.BuildForm(formlet)
            let formStream =
                U.Reactive.Choose form1.State (fun res ->
                    match res with
                    | Success innerF ->
                        Some <| this.BuildForm innerF
                    | Failure fs ->
                        None
                )
                |> U.Reactive.Heat

            let body =
                U.Reactive.Concat form1.Body (
                    U.Reactive.Switch (
                        U.Reactive.Select formStream (fun f ->
                            f.Body
                        )
                    )
                 )

            let state =
                U.Reactive.Switch (U.Reactive.Select formStream (fun f -> f.State))

            let notify o =
                form1.Notify o

            let dispose () =
                form1.Dispose()

            { Body = body; Notify = notify; Dispose = dispose; State = state}


    /// Flips the stream of body edit operations so that each left
    /// branch becomes a right branch and vice versa.
    member this.FlipBody (formlet: IFormlet<'B,'T>) : IFormlet<'B,'T> =
        this.New <| fun () ->
            let form = formlet.Build()
            let body =
                U.Reactive.Select form.Body Tree.FlipEdit
            {form with Body = body}
        |> this.WithLayout formlet.Layout

    /// Collects all values from nested formlets.
    member this.SelectMany (formlet: IFormlet<'B, IFormlet<'B, 'T>>) : IFormlet<'B,list<'T>> =
        this.New <| fun () ->
            let form1 = this.BuildForm formlet
            let formStream =
                U.Reactive.Choose form1.State (fun res ->
                    match res with
                    | Success innerF ->
                        Some <| this.BuildForm innerF
                    | Failure fs ->
                        None
                )
                |> U.Reactive.Heat

            let body =
                let left =
                    U.Reactive.Select form1.Body Tree.Edit.Left
                let right =
                    let tag = ref Tree.Edit.Left
                    let incrTag () =
                        let f = tag.Value
                        tag := Tree.Edit.Right << f
                    let allBodies =
                        U.Reactive.Select formStream (fun f ->
                            incrTag ()
                            let tagLocal = tag.Value
                            U.Reactive.Select f.Body tagLocal
                        )
                    U.Reactive.SelectMany allBodies
                U.Reactive.Merge left right

            let state =
                let stateStream = U.Reactive.Select formStream (fun f -> f.State)
                U.Reactive.Select (U.Reactive.CollectLatest stateStream) Result<_>.Sequence

            let notify o =
                form1.Notify o

            let dispose () =
                form1.Dispose()

            { Body = body; Notify = notify; Dispose = dispose; State = state}


    /// Constructs a formlet with a handler to it's notification channel.
    member this.WithNotificationChannel<'T> (formlet: IFormlet<'B,'T>)
                                : IFormlet<'B,'T * (obj -> unit)>  =
        this.New <| fun () ->
            let form = formlet.Build ()
            let state =
                U.Reactive.Select form.State (Result<_>.Map(fun v -> (v, form.Notify)))
            {
                Body    = form.Body
                Notify  = form.Notify
                Dispose = form.Dispose
                State   = state
            }
        |> this.WithLayout formlet.Layout


    member this.Replace (formlet: IFormlet<'B,'T1>) (f: 'T1 -> IFormlet<'B,'T2>) : IFormlet<'B, 'T2> =
        // TODO: Think!
        formlet
        |> this.Map (fun value ->
            f value
        )
        |> this.Switch

    member this.Deletable(formlet: IFormlet<'B,option<'T>>) : IFormlet<'B, option<'T>> =
        this.Replace formlet (fun value ->
            match value with
            | None ->
                this.ReturnEmpty None
            | Some value ->
                this.Return (Some value)
        )

    member this.WithCancelation (formlet: IFormlet<'B,'T>) cancelFormlet : IFormlet<'B,option<'T>> =
        let compose (r1:Result<'T>) (r2: Result<unit>) : Result<option<'T>> =
            match r1, r2 with
            | _ , Success _ ->
                Success None
            | Success s, _  ->
                Success (Some s)
            | Failure fs, _ ->
                Failure fs

        let f1 = this.Return compose
        let f2 = this.LiftResult formlet
        let f3 = this.LiftResult cancelFormlet
        let f = this.Apply f1 f2
        this.Apply f f3
        |> this.MapResult Result<_>.Join

    member this.Bind<'T1, 'T2> (formlet: IFormlet<'B,'T1>) (f: 'T1 -> IFormlet<'B,'T2>) : IFormlet<'B, 'T2> =
        formlet |> this.Map f |> this.Join

    member this.Delay (f: unit -> IFormlet<'B,'T>) : IFormlet<'B,'T> =
        {
            Build = fun () -> this.BuildForm(f())
            Layout = L.Delay (fun () -> f().Layout)
            Utils = U
        } :> IFormlet<_,_>

    /// Given a sequence of formlets returns a composed formlet
    /// producing a list of values.
    member this.Sequence<'B,'T> (fs : seq<IFormlet<'B,'T>>) : IFormlet<'B,List<'T>> =
        let fs = List.ofSeq fs
        match fs with
        | []        ->
            this.Return []
        | f :: fs   ->
            let fComp  = this.Return (fun v vs -> v :: vs)
            let fRest = this.Sequence fs
            this.Apply (this.Apply fComp f) fRest

    /// Returns with lifted result type.
    member this.LiftResult<'T> (formlet: IFormlet<'B,'T>) : IFormlet<'B,Result<'T>> =
        this.MapResult Success formlet

    /// Constructs a formlet with an extra notification action.
    member this.WithNotification (notify: obj -> unit) (formlet: IFormlet<'B,'T>) : IFormlet<'B,'T>  =
        this.New <| fun () ->
            let form = this.BuildForm(formlet)
            {form with Notify = fun obj -> form.Notify obj; notify obj}
        |> this.WithLayout formlet.Layout

    member this.BindWith (hF: 'B -> 'B -> 'B)  (formlet: IFormlet<'B,'T>) (f: 'T -> IFormlet<'B,'U>) : IFormlet<'B,'U> =
        this.New <| fun () ->
            let formlet = this.Bind formlet f
            let form = formlet.Build ()
            let left =
                U.Reactive.Where form.Body (fun edit ->
                    match edit with
                    | Tree.Edit.Left _  -> true
                    | _                 -> false
                )
                |> U.DefaultLayout.Apply
            let right =
                U.Reactive.Where form.Body (fun edit ->
                    match edit with
                    | Tree.Edit.Right _ -> true
                    | _                 -> false
                )
                |> U.DefaultLayout.Apply
            let combB =
                match left, right with
                | Some (bLeft,_), Some (bRight, _)  ->
                    hF bLeft bRight
                    |> Tree.Set
                    |> U.Reactive.Return
                | _                                 ->
                    U.Reactive.Never ()
            {form with Body = combB}

[<JavaScript>]
type FormletBuilder<'B>[<JavaScript>](F: FormletProvider<'B>) =
    member this.Return x = F.Return x
    member this.Bind(x, f) = F.Bind x f
    member this.Delay f = F.Delay f
    member this.ReturnFrom f = f

