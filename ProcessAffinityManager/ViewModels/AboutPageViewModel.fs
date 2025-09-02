namespace ProcessAffinityManager.ViewModels

open System.Reflection

type AboutPageViewModel() =
    inherit ViewModelBase()
    
    member val Version =
        AssemblyName
            .GetAssemblyName(Assembly.GetExecutingAssembly().Location)
            .Version.ToString() with get

    member val Author = "datasone" with get
    member val GitHubUrl = "https://github.com/datasone/ProcessAffinityManager" with get
    member val License = "MIT" with get
