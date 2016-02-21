[<AutoOpen>]
module BReusable 

open System

// F# 3 doesn't have isNull, so this is probably the bad slow one
#if FSharp4
#else
let inline isNull value = match value with |null -> true | _ -> false
#endif

let (|NullString|EmptyString|Whitespace|ValueString|) x = 
    if isNull x then NullString
    elif String.IsNullOrEmpty x then EmptyString
    elif String.IsNullOrWhiteSpace x then Whitespace
    else ValueString

[<RequireQualifiedAccess>]
module Seq = 
    let Any items = items |> Seq.exists(fun _ -> true)
    let FirstOrDefault items = if items |> Any then items |> Seq.head |> Some else None

type System.String with
    static member contains (delimiter:string) (x:string) : bool = x.Contains delimiter

let (|HasEnvVars|_|) x =
                if String.contains "%" x then Environment.ExpandEnvironmentVariables x |> Some
                else None
type IOPath = 
    private{ Path:string}
[<RequireQualifiedAccess; CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module IOPath =
    open System.IO
    let private invalidPathChars = Path.GetInvalidPathChars() |> Set.ofSeq
    let private hasInvalidChars = Set.ofSeq >> (Set.intersect invalidPathChars) >> Seq.Any
    let getPath (ioPath:IOPath) = 
        ioPath.Path
    let tryMakeIOPath path = 
        if hasInvalidChars path then
            None
        else
            match path with
                | HasEnvVars x -> x
                | x -> x
            |> (fun path -> if Directory.Exists path || File.Exists path then Some {IOPath.Path = path} else None)

module Files =
    open System.IO
    open System.Runtime.CompilerServices

    type FileRef = private { Path:string }
    [<RequireQualifiedAccess; CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
    module FileRef = 
        let tryMakeFile path = 
            let (|FileExists|_|) = function | x when File.Exists x -> Some () | _ -> None
            match path with
            | HasEnvVars expanded when File.Exists expanded -> 
                {Path = expanded } |> Some
            | FileExists -> {Path = path} |> Some
            | _ -> None
        let getPath p = p.Path

    type FindFileResult =
        |Found of FileRef
        |NotFoundIn of string seq

    [<Extension>] // c# helpers
    module FindRefExtensions =
        [<Extension>]
        let WithFoundValue ff (fIfFound:Action<_>) (fIfNotFound:Action<_>) =
            match ff with
            |Found fr -> fIfFound.Invoke fr
            | NotFoundIn searchPaths -> fIfNotFound.Invoke searchPaths
        [<Extension>]
        let GetPath (f:FileRef)= f.Path
