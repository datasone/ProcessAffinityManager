module ProcessAffinityManager.Services.CpuSetsHelper

open ProcessAffinityManager.Models
open ProcessAffinityManager.Services.NativeMethods

let cpuSetIdByCoreId (cpuSetsInfo: SYSTEM_CPU_SET_INFORMATION seq) =
    cpuSetsInfo
    |> Seq.map (fun info ->
        let cpuSetId = info.CpuSetUnion.CpuSet.Id
        let cpuCoreId = info.CpuSetUnion.CpuSet.LogicalProcessorIndex
        (cpuCoreId, cpuSetId))
    |> Map.ofSeq
    |> Map.values
    |> List.ofSeq

let cpuMaskToSets (cpuSetIds: uint32 list) (cpuMask: int64) =
    let coreCount = System.Environment.ProcessorCount

    seq { 0 .. coreCount - 1 }
    |> Seq.filter (fun i -> (cpuMask >>> i) &&& 1L = 1L)
    |> Seq.map (fun i -> cpuSetIds[i])

let defaultCpuProfiles (cpuSetsInfo: SYSTEM_CPU_SET_INFORMATION seq) =
    let efficiencyGroups =
        cpuSetsInfo |> Seq.groupBy _.CpuSetUnion.CpuSet.EfficiencyClass |> Map.ofSeq

    let llcGroups =
        cpuSetsInfo |> Seq.groupBy _.CpuSetUnion.CpuSet.LastLevelCacheIndex |> Map.ofSeq

    let coreCount = System.Environment.ProcessorCount

    let defaultProfile =
        { Id = System.Guid.NewGuid()
          Name = "All Cores"
          ProfileType = CPUAffinity
          CpuMask = (1L <<< coreCount) - 1L
          ExclusiveMode = NotExclusive
          SubProfiles = [] }

    let additionalProfiles =
        if efficiencyGroups.Keys.Count > 1 then
            efficiencyGroups
            |> Seq.map (fun group ->
                let nameTag =
                    if group.Key = Seq.max efficiencyGroups.Keys then
                        "Performance"
                    else
                        "Efficiency"

                if llcGroups.Keys.Count > 1 then
                    let llcGroups =
                        group.Value |> Seq.groupBy _.CpuSetUnion.CpuSet.LastLevelCacheIndex |> Map.ofSeq

                    llcGroups
                    |> Seq.map (fun group ->
                        let cpuMask =
                            group.Value
                            |> Seq.map _.CpuSetUnion.CpuSet.LogicalProcessorIndex
                            |> Seq.fold (fun mask i -> mask ||| (1L <<< int32 i)) 0L

                        { Id = System.Guid.NewGuid()
                          Name = $"{nameTag} Cores - Last Level Cache Group {group.Key}"
                          ProfileType = CPUAffinity
                          CpuMask = cpuMask
                          ExclusiveMode = NotExclusive
                          SubProfiles = [] })
                else
                    let cpuMask =
                        group.Value
                        |> Seq.map _.CpuSetUnion.CpuSet.LogicalProcessorIndex
                        |> Seq.fold (fun mask i -> mask ||| (1L <<< int32 i)) 0L

                    [ { Id = System.Guid.NewGuid()
                        Name = $"{nameTag} Cores"
                        ProfileType = CPUAffinity
                        CpuMask = cpuMask
                        ExclusiveMode = NotExclusive
                        SubProfiles = [] } ])
        else
            llcGroups
            |> Seq.map (fun group ->
                let cpuMask =
                    group.Value
                    |> Seq.map _.CpuSetUnion.CpuSet.LogicalProcessorIndex
                    |> Seq.fold (fun mask i -> mask ||| (1L <<< int32 i)) 0L

                [ { Id = System.Guid.NewGuid()
                    Name = $"Last Level Cache Group {group.Key}"
                    ProfileType = CPUAffinity
                    CpuMask = cpuMask
                    ExclusiveMode = NotExclusive
                    SubProfiles = [] } ])

    additionalProfiles |> Seq.concat |> Seq.append (Seq.singleton defaultProfile)
