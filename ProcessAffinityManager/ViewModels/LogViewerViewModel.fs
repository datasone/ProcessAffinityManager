namespace ProcessAffinityManager.ViewModels

open CommunityToolkit.Mvvm.ComponentModel
open ProcessAffinityManager.Services

type LogViewerViewModel() =
    inherit ViewModelBase()
    
    member val LogEntries = LoggingService.LogEntries with get