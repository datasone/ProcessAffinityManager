namespace ProcessAffinityManager.Services

open System
open System.Diagnostics
open System.Management
open ProcessAffinityManager.Models

type ProcessMonitoringService() as this =
    let mutable isRunning = false

    let mutable cpuSetIdsByCoreId: uint32 list = List.empty
    let mutable currentRules: Rule seq = Seq.empty
    let mutable currentProfiles: Profile seq = Seq.empty

    let mutable processStartWatcher: ManagementEventWatcher = null
    let mutable processStopWatcher: ManagementEventWatcher = null

    let processStartedEvent = Event<EventHandler<ProcessEventArgs>, ProcessEventArgs>()
    let processChangedEvent = Event<EventHandler<ProcessEventArgs>, ProcessEventArgs>()
    let processEndedEvent = Event<EventHandler<int>, int>()

    let mutable exclusiveProcessPids = Set.empty<int>
    let mutable exclusiveProfile: Profile option = None
    let mutable fallbackProfile: Profile option = None

    let defaultAffinity = Process.GetCurrentProcess().ProcessorAffinity

    let actualProfile processId (profile: Profile) =
        match profile, exclusiveProfile with
        | profile, Some exclusive when profile.Id = exclusive.Id ->
            exclusiveProcessPids <- exclusiveProcessPids |> Set.add processId

            if exclusiveProcessPids.Count = 1 then
                this.ApplyFallbackToAllOtherProcesses()

            profile
        | profile, _ ->
            if exclusiveProcessPids.Contains processId then
                exclusiveProcessPids <- exclusiveProcessPids |> Set.remove processId

                if exclusiveProcessPids.IsEmpty then
                    this.RestoreAllOtherProcesses processId

            if exclusiveProcessPids.IsEmpty then
                profile
            else
                Option.get fallbackProfile

    let getProcessName (processObj: Process) =
        let filePath =
            try
                Some processObj.MainModule.FileName
            with _ ->
                None

        filePath
        |> Option.map System.IO.Path.GetFileName
        |> Option.defaultValue processObj.ProcessName

    let applyProfileToProcess processId profile =
        try
            let processObj = Process.GetProcessById processId
            let processName = getProcessName processObj
            let profile = actualProfile processId profile

            // Reset before applying, as we may apply a CPU set on a process already have affinity set
            processObj.ProcessorAffinity <- defaultAffinity
            let _ = NativeMethods.setProcessDefaultCpuSets processObj Seq.empty

            match profile.ProfileType with
            | CPUAffinity ->
                processObj.ProcessorAffinity <- IntPtr(profile.CpuMask)
                LoggingService.Info $"Applied profile '{profile.Name}' to {processName} (PID: {processId})"
            | CPUSet ->
                let setIdsToApply = CpuSetsHelper.cpuMaskToSets cpuSetIdsByCoreId profile.CpuMask

                match NativeMethods.setProcessDefaultCpuSets processObj setIdsToApply with
                | Error error -> LoggingService.Error $"Failed to apply profile to PID {processId}: {error}"
                | _ -> ()

                LoggingService.Info $"Applied CPU Set profile '{profile.Name}' to {processName} (PID: {processId})"

            let args =
                { ProcessId = processId
                  ProcessName = processName
                  FilePath = "" // Not relevant here
                  AppliedProfile = Some profile }

            processChangedEvent.Trigger(null, args)

        with
        | :? ArgumentException ->
            if exclusiveProcessPids.Contains processId then
                exclusiveProcessPids <- exclusiveProcessPids |> Set.remove processId

                if exclusiveProcessPids.IsEmpty then
                    this.RestoreAllOtherProcesses processId

            processEndedEvent.Trigger(null, processId)
        | ex -> LoggingService.Error $"Failed to apply profile to PID {processId}: {ex.Message}"

    let applySubProfileToProcess processId subProfile =
        let parentProfile =
            currentProfiles |> Seq.tryFind (fun p -> p.Id = subProfile.ParentProfileId)

        parentProfile
        |> Option.iter (fun parent ->
            LoggingService.Info
                $"Scheduling profile '{parent.Name}' for PID {processId} in {subProfile.DelaySeconds} seconds."

            async {
                do! Async.Sleep(subProfile.DelaySeconds * 1000)

                applyProfileToProcess processId parent
            }
            |> Async.Start)

    let matchRule (processName: string) (filePath: string) =
        let processName =
            if processName.EndsWith(".exe") then
                processName.Substring(0, processName.Length - 4)
            else
                processName

        currentRules
        |> Seq.tryFind (fun rule ->
            match rule.RuleType with
            | ProcessName ->
                let criteria =
                    if rule.Criteria.EndsWith(".exe") then
                        rule.Criteria.Substring(0, rule.Criteria.Length - 4)
                    else
                        rule.Criteria

                processName.Equals(criteria, StringComparison.OrdinalIgnoreCase)
            | ExecutableFilePath -> filePath.Equals(rule.Criteria, StringComparison.OrdinalIgnoreCase)
            | ExecutableDirectory ->
                if String.IsNullOrEmpty(filePath) then
                    false
                else
                    filePath.StartsWith(rule.Criteria, StringComparison.OrdinalIgnoreCase)
            | FallbackDefault -> true)

    let profileFromRule (matchedRule: Rule option) =
        let matchedProfileId =
            matchedRule
            |> Option.bind (fun rule ->
                match rule.Target with
                | MainProfile profileId -> Some profileId
                | SubProfile subProfileId ->
                    currentProfiles
                    |> Seq.collect _.SubProfiles
                    |> Seq.tryFind (fun sp -> sp.Id = subProfileId)
                    |> Option.map _.ParentProfileId)

        matchedProfileId
        |> Option.bind (fun profileId -> currentProfiles |> Seq.tryFind (fun p -> p.Id = profileId))

    let onProcessStarted sender (e: EventArrivedEventArgs) =
        if not isRunning then
            ()
        else
            let processName = e.NewEvent.Properties["ProcessName"].Value :?> string
            let processId = Convert.ToInt32 e.NewEvent.Properties["ProcessID"].Value

            let filePath =
                try
                    Process.GetProcessById(processId).MainModule.FileName
                with _ ->
                    ""

            let matchedRule = matchRule processName filePath

            matchedRule
            |> Option.iter (fun rule ->
                try
                    match rule.Target with
                    | MainProfile profileId ->
                        currentProfiles
                        |> Seq.tryFind (fun p -> p.Id = profileId)
                        |> Option.iter (applyProfileToProcess processId)
                    | SubProfile subProfileId ->
                        currentProfiles
                        |> Seq.collect _.SubProfiles
                        |> Seq.tryFind (fun sp -> sp.Id = subProfileId)
                        |> Option.iter (applySubProfileToProcess processId)
                with ex ->
                    LoggingService.Error $"Failed during profile application for PID {processId}: {ex.Message}")

            let eventArgs =
                { ProcessId = processId
                  ProcessName = processName
                  FilePath = filePath
                  AppliedProfile = matchedRule |> profileFromRule |> Option.map (actualProfile processId) }

            processStartedEvent.Trigger(box sender, eventArgs)

    let onProcessEnded sender (e: EventArrivedEventArgs) =
        let processId = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value)

        if exclusiveProcessPids.Contains processId then
            exclusiveProcessPids <- exclusiveProcessPids |> Set.remove processId

            if exclusiveProcessPids.IsEmpty then
                this.RestoreAllOtherProcesses processId

        processEndedEvent.Trigger(box sender, processId)

    member private this.ApplyFallbackToAllOtherProcesses() =
        fallbackProfile
        |> Option.iter (fun fallback ->
            LoggingService.Info "Exclusive process started. Applying fallback profile to other processes..."

            Process.GetProcesses()
            |> Array.filter (fun p -> not (exclusiveProcessPids.Contains(p.Id)))
            |> Array.iter (fun p -> applyProfileToProcess p.Id fallback))


    member private this.RestoreAllOtherProcesses(excludedPid: int) =
        LoggingService.Info "Last exclusive process ended. Restoring original profiles..."

        for p in Process.GetProcesses() |> Seq.filter (fun proc -> proc.Id <> excludedPid) do
            let processName = getProcessName p

            let filePath =
                try
                    p.MainModule.FileName
                with _ ->
                    ""

            let matchedRule = matchRule processName filePath

            matchedRule
            |> Option.iter (fun rule ->
                match rule.Target with
                | MainProfile profileId ->
                    currentProfiles
                    |> Seq.tryFind (fun prof -> prof.Id = profileId)
                    |> Option.iter (applyProfileToProcess p.Id)
                | SubProfile subProfileId ->
                    currentProfiles
                    |> Seq.collect _.SubProfiles
                    |> Seq.tryFind (fun sp -> sp.Id = subProfileId)
                    |> Option.iter (applySubProfileToProcess p.Id))

    interface IProcessMonitoringService with
        member _.IsRunning = isRunning

        [<CLIEvent>]
        member _.ProcessStarted = processStartedEvent.Publish

        [<CLIEvent>]
        member _.ProcessChanged = processChangedEvent.Publish

        [<CLIEvent>]
        member _.ProcessEnded = processEndedEvent.Publish

        member _.Start cpuSetIds rules profiles =
            if isRunning then
                ()
            else
                LoggingService.Info "Starting process monitoring service..."
                isRunning <- true
                cpuSetIdsByCoreId <- cpuSetIds
                currentRules <- rules |> Seq.sortBy _.Priority
                currentProfiles <- profiles

                exclusiveProfile <- currentProfiles |> Seq.tryFind (fun p -> p.ExclusiveMode <> NotExclusive)

                fallbackProfile <-
                    exclusiveProfile
                    |> Option.bind (fun ep ->
                        match ep.ExclusiveMode with
                        | IsExclusive(Some fallbackId) -> currentProfiles |> Seq.tryFind (fun p -> p.Id = fallbackId)
                        | _ -> None)

                exclusiveProcessPids <- Set.empty

                try
                    let startQuery = WqlEventQuery("Win32_ProcessStartTrace", TimeSpan.FromSeconds(1L))
                    // let startQuery = WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace")

                    processStartWatcher <- new ManagementEventWatcher(startQuery)
                    processStartWatcher.EventArrived.AddHandler(EventArrivedEventHandler(onProcessStarted))
                    processStartWatcher.Start()

                    let stopQuery = WqlEventQuery("Win32_ProcessStopTrace", TimeSpan.FromSeconds(1L))
                    // let stopQuery = WqlEventQuery("SELECT * FROM Win32_ProcessStopTrace")

                    processStopWatcher <- new ManagementEventWatcher(stopQuery)
                    processStopWatcher.EventArrived.AddHandler(EventArrivedEventHandler(onProcessEnded))
                    processStopWatcher.Start()
                with ex ->
                    LoggingService.Error $"WMI Initialization failed: {ex.Message}"

        member _.Stop() =
            if not isRunning then
                ()
            else
                LoggingService.Info "Stopping process monitoring service..."
                isRunning <- false

                processStartWatcher.Stop()
                processStartWatcher.Dispose()
                processStopWatcher.Stop()
                processStopWatcher.Dispose()

        member _.ApplyProfileToProcess processId profile = applyProfileToProcess processId profile

        member _.ApplySubProfileToProcess processId subProfile =
            applySubProfileToProcess processId subProfile

        member this.GetCurrentProcesses() =
            exclusiveProcessPids <- set []
            
            Process.GetProcesses()
            |> Array.map (fun p ->
                let processName = getProcessName p

                let filePath =
                    try
                        p.MainModule.FileName
                    with _ ->
                        ""

                let matchedProfile = profileFromRule (matchRule processName filePath)

                if isRunning then
                    matchedProfile
                    |> Option.iter (fun profile -> applyProfileToProcess p.Id profile)

                { ProcessId = p.Id
                  ProcessName = processName
                  FilePath = filePath
                  AppliedProfile = matchedProfile })
            |> Seq.ofArray
