namespace MinecraftManager.Lib

open System
open System.Diagnostics
open System.ComponentModel
open System.IO
open System.Runtime.CompilerServices
//module ProcessCommands = 
//
//    type CommandPathType = 
//        |RelativeCmdPath
//        |AbsoluteCmdPath
//        |ShellLocateCmdName
//    type CommandRefCreateArgs = 
//        | RelativeCmd of relativePath:string
//        | AbsoluteCmd of rootedPath:string
//        | ShellLocateCmd of filename:string*verificationArgsOpt:string option
//    type CommandRef = 
//        private {CmdPath:string; CommandPathType:CommandPathType}
//
//    [<RequireQualifiedAccess; CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
//    module CommandRef =
//        let private verifyProcessCanStart (filename, argsOpt) = 
//            try
//                let _output = 
//                //expect failing items to throw exception for file not found or win32 exception
//                //consider calling "where.exe" for paths that are relative
//                    match argsOpt with 
//                    | Some argsOpt -> Process.Start(filename,argsOpt)
//                    |None -> Process.Start(filename)
//
//                    |> fun p ->  p.WaitForExit(); p.StandardOutput.ReadToEnd()
//                Some filename
//            with :? Win32Exception as ex -> 
//                None
//
//        // expect it to be a relative path, absolute path,  
//        // or able to return some text on an attempt to run with no args (something under %PATH%)
//        let tryMakeCmdRef =
//            function
//                | RelativeCmd relativePath -> relativePath,CommandPathType.RelativeCmdPath, File.Exists relativePath
//                | AbsoluteCmd rootedPath -> rootedPath, CommandPathType.AbsoluteCmdPath, Path.IsPathRooted rootedPath && File.Exists rootedPath
//                //| ShellLocateCmd (filename, None) -> filename, Process.Start()
//                | ShellLocateCmd (filename,argsOpt) -> 
//                    let canStart = 
//                        verifyProcessCanStart (filename,argsOpt) 
//                        |> function |Some _ -> true | None -> false
//                    (filename,CommandPathType.ShellLocateCmdName, canStart)
//            >> function
//                    | path, t, true ->  Some {CmdPath=path; CommandPathType = t}
//                    | _ -> None

type FileRef = private { Path:string }
[<RequireQualifiedAccess; CompilationRepresentation (CompilationRepresentationFlags.ModuleSuffix)>]
module FileRef = 
    let tryMakeFile path = 
        match File.Exists path with
        | true -> {Path = path} |> Some
        | false -> None

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

module CrossCutting = 
    module Logging = 
        let log (category:string) (msg:string) = Debug.WriteLine (msg, category)

module JavaExe =
    module InPathBehavior =
        let tryLocate logger = 
            let java= "java.exe"
            try
                let p = Process.Start(java + " -version")
                p.WaitForExit()
                let _output = p.StandardOutput.ReadToEnd()
                Some java
            with :? Win32Exception as ex -> 
                logger "JavaInPathBehavior" ex.Message
                None
        let TryLocate (logger:Action<_,_>) = tryLocate (fun c s -> logger.Invoke(c,s))
    module PropertyBehavior = 
        let tryLocate fGetter useUserInputF = 
            let checkIsValidPath p = not <| String.IsNullOrEmpty p && File.Exists p
            let java = 
                let java = fGetter()
                if checkIsValidPath java then
                    java
                else
                    useUserInputF()
            // verify/re-verify
            if checkIsValidPath java then
                Some java
            else 
                None
        let TryLocate (fGetter:Func<_>) (useUserInputF:Func<_>) =
            tryLocate fGetter.Invoke useUserInputF.Invoke

module Logs = 
    let findServerLogOpt serverPath = 
        let logsDir = Path.Combine(serverPath,"logs")
        let latestLog = Path.Combine(logsDir,"latest.log")
        let olderVersionLogPath = Path.Combine(serverPath,"server.log")
        let toSearch = [latestLog;olderVersionLogPath]
        toSearch
        |> Seq.choose( FileRef.tryMakeFile)
        |> Seq.tryFind (fun _ -> true)
        |> function 
            | Some fp -> Found fp
            | None -> NotFoundIn toSearch


module MineCraftAs = // translated from http://www.minecraftforum.net/forums/support/unmodified-minecraft-client/tutorials-and-faqs/1871678-how-to-use-custom-jars-in-the-new-launcher?comment=8
    // requires server (server.properties file) be set to online-mode = false
    let createProfile versionsPath version newName = 
        let targetVersionPath =Path.Combine(versionsPath, version + newName)
        let sourceVersionPath = Path.Combine(versionsPath, version)

        // copy .minecraft/versions/version
        File.Copy(sourceVersionPath,targetVersionPath)

        let sourceFilename,sourceFilePath = 
            let filename = version + ".json"
            let path = Path.Combine(sourceVersionPath, filename)

            if not <| File.Exists path then
                failwithf "Could not find version/profile settings at %A" sourceVersionPath

            filename,path 

        let targetFilename = version + newName + ".json" 

        let targetJsonFile = Path.Combine(targetVersionPath ,targetFilename)
        // is this necessary what about the inherit approach used by forge?
        // what if the name doesn't change, and that means it doesn't redownload the jar?
        File.Move(Path.Combine(targetVersionPath,sourceFilename), targetJsonFile)

        // edit .minecraft/versions/newversion
        File.ReadAllText(targetJsonFile).
            // replace the argument to --username with desired name
            Replace("${auth_player_name}",newName).
            // set id: to new profile name (sprintf "%s%s" version name
            Replace(sprintf """id": "%s",""" version,sprintf  """id": "%s",""" newName)
        |> fun text -> File.WriteAllText(targetJsonFile, text)