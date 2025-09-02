namespace ProcessAffinityManager.Models

type ProfileType =
    | CPUAffinity
    | CPUSet

type ExclusiveMode =
    | NotExclusive
    | IsExclusive of FallbackProfileId: System.Guid option

type SubProfile =
    { Id: System.Guid
      ParentProfileId: System.Guid
      DelaySeconds: int }

type Profile =
    { Id: System.Guid
      Name: string
      ProfileType: ProfileType
      CpuMask: int64 // A bitmask representing the selected cores
      ExclusiveMode: ExclusiveMode
      SubProfiles: SubProfile list }

type ProfileTarget =
    | MainProfile of profileId: System.Guid
    | SubProfile of subProfileId: System.Guid

type RuleType =
    | ProcessName
    | ExecutableFilePath
    | ExecutableDirectory
    | FallbackDefault

type Rule =
    { Id: System.Guid
      RuleType: RuleType
      Criteria: string
      Priority: int
      Target: ProfileTarget }
