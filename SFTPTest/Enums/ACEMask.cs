﻿namespace SFTPTest.Enums;

[Flags]
public enum ACEMask : uint
{
    ACE4_READ_DATA = 0x00000001,
    ACE4_LIST_DIRECTORY = 0x00000001,
    ACE4_WRITE_DATA = 0x00000002,
    ACE4_ADD_FILE = 0x00000002,
    ACE4_APPEND_DATA = 0x00000004,
    ACE4_ADD_SUBDIRECTORY = 0x00000004,
    ACE4_READ_NAMED_ATTRS = 0x00000008,
    ACE4_WRITE_NAMED_ATTRS = 0x00000010,
    ACE4_EXECUTE = 0x00000020,
    ACE4_DELETE_CHILD = 0x00000040,
    ACE4_READ_ATTRIBUTES = 0x00000080,
    ACE4_WRITE_ATTRIBUTES = 0x00000100,
    ACE4_DELETE = 0x00010000,
    ACE4_READ_ACL = 0x00020000,
    ACE4_WRITE_ACL = 0x00040000,
    ACE4_WRITE_OWNER = 0x00080000,
    ACE4_SYNCHRONIZE = 0x00100000,
}