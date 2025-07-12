/*******************************************************************************
*
*  (C) COPYRIGHT AUTHORS, 2018 - 2024
*
*  TITLE:       MAIN.C
*
*  VERSION:     1.30
*
*  DATE:        18 Aug 2024
*
*  Ntdll/Win32u/Iumdll Syscall dumper
*  Based on gr8 scg project
*
* THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF
* ANY KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED
* TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
* PARTICULAR PURPOSE.
*
*******************************************************************************/

#if !defined UNICODE
#error ANSI build is not supported
#endif

#if defined (_MSC_VER)
#if (_MSC_VER >= 1900)
#ifdef _DEBUG
#pragma comment(lib, "vcruntimed.lib")
#pragma comment(lib, "ucrtd.lib")
#else
#pragma comment(lib, "libucrt.lib")
#pragma comment(lib, "libvcruntime.lib")
#endif
#endif
#endif

#include <Windows.h>
#include <stdio.h>
#include <strsafe.h>
#include "minirtl\cmdline.h"
#include "minirtl\minirtl.h"

#include <Zydis.h>

#pragma comment(lib, "version.lib")

typedef enum tagScgDllType {
    ScgNtDll = 0,
    ScgWin32u,
    ScgIumDll,
    ScgUnknown
} ScgDllType;

typedef struct _LANGANDCODEPAGE {
    WORD wLanguage;
    WORD wCodePage;
} LANGANDCODEPAGE, * LPTRANSLATE;

#ifndef RtlOffsetToPointer
#define RtlOffsetToPointer(Base, Offset)  ((PCHAR)( ((PCHAR)(Base)) + ((ULONG_PTR)(Offset))))
#endif

/*
* MapInputFile
*
* Purpose:
*
* Create mapped section from input file.
*
*/
PVOID MapInputFile(
    _In_ LPCWSTR lpFileName
)
{
    HANDLE hFile, hMapping;
    PVOID  pvImageBase = NULL;

    hFile = CreateFile(lpFileName,
        GENERIC_READ,
        FILE_SHARE_READ |
        FILE_SHARE_WRITE |
        FILE_SHARE_DELETE,
        NULL,
        OPEN_EXISTING,
        0,
        NULL);

    if (hFile != INVALID_HANDLE_VALUE) {

        hMapping = CreateFileMapping(hFile,
            NULL,
            PAGE_READONLY | SEC_IMAGE,
            0,
            0,
            NULL);

        if (hMapping != NULL) {

            pvImageBase = MapViewOfFile(hMapping,
                FILE_MAP_READ, 0, 0, 0);

            CloseHandle(hMapping);
        }
        CloseHandle(hFile);
    }
    return pvImageBase;
}

/*
* GetDllType
*
* Purpose:
*
* Parse dll version and check it against known values to determine dll type.
*
*/
ScgDllType GetDllType(
    _In_ LPCWSTR lpFileName)
{
    DWORD dwSize;
    DWORD dwHandle;
    PVOID vinfo = NULL;
    LPTRANSLATE lpTranslate = NULL;
    LPWSTR lpOriginalFileName;
    WCHAR szKey[100 + 1];

    ScgDllType dllType = ScgUnknown;

    dwSize = GetFileVersionInfoSize(lpFileName, &dwHandle);
    if (dwSize) {
        vinfo = LocalAlloc(LMEM_ZEROINIT, (SIZE_T)dwSize);
        if (vinfo) {
            if (GetFileVersionInfo(lpFileName, 0, dwSize, vinfo)) {

                dwSize = 0;
                if (VerQueryValue(vinfo,
                    L"\\VarFileInfo\\Translation",
                    (LPVOID*)&lpTranslate,
                    (PUINT)&dwSize))
                {
                    RtlFillMemory(&szKey, sizeof(szKey), 0);

                    StringCchPrintf(szKey,
                        RTL_NUMBER_OF(szKey),
                        TEXT("\\StringFileInfo\\%04x%04x\\InternalName"),
                        lpTranslate[0].wLanguage,
                        lpTranslate[0].wCodePage);

                    if (VerQueryValue(vinfo, szKey, (LPVOID*)&lpOriginalFileName, (PUINT)&dwSize)) {

                        if (_strcmpi(lpOriginalFileName, L"IumDll.dll") == 0) {
                            dllType = ScgIumDll;
                        }
                        else if (_strcmpi(lpOriginalFileName, L"Ntdll.dll") == 0) {
                            dllType = ScgNtDll;
                        }
                        else if (_strcmpi(lpOriginalFileName, L"win32u") == 0)
                        {
                            dllType = ScgWin32u;
                        }

                    }
                }
            }
            LocalFree(vinfo);
        }
    }

    return dllType;
}

