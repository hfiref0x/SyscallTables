
# Syscall tables
## Combined Windows syscall tables

# X86-64

## Ntoskrnl service tables

+ Windows 2003 SP2 build 3790 also Windows XP 64;
+ Windows Vista RTM build 6000;
+ Windows Vista SP2 build 6002; * Note that it is identical for 6001 build
+ Windows 7 SP1 build 7601; * Note that it is identical for 7600 build
+ Windows 8 RTM build 9200;
+ Windows 8.1 build 9600;
+ Windows 10 TP build 10061;
+ Windows 10 TH1 build 10240;
+ Windows 10 TH2 build 10586;
+ Windows 10 RS1 build 14393;
+ Windows 10 RS2 build 15063;
+ Windows 10 RS3 build 16299;
+ Windows 10 RS4 build 17134;
+ Windows 10 RS5 build 17763;
+ Windows 10 19H1 build 18362;
+ Windows 10 19H2 build 18363;
+ Windows 10 20H1 build 19041; * Note that 19042, 19043, 19044, 19045 are the same as 19041
+ Windows Server 2022 build 20348;
+ Windows 11 21H2 build 22000;
+ Windows 11 22H2 build 22621;
+ Windows 11 23H2 build 22631;
+ Windows 11 24H2 build 26120;
+ Windows 11 25H2 build 27686;
+ Windows 11 25H2 build 27695;
+ Windows 11 25H2 build 27723;
+ Windows 11 25H2 build 27729;
+ Windows 11 25H2 build 27783;
+ Windows 11 25H2 build 27823;
+ Windows 11 25H2 build 27842.

** located in Compiled\Composition\X86_64\ntos

NT6 (Windows Vista/7/8/8.1) + bonus NT5.2 (Windows XP x64)

**View online** https://hfiref0x.github.io/X86_64/NT6_syscalls.html

NT10 (Windows 10/11)

**View online** https://hfiref0x.github.io/X86_64/NT10_syscalls.html

## Win32k service tables

+ Windows Vista RTM build 6000;
+ Windows 7 SP1 build 7601;
+ Windows 8 RTM build 9200;
+ Windows 8.1 build 9600;
+ Windows 10 TH1 build 10240;
+ Windows 10 TH2 build 10586;
+ Windows 10 RS1 build 14393;
+ Windows 10 RS2 build 15063;
+ Windows 10 RS3 build 16299;
+ Windows 10 RS4 build 17134;
+ Windows 10 RS5 build 17763;
+ Windows 10 19H1 build 18362;
+ Windows 10 19H2 build 18363;
+ Windows 10 20H1 build 19041; * Note that 19042, 19043, 19044, 19045 are the same as 19041
+ Windows Server 2022 build 20348;
+ Windows 11 21H2 build 22000;
+ Windows 11 22H2 build 22621;
+ Windows 11 23H2 build 22631;
+ Windows 11 24H2 build 26120;
+ Windows 11 25H2 build 27686;
+ Windows 11 25H2 build 27695;
+ Windows 11 25H2 build 27723;
+ Windows 11 25H2 build 27729;
+ Windows 11 25H2 build 27783;
+ Windows 11 25H2 build 27823;
+ Windows 11 25H2 build 27842.

** located in Compiled\Composition\X86_64\win32k

NT6 (Windows Vista/7/8/8.1)

**View online** https://hfiref0x.github.io/X86_64/NT6_w32ksyscalls.html

NT10 (Windows 10/11)

**View online** https://hfiref0x.github.io/X86_64/NT10_w32ksyscalls.html

## IUM service tables

+ Windows 10 20H1 build 19041; * Note that 19042, 19043, 19044, 19045 are the same as 19041
+ Windows 11 DEV build 25276;
+ Windows 11 25H2 27823.

** located in Compiled\Composition\X86_64\ium

NT10 (Windows 10/11)
**View online** https://hfiref0x.github.io/X86_64/NT10_iumsyscalls.html

# ARM64

## Ntoskrnl service tables

+ Windows 11 23H2 build 22631;
+ Windows 11 24H2 build 26100.

** located in Compiled\Composition\ARM64\ntos

NT10 (Windows 10/11)

**View online** https://hfiref0x.github.io/ARM64/syscalls.html

## Win32k service tables

+ Windows 11 23H2 build 22631;
+ Windows 11 24H2 build 26100.

** located in Compiled\Composition\ARM64\win32k

NT10 (Windows 10/11)

**View online** https://hfiref0x.github.io/ARM64/w32ksyscalls.html

# Usage

1) Dump syscall table list (using scg for ntoskrnl or wscg64 for win32k), see run examples for more info.  
2) [Tables] <- put syscall list text file named as build number inside directory (ntos subdirectory for ntoskrnl.exe tables, win32k subdirectory for win32k.sys tables);

3) sstc.exe <- run composer with key -h to generate html output file, else output file will be saved in markdown table format. Specify -w as second param if you want to generate win32k combined syscall table. By default sstc will read files from "Tables" (without quotes) directory and compose output table. Specify -d "DirectoryName" (without quotes) if you want to generate table from different directory, in any case sstc will expect ntos and/or win32k subfolders are present inside target directory.

Run Examples:
* scg64.exe c:\wfiles\ntdll\ntdll_7600.dll > table7600.txt
* scg64.exe c:\wfiles\win32u\win32u_11.dll > win32u_11.txt 
* sstc -w
* sstc -h
* sstc -h -d OnlyW10
* sstc -h -w

# 3rd party code usage
Uses Zydis x86/x86-64 disassembler and code generation library (https://github.com/zyantific/zydis)

# Build

Composer source code written in C#. In order to build from source you need Microsoft Visual Studio version 2022 and higher and .NET Framework version 4.5 and higher. Both scg and wscg source code written in C. In order to build from source you need Microsoft Visual Studio version 2022 with SDK 19041 or higher installed.

# Authors

+ scg (c) 2018 - 2025 SyscallTables Project
+ sstComposer (c) 2016 - 2025 SyscallTables Project

Original scg (c) 2011 gr8
