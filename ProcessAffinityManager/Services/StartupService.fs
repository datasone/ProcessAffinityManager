namespace ProcessAffinityManager.Services

open System
open Microsoft.Win32.TaskScheduler

module StartupService =
    let private taskName = "ProcessAffinityManager_Startup"

    let private getExecutablePath () =
        System.Reflection.Assembly.GetExecutingAssembly().Location

    let isStartupTaskScheduled () =
        try
            use ts = new TaskService()
            ts.FindTask(taskName) <> null
        with ex ->
            LoggingService.Error $"Error checking for startup task: {ex.Message}"
            false

    let createStartupTask () =
        try
            use ts = new TaskService()

            let td = ts.NewTask()
            td.RegistrationInfo.Description <- "Starts Process Affinity Manager on user logon."

            td.Principal.RunLevel <- TaskRunLevel.Highest

            td.Triggers.Add(new LogonTrigger()) |> ignore

            let exePath = getExecutablePath ()
            let workingDir = System.IO.Path.GetDirectoryName(exePath)
            td.Actions.Add(new ExecAction(exePath, null, workingDir)) |> ignore

            td.Settings.DisallowStartIfOnBatteries <- false
            td.Settings.StopIfGoingOnBatteries <- false
            td.Settings.ExecutionTimeLimit <- TimeSpan.Zero

            ts.RootFolder.RegisterTaskDefinition(taskName, td) |> ignore
            true
        with ex ->
            LoggingService.Error $"Failed to create startup task: {ex.Message}"
            false

    let removeStartupTask () =
        try
            use ts = new TaskService()

            if ts.FindTask(taskName) <> null then
                ts.RootFolder.DeleteTask(taskName)

            true
        with ex ->
            LoggingService.Error $"Failed to remove startup task: {ex.Message}"
            false
