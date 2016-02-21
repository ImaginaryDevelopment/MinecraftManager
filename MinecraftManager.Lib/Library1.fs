namespace MinecraftManager.Lib

open System
open System.Diagnostics
open System.ComponentModel
open System.IO
open System.Runtime.CompilerServices

open Files


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

    let getServerLogSize serverPath = 
        match findServerLogOpt serverPath with
        | Found fp -> FileInfo(FileRef.getPath fp).Length |> Nullable
        | _ -> Nullable()
//module NodeWrapper =
//    type NodeCollection<'t>(nodes) = 
//        member x.Find()
//        
//    type Node< ^t when ^t : (member Nodes : Node< ^t > seq) > = 
//        {Node:^t} 
//            with
//                member x.Nodes : seq<Node< ^t >> = (^t : (member Nodes : seq<Node< ^t >>) (x.Node))

// treenode wrapper
type Node<'t>(t, fFindChildren, fAddChild, fAddTag, fSetTooltip) = 
    new (t:'t, findChildren:Func<_,_,_,_>, addChild:Func<'t,string,string,Node<'t>>, addTag:Action<'t,obj>, setTooltip:Action<'t,string>) =
        Node(t,findChildren.Invoke, addChild.Invoke, addTag.Invoke, setTooltip.Invoke )
    member x.Node = t
    member x.Find (key,searchAllChildren) : Node<'t> seq = fFindChildren(x.Node, key,searchAllChildren)
    member x.AddChild key text:Node<'t> = fAddChild(x.Node,key,text)
    member x.AddTag (v:obj) = fAddTag(x.Node, v)
    member x.SetTooltip text = fSetTooltip(x.Node, text)

module Worlds = 
    type WorldType = |Server |Client// single player world
        with override x.ToString() = sprintf "%A" x
    let findWorlds serverPath clientPath = 
        seq {
            for dir in Directory.GetDirectories serverPath do
                if Directory.GetFiles(dir, "level.dat") |> Seq.Any then
                    yield (Server,dir)
            match clientPath with
            | NullString |EmptyString | Whitespace -> ()
            | ValueString -> 
                
                //directory not a file
                match IOPath.tryMakeIOPath clientPath with
                | Some clientPath ->
                    let savesDir = Path.Combine(IOPath.getPath(clientPath),"saves")
                    for dir in Directory.GetDirectories savesDir do
                        if Directory.EnumerateFiles(dir, "level.dat") |> Seq.Any then
                            yield (Client,dir)
                | None -> ()
        }

    let setupWorldsUI (serverNode:Node<_>) serverPath minecraftClientPathOpt worldsDoLazy addNodeDoubleClick= 
        if String.IsNullOrEmpty serverPath || not <| Directory.Exists serverPath then
            ()
        else
            match serverNode.Find("worlds", false) |> Seq.FirstOrDefault with
            | Some worldsNode -> 
                let findOrCreate (parent:Node<_>) key text = 
                    let existingNode = parent.Find(key,false) |> Seq.FirstOrDefault
                    match existingNode with 
                    |Some x -> x
                    | None -> parent.AddChild key text

                let serverWorldsNode = findOrCreate worldsNode "server" "ServerWorlds"
                serverWorldsNode.SetTooltip ("Server dir: " + serverPath)
                let clientWorldsNode = findOrCreate worldsNode "client" "ClientWorlds"
                let worlds = findWorlds serverPath minecraftClientPathOpt |> List.ofSeq

                for (worldType,dir) in worlds do
                    let worldName = Path.GetFileName dir
                    let createWorldNode () = 
                        let worldNode = 
                            match worldType with
                                | Server -> serverWorldsNode
                                | Client -> clientWorldsNode
                            |> (fun p -> p.AddChild worldName (sprintf "%s (%A)" worldName worldType ))
                        worldNode.AddTag dir
                        worldNode.SetTooltip dir
                        if worldsDoLazy then
                            worldNode.AddChild String.Empty String.Empty |> ignore<Node<_>>
                        addNodeDoubleClick(worldNode.Node, fun () -> Process.Start dir)
                        // doubleClickActions.Add(worldNode, fun () -> Process.Start(closurePath))
                        worldNode
                    match worldsNode.Find(worldName, false) |> Seq.FirstOrDefault with
                    | Some x -> ()
                    | None -> createWorldNode() |> ignore<Node<_>>
            | None -> ()
    let SetupWorldsUI (serverNode:Node<_>) serverPath minecraftClientPathOpt worldsDoLazy (addNodeDoubleClick:Action<'t,Func<_>>) = 
        setupWorldsUI serverNode serverPath minecraftClientPathOpt worldsDoLazy addNodeDoubleClick.Invoke

module MineCraftLaunching = // translated from http://www.minecraftforum.net/forums/support/unmodified-minecraft-client/tutorials-and-faqs/1871678-how-to-use-custom-jars-in-the-new-launcher?comment=8
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

    let minecraftAs minecraftBinPath java clientMemoryArguments fOnMessage alias = 
            if String.IsNullOrEmpty minecraftBinPath || String.IsNullOrEmpty java then
                ()
            else
            let binPath = if minecraftBinPath.EndsWith(@"\") then minecraftBinPath else sprintf "%s\\" minecraftBinPath
            let nativesPath = Path.Combine(binPath, "natives")
            let java = 
                if java.Contains(" ") then
                    sprintf "\"%s\"" java
                else java

            let oldPath = Environment.CurrentDirectory
            try
                Environment.CurrentDirectory <- binPath
                // used to be binPath + "*\" "
                // if this fails try minecraft.jar;lwjgl.jar;lwjgl_util.jar from http://stackoverflow.com/a/15562373/57883
                let rawBinPath = "minecraft.jar;*"

                let arguments = sprintf "%s -cp \"%s\" -Djava.library.path=\"%s\" net.minecraft.client.Minecraft %s" clientMemoryArguments rawBinPath nativesPath alias
                try
                    let startInfo = new ProcessStartInfo(java, arguments)
                    let p = Process.Start(startInfo)
                    Debug.WriteLine(p.Id)
                    System.Threading.Thread.Sleep(1000)
                    if (p.HasExited && p.ExitCode > 0) then
                        // try to launch it with output redirected to give an error message to the user
                        startInfo.RedirectStandardError <- true
                        startInfo.RedirectStandardOutput <- true
                        startInfo.UseShellExecute <- false
                        let p = Process.Start(startInfo)
                        p.WaitForExit()
                        fOnMessage(p.StandardError.ReadToEnd())
                        fOnMessage(p.StandardOutput.ReadToEnd())
                with :? Win32Exception as ex ->
                    fOnMessage(ex.Message + Environment.NewLine + java + Environment.NewLine + arguments)
            finally
                Environment.CurrentDirectory <- oldPath

    let MinecraftAs minecraftBinPath java clientMemoryArguments alias (fOnMessage:Action<_>) = minecraftAs minecraftBinPath java clientMemoryArguments fOnMessage.Invoke alias 
        