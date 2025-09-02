namespace ProcessAffinityManager

open System
open System.Threading
open Avalonia

module Program =

    let appGuid = "5196AEF3-59D1-499C-980C-10F32C76BC7E"

    [<CompiledName "BuildAvaloniaApp">]
    let buildAvaloniaApp () =
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace(areas = Array.empty)

    [<EntryPoint; STAThread>]
    let main argv =
        use mutex = new Mutex(false, $"Global\\{appGuid}")

        if mutex.WaitOne(0, false) then
            buildAvaloniaApp().StartWithClassicDesktopLifetime(argv)
        else
            -1
