# Syscall Tables
[![Visitors](https://api.visitorbadge.io/api/visitors?path=https%3A%2F%2Fgithub.com%2Fhfiref0x%2FSyscallTables&countColor=%23263759&style=flat)](https://visitorbadge.io/status?path=https%3A%2F%2Fgithub.com%2Fhfiref0x%2FSyscallTables)

## Combined Windows syscall tables

# X86-64

## Ntoskrnl service tables

- Windows 2003 SP2 build 3790 (also Windows XP 64)
- Windows Vista RTM build 6000
- Windows Vista SP2 build 6002 (identical to 6001)
- Windows 7 SP1 build 7601 (identical to 7600)
- Windows 8 RTM build 9200
- Windows 8.1 build 9600
- Windows 10 TP build 10061
- Windows 10 TH1 build 10240
- Windows 10 TH2 build 10586
- Windows 10 RS1 build 14393
- Windows 10 RS2 build 15063
- Windows 10 RS3 build 16299
- Windows 10 RS4 build 17134
- Windows 10 RS5 build 17763
- Windows 10 19H1 build 18362
- Windows 10 19H2 build 18363
- Windows 10 20H1 build 19041 (19042, 19043, 19044, 19045 are identical to 19041);
- Windows Server 2022 build 20348
- Windows 11 21H2 build 22000
- Windows 11 22H2 build 22621
- Windows 11 23H2 build 22631
- Windows 11 24H2 build 26120
- Windows 11 25H2 build 27842
- Windows 11 25H2 build 27881

**Located in** `Compiled\Composition\X86_64\ntos`

NT6 (Windows Vista/7/8/8.1) + bonus NT5.2 (Windows XP x64)  
**View online:** https://hfiref0x.github.io/sctables/X86_64/NT6_syscalls.html

NT10 (Windows 10/11)  
**View online:** https://hfiref0x.github.io/sctables/X86_64/NT10_syscalls.html

## Win32k service tables

- Windows Vista RTM build 6000
- Windows 7 SP1 build 7601
- Windows 8 RTM build 9200
- Windows 8.1 build 9600
- Windows 10 TH1 build 10240
- Windows 10 TH2 build 10586
- Windows 10 RS1 build 14393
- Windows 10 RS2 build 15063
- Windows 10 RS3 build 16299
- Windows 10 RS4 build 17134
- Windows 10 RS5 build 17763
- Windows 10 19H1 build 18362
- Windows 10 19H2 build 18363
- Windows 10 20H1 build 19041 (19042, 19043, 19044, 19045 are identical to 19041);
- Windows Server 2022 build 20348
- Windows 11 21H2 build 22000
- Windows 11 22H2 build 22621
- Windows 11 23H2 build 22631
- Windows 11 24H2 build 26120
- Windows 11 25H2 build 27842
- Windows 11 25H2 build 27881

**Located in** `Compiled\Composition\X86_64\win32k`

NT6 (Windows Vista/7/8/8.1)  
**View online:** https://hfiref0x.github.io/sctables/X86_64/NT6_w32ksyscalls.html

NT10 (Windows 10/11)  
**View online:** https://hfiref0x.github.io/sctables/X86_64/NT10_w32ksyscalls.html

## IUM service tables

- Windows 10 20H1 build 19041 (19042, 19043, 19044, 19045 are identical to 19041)
- Windows 11 DEV build 25276
- Windows 11 25H2 build 27823

**Located in** `Compiled\Composition\X86_64\ium`

NT10 (Windows 10/11)  
**View online:** https://hfiref0x.github.io/sctables/X86_64/NT10_iumsyscalls.html

# ARM64

## Ntoskrnl service tables

- Windows 11 23H2 build 22631
- Windows 11 24H2 build 26100

**Located in** `Compiled\Composition\ARM64\ntos`

NT10 (Windows 10/11)  
**View online:** https://hfiref0x.github.io/sctables/ARM64/syscalls.html

## Win32k service tables

- Windows 11 23H2 build 22631
- Windows 11 24H2 build 26100

**Located in** `Compiled\Composition\ARM64\win32k`

NT10 (Windows 10/11)  
**View online:** https://hfiref0x.github.io/sctables/ARM64/w32ksyscalls.html

# Usage

1. Dump syscall table list (using `scg` for ntoskrnl or `wscg64` for win32k). See run examples for more info.  
2. Place syscall list text file named as build number inside the directory (use `ntos` subdirectory for ntoskrnl.exe tables, `win32k` subdirectory for win32k.sys tables).
3. Use `sstc.exe` to run composer with `-h` key to generate HTML output file, otherwise the output file will be saved in markdown table format. Specify `-w` as the second parameter if you want to generate win32k combined syscall table. By default, `sstc` will read files from the "Tables" directory and compose the output table. Specify `-d "DirectoryName"` if you want to generate table from a different directory; in any case, `sstc` expects `ntos` and/or `win32k` subfolders to be present inside the target directory.

**Run Examples:**
- `scg64.exe c:\wfiles\ntdll\ntdll_7600.dll > table7600.txt`
- `scg64.exe c:\wfiles\win32u\win32u_11.dll > win32u_11.txt`
- `sstc -w`
- `sstc -h`
- `sstc -h -d OnlyW10`
- `sstc -h -w`

# 3rd Party Code Usage

Uses [Zydis x86/x86-64 disassembler and code generation library](https://github.com/zyantific/zydis).

# Build

Composer source code is written in C#.  
To build from source, you need Microsoft Visual Studio 2022 or higher and .NET Framework 4.8 or higher.  
Both `scg` and `wscg` source code are written in C.  
To build from source, you need Microsoft Visual Studio 2022 with SDK 19041 or higher installed.

# Authors

- scg (c) 2018 - 2025 SyscallTables Project
- sstComposer (c) 2016 - 2025 SyscallTables Project

Original scg (c) 2011 gr8
