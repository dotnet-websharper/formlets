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

// Interface for basic validation functionality.
[<JavaScript>]
type IValidatorProvider =
    // abstract Validate<'T, 'F when 'F :> IFormlet<'B,'T>> : ('T -> bool) ->  string -> 'F -> 'F
    [<Name "Matches">]
    abstract Matches : string -> string -> bool

[<JavaScript>]
type Validator [<JavaScript>](VP: IValidatorProvider) =

    [<JavaScript>]
    member private this.Validate<'B, 'T, 'F when 'F :> IFormlet<'B,'T>> (f: 'T -> bool) (msg : string) (flet : 'F) : 'F =
        (flet :> IFormlet<'B,'T>).MapResult (fun res ->
            match res with
            | Success v ->
                if f v then
                    Success v
                else
                    Failure [msg]
            | Failure fs -> Failure fs)
        |> unbox

    /// Validator for preventing empty values.
    [<JavaScript>]
    member this.Is f m flet =
        this.Validate f m flet

    /// Validator for preventing empty values.
    [<JavaScript>]
    member this.IsNotEmpty<'B, 'F when 'F :> IFormlet<'B,string>> (msg : string) flet =
        this.Validate<'B, string,'F>  (fun s -> s <> "")  msg  flet

    /// Validates a formlet against a regex, with a given error message on failure.
    [<JavaScript>]
    member this.IsRegexMatch regex msg  flet =
        this.Validate (fun x -> VP.Matches regex x) msg flet

    /// TODO: Check for consistency!
    /// Only accept email addresses.
    [<JavaScript>]
    member this.IsEmail msg =
        let regex =
            "^[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+\
            (?:\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*\
            @(?:[A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?\.)+\
            [A-Za-z0-9](?:[A-Za-z0-9-]*[A-Za-z0-9])?$"
        this.IsRegexMatch regex msg

    /// Only accept integer input.
    [<JavaScript>]
    member this.IsInt msg =
        this.IsRegexMatch "^-?\d+$" msg

    /// Only accept float input.
    [<JavaScript>]
    member this.IsFloat  msg =
        let patt = "^\s*(\+|-)?((\d+(\.\d+)?)|(\.\d+))\s*$"
        this.IsRegexMatch patt msg

    /// Only accept "true" values.
    [<JavaScript>]
    member this.IsTrue (msg : string) flet  =
        this.Validate id msg flet

    /// Only accept values greater than the given value.
    [<JavaScript>]
    member this.IsGreaterThan min (msg: string) flet =
        this.Validate (fun i -> i > min)  msg  flet

    /// Only accept values less than than the given value.
    [<JavaScript>]
    member this.IsLessThan max (msg: string) flet =
        this.Validate (fun i -> i < max)  msg flet

    /// Only accept values equal to the given value.
    [<JavaScript>]
    member this.IsEqual value (msg: string) flet =
        this.Validate (fun i -> i = value) msg flet

    /// Only accept values not equal to the the given value.
    [<JavaScript>]
    member this.IsNotEqual value (msg: string) flet =
        this.Validate (fun i -> i <> value)  msg flet