VOID ProcessExportEntry(
    _In_ PCHAR FunctionCode,
    _In_ PCHAR FunctionName,
    _In_ ScgDllType DllType,
    _In_ WORD MachineType
)
{
    size_t cchFuncName = 0;
    DWORD sid;
    PBYTE ptrCode;
    ULONG i, max, value;

    USHORT targetUShort;
    ZydisDisassembledInstruction instruction;
    ZyanU64 runtime_address;
    ZyanUSize offset;
    ZydisMachineMode machineMode;
    CHAR nameBuffer[MAX_PATH];

    switch (DllType) {
    case ScgIumDll:
        targetUShort = 'uI';
        break;
    case ScgWin32u:
        targetUShort = 'tN';
        break;
    default:
        targetUShort = 'wZ';
        break;
    }

    if (*(USHORT*)FunctionName != targetUShort) {
        return;
    }

    if (S_OK != StringCchLengthA(FunctionName, MAX_PATH, &cchFuncName))
    {
        printf_s("scg: Unexpected function name length %zu\r\n", cchFuncName);
    }

    if (S_OK != StringCchCopyNA(nameBuffer, MAX_PATH, FunctionName, cchFuncName))
    {
        printf_s("scg: Unexpected error with function name\r\n");
    }

    sid = (DWORD)-1;
    ptrCode = (PBYTE)FunctionCode;
    i = 0;

    switch (MachineType)
    {
    case IMAGE_FILE_MACHINE_I386:
        max = 16;
        machineMode = ZYDIS_MACHINE_MODE_LEGACY_32;
        break;
    case IMAGE_FILE_MACHINE_AMD64:
    default:
        max = 32;
        machineMode = ZYDIS_MACHINE_MODE_LONG_64;
        break;
    }

    if (MachineType == IMAGE_FILE_MACHINE_AMD64 ||
        MachineType == IMAGE_FILE_MACHINE_I386)
    {
        offset = 0;
        runtime_address = (ZyanU64)ptrCode;

        RtlSecureZeroMemory(&instruction, sizeof(instruction));

        while (ZYAN_SUCCESS(ZydisDisassembleIntel(
            machineMode,
            runtime_address,
            ptrCode + offset,
            max - offset,
            &instruction)))
        {
            //
            // mov reg, SyscallId
            //
            if (instruction.info.length == 5 &&
                instruction.info.mnemonic == ZYDIS_MNEMONIC_MOV &&
                instruction.info.operand_count == 2 &&
                instruction.operands[0].type == ZYDIS_OPERAND_TYPE_REGISTER &&
                instruction.operands[1].type == ZYDIS_OPERAND_TYPE_IMMEDIATE)
            {
                sid = (DWORD)instruction.operands[1].imm.value.u;
                break;
            }
            offset += instruction.info.length;
            runtime_address += instruction.info.length;
        }
    }
    else if (MachineType == IMAGE_FILE_MACHINE_ARM64) {
        //
        // SVC SyscallId
        //
        value = *(DWORD*)ptrCode;
        sid = (value >> 5) & 0xffff;
    }

    if (sid != (DWORD)-1) {
        if (DllType == ScgNtDll) {
            //
            // Hack for consistency.
            //
            nameBuffer[0] = 'N';
            nameBuffer[1] = 't';
        }
        printf_s("%s\t%lu\n", nameBuffer, sid);
    }
#ifdef _DEBUG
    else {
        printf_s("scg: syscall index for %s not found\r\n", nameBuffer);
    }
#endif
}

