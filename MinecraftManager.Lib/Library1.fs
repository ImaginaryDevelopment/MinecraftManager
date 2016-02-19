namespace MinecraftManager.Lib

open System
open System.Diagnostics
open System.ComponentModel
open System.IO

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
