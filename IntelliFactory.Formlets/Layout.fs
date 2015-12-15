// $begin{copyright}
//
// This file is part of WebSharper
//
// Copyright (c) 2008-2014 IntelliFactory
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

type Layout<'B> =
    {
        Apply : IObservable<Tree.Edit<'B>> ->
                option<'B * IDisposable>
    }

type Container<'Body> =
    {
        Body : 'Body
        SyncRoot : obj
        Insert : int -> 'Body -> unit
        Remove : seq<'Body> -> unit
    }

[<JavaScript>]
type internal D() =
    interface IDisposable with
        member this.Dispose () = ()

type Reactive =
    {
        Reactive : IReactive
    }

/// Defines layout managers and their associated operations.
/// TODO: make private (error with WS???)
[<JavaScript>]
type LayoutUtils (R: Reactive) =

//    [<JavaScript>]
//    static member New(r: IReactive) =
//        new LayoutUtils({Reactive = r})

    member this.Default<'B>() : Layout<'B> =
        { Apply = fun _ -> None }

    member this.Delay (f: unit -> Layout<'Body>) =
        { Apply = fun x -> f().Apply x }

    member this.New (container: unit -> Container<'Body>) : Layout<'Body> =
        {
            Apply = fun event ->
                let panel = container ()
                let tree = ref Tree.Empty
                let disp =
                    event.Subscribe ( fun edit ->
                        // Get and delete sub tree to be removed
                        let deletedTree = Tree.ReplacedTree edit !tree
                        tree := Tree.Apply edit !tree
                        let (off, _) = Tree.Range edit !tree
                        panel.Remove deletedTree.Sequence
                        // Insert new nodes
                        edit
                        |> Seq.iteri (fun i e ->
                            panel.Insert (off + i) e
                        )
                    )
                Some (panel.Body, disp)
        }
