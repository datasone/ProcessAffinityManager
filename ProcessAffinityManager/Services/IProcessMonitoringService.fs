namespace ProcessAffinityManager.Services

open System
open ProcessAffinityManager.Models

type ProcessEventArgs =
    { ProcessId: int
      ProcessName: string
      FilePath: string
      AppliedProfile: Profile option }

type IProcessMonitoringService =
    abstract member IsRunning: bool with get
    
    [<CLIEvent>]
    abstract member ProcessStarted: IEvent<EventHandler<ProcessEventArgs>, ProcessEventArgs>

    [<CLIEvent>]
    abstract member ProcessChanged: IEvent<EventHandler<ProcessEventArgs>, ProcessEventArgs>
    
    [<CLIEvent>]
    abstract member ProcessEnded: IEvent<EventHandler<int>, int>

    abstract member Start: cpuSetIds: uint32 list -> rules: Rule seq -> profiles: Profile seq -> unit

    abstract member Stop: unit -> unit

    abstract member ApplyProfileToProcess: processId: int -> profile: Profile -> unit

    abstract member ApplySubProfileToProcess: processId: int -> subProfile: SubProfile -> unit

    abstract member GetCurrentProcesses: unit -> ProcessEventArgs seq
