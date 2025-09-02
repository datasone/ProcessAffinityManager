namespace ProcessAffinityManager.Services

open System
open System.Collections.ObjectModel
open Avalonia
open Avalonia.Media
open Avalonia.Threading

type LogLevel =
    | Info
    | Warning
    | ErrorLog
    
    override this.ToString() =
        match this with
        | Info -> "Info"
        | Warning -> "Warning"
        | ErrorLog -> "Error" 

type LogEntry = {
    Timestamp: DateTime
    Level: LogLevel
    Message: string
}

module LoggingService =
    let LogEntries = ObservableCollection<LogEntry>()

    let private log (level: LogLevel) (message: string) =
        let entry = { Timestamp = DateTime.Now; Level = level; Message = message }
        
        Dispatcher.UIThread.Post(fun () ->
            LogEntries.Add(entry)

            if LogEntries.Count > 5000 then
                LogEntries.RemoveAt(0)
        )

    let Info (message: string) = log Info message
    let Warn (message: string) = log Warning message
    let Error (message: string) = log ErrorLog message