/*
* ParseInputFile
*
* Purpose:
*
* Generate syscall list from given dll.
*
*/
VOID ParseInputFile(
    _In_ LPCWSTR lpFileName)
{
    PIMAGE_FILE_HEADER FileHeader;

    union {
        PIMAGE_OPTIONAL_HEADER32 oh32;
        PIMAGE_OPTIONAL_HEADER64 oh64;
    } oh;
    PIMAGE_EXPORT_DIRECTORY ExportDirectory = NULL;

    PULONG NameTableBase;
    PULONG FunctionsTableBase;
    PUSHORT NameOrdinalTableBase;

    PCHAR pvImageBase;
    ULONG entryIndex;

    ScgDllType dllType = GetDllType(lpFileName);

    if (dllType == ScgUnknown) {
        printf_s("scg: unknown or corrupted dll, must be ntdll/win32u/iumdll\r\n");
        return;
    }

    pvImageBase = MapInputFile(lpFileName);
    if (pvImageBase == NULL) {
        printf_s("scg: cannot load input file, GetLastError %lu\r\n", GetLastError());
        return;
    }

    __try {

        FileHeader = (PIMAGE_FILE_HEADER)RtlOffsetToPointer(pvImageBase,
            ((PIMAGE_DOS_HEADER)pvImageBase)->e_lfanew + sizeof(DWORD));

        switch (FileHeader->Machine) {

        case IMAGE_FILE_MACHINE_I386:

            oh.oh32 = (PIMAGE_OPTIONAL_HEADER32)RtlOffsetToPointer(FileHeader,
                sizeof(IMAGE_FILE_HEADER));

            ExportDirectory = (PIMAGE_EXPORT_DIRECTORY)RtlOffsetToPointer(pvImageBase,
                oh.oh32->DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress);

            break;

        case IMAGE_FILE_MACHINE_ARM64:
        case IMAGE_FILE_MACHINE_AMD64:

            oh.oh64 = (PIMAGE_OPTIONAL_HEADER64)RtlOffsetToPointer(FileHeader,
                sizeof(IMAGE_FILE_HEADER));

            ExportDirectory = (PIMAGE_EXPORT_DIRECTORY)RtlOffsetToPointer(pvImageBase,
                oh.oh64->DataDirectory[IMAGE_DIRECTORY_ENTRY_EXPORT].VirtualAddress);

            break;

        default:
            printf_s("scg: unexpected image machine type %lx\r\n", FileHeader->Machine);
            break;
        }

        if (ExportDirectory) {

            NameTableBase = (PULONG)RtlOffsetToPointer(pvImageBase, (ULONG)ExportDirectory->AddressOfNames);
            NameOrdinalTableBase = (PUSHORT)RtlOffsetToPointer(pvImageBase, ExportDirectory->AddressOfNameOrdinals);
            FunctionsTableBase = (PULONG)RtlOffsetToPointer(pvImageBase, ExportDirectory->AddressOfFunctions);

            for (entryIndex = 0; entryIndex < ExportDirectory->NumberOfNames; entryIndex++) {

                ProcessExportEntry(
                    RtlOffsetToPointer(pvImageBase, FunctionsTableBase[NameOrdinalTableBase[entryIndex]]),
                    RtlOffsetToPointer(pvImageBase, NameTableBase[entryIndex]),
                    dllType,
                    FileHeader->Machine);

            }
        }
    }
    __finally {
        UnmapViewOfFile(pvImageBase);
        if (AbnormalTermination()) {
            printf_s("Unexpected exception occurred\r\n");
        }
    }
}

/*
* main
*
* Purpose:
*
* Program entry point.
*
*/
int main()
{
    ULONG paramLength = 0;
    WCHAR szInputFile[MAX_PATH + 1];
    PVOID pvEnable = (PVOID)1;

    HeapSetInformation(GetProcessHeap(), HeapEnableTerminationOnCorruption, &pvEnable, sizeof(pvEnable));

    RtlSecureZeroMemory(szInputFile, sizeof(szInputFile));
    GetCommandLineParam(GetCommandLine(), 1, szInputFile, MAX_PATH, &paramLength);
    if (paramLength > 0) {
        ParseInputFile((LPCWSTR)szInputFile);
    }
    else {
        printf_s("Syscall Generator (NTOS/WIN32K/IUM)\r\nSupports ntdll/win32u/iumdll dlls as targets\r\nUsage: scg filename\r\n");
    }

    return 0;
}
