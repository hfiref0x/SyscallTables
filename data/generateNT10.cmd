rem example command script
set arg1=%1
C:\SyscallTables\Compiled\scg64 Z:\WindowsNT\%arg1%\ntdll.dll >C:\SyscallTables\Compiled\Composition\X86_64\NT10\ntos\%arg1%.txt
C:\SyscallTables\Compiled\scg64 Z:\WindowsNT\%arg1%\win32u.dll >C:\SyscallTables\Compiled\Composition\X86_64\NT10\win32k\%arg1%.txt
pushd C:\SyscallTables\Compiled\Composition\X86_64\
C:\SyscallTables\Compiled\Composition\sstc.exe -h -d NT10
C:\SyscallTables\Compiled\Composition\sstc.exe -h -w -d NT10
popd
pause