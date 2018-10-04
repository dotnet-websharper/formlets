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

/// Represent a form
[<JavaScript>]
type Form<'Body,'State> =
    {
        Body    : IObservable<Tree.Edit<'Body>>
        Dispose : unit -> unit
        Notify  : obj -> unit
        State   : IObservable<Result<'State>>
    }
        interface IDisposable with
            [<JavaScript>]
            member this.Dispose() = this.Dispose()
