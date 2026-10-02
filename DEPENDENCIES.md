# Pinned build inputs

| Component | Version / identity | Source |
| --- | --- | --- |
| AutoEvent base | commit `3fcb670d447650f82bcf3dc30c262a4dbe533e57` | https://github.com/RisottoMan/AutoEvent |
| EXILED | ExMod `v9.14.2`, NuGet `ExMod.Exiled` 9.14.2 | https://github.com/ExMod-Team/EXILED/releases/tag/v9.14.2 |
| ProjectMER | `2026.7.6.1`, `ProjectMER.dll` SHA-256 `2091A7D44BC3D09EBC7764BCD4B16BA5C7219149EABD82285511D81DADC4B150` | https://github.com/Michal78900/ProjectMER/releases/tag/2026.7.6.1 |
| AudioPlayerApi | `1.1.2`, `AudioPlayerApi.dll` SHA-256 `D034AA58D1F386C8D65F83ED3D57DC75995DEB9C45347F64435BD311E5F5772E` | https://github.com/Killers0992/AudioPlayerApi/releases/tag/1.1.2 |
| SCP:SL dedicated server | Steam build ID `24893701`; LabAPI assembly `1.1.7.0` | Installed dedicated server used for the build |

`ProjectMER.dll` and `AudioPlayerApi.dll` are downloaded from the pinned releases by `prepare-dependencies.ps1` and verified against the SHA-256 checksums above. The generated `dependencies/` directory is ignored by Git. EXILED is restored from NuGet. Game assemblies come from a locally installed SCP:SL dedicated server (Steam app 996560); `build-exiled.ps1` checks its LabAPI version and supplies them through `SL_REFERENCES`.
