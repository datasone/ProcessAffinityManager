namespace ProcessAffinityManager.Services

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open ProcessAffinityManager.Models
open ProcessAffinityManager.Services.NativeMethods

type AppTheme =
    | System
    | Light
    | Dark

// TODO: i18n
type AppSettings =
    { StartOnSystemBoot: bool
      StartMinimized: bool
      Theme: AppTheme }

type AppConfiguration =
    { Rules: Rule list
      Profiles: Profile list
      Settings: AppSettings }

module Defaults =
    let settings =
        { StartOnSystemBoot = false
          StartMinimized = false
          Theme = AppTheme.System }

    let configuration profiles =
        let allCoresProfile =
            profiles
            |> Seq.find (fun p -> p.Name = "All Cores")
            
        let defaultRule = {
            Id = Guid.NewGuid()
            RuleType = FallbackDefault
            Criteria = ""
            Priority = 0
            Target = MainProfile allCoresProfile.Id
        }
        
        { Rules = [ defaultRule ]
          Profiles = profiles
          Settings = settings }

module ConfigurationService =
    let private configPath =
        Path.Combine(
            Environment.GetFolderPath Environment.SpecialFolder.ApplicationData,
            "ProcessAffinityManager",
            "config.json"
        )

    let private options =
        let options = JsonSerializerOptions(WriteIndented = true)
        JsonFSharpOptions.Default().AddToJsonSerializerOptions options
        options

    let save (config: AppConfiguration) =
        try
            let dir = Path.GetDirectoryName configPath

            if not (Directory.Exists dir) then
                Directory.CreateDirectory dir |> ignore

            let json = JsonSerializer.Serialize(config, options)
            File.WriteAllText(configPath, json)
        with ex ->
            LoggingService.Error $"Failed to save configuration: {ex.Message}"

    let load (systemCpuSetsInfo: SYSTEM_CPU_SET_INFORMATION seq) : AppConfiguration =
        try
            if File.Exists configPath then
                let json = File.ReadAllText configPath
                JsonSerializer.Deserialize<AppConfiguration>(json, options)
            else
                let defaultProfiles =
                    CpuSetsHelper.defaultCpuProfiles systemCpuSetsInfo |> List.ofSeq

                Defaults.configuration defaultProfiles
        with ex ->
            LoggingService.Error $"Failed to load configuration: {ex.Message}"

            let defaultProfiles =
                CpuSetsHelper.defaultCpuProfiles systemCpuSetsInfo |> List.ofSeq

            Defaults.configuration defaultProfiles
