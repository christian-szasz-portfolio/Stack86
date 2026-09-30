/**
 * INT 86h service numbers for standard library function emulation.
 * These match the service numbers assigned in the compiler's CLibraryRegistry.
 */
export enum Int86Service {
  Sleep = 0x01,
  Strlen = 0x02,
  Strcpy = 0x03,
  Strcmp = 0x04,
  Memset = 0x05,
  Rand = 0x06,
  Srand = 0x07,
  Atoi = 0x08,
  Strcat = 0x09,
  Memcpy = 0x0A,
  Toupper = 0x0B,
  Tolower = 0x0C,
  Getchar = 0x0D,
  Exit = 0x0E,

  // string.h extended
  Strncpy = 0x0F,
  Strncmp = 0x10,
  Strncat = 0x11,
  Strchr = 0x12,
  Strrchr = 0x13,
  Strstr = 0x14,
  Memcmp = 0x15,

  // ctype.h extended
  Isalpha = 0x16,
  Isdigit = 0x17,
  Isalnum = 0x18,
  Isspace = 0x19,
  Isupper = 0x1A,
  Islower = 0x1B,
  Ispunct = 0x1C,
  Isprint = 0x1D,
  Isxdigit = 0x1E,
  Iscntrl = 0x1F,

  // stdlib.h heap
  Malloc = 0x20,
  Free = 0x21,
  Calloc = 0x22,
  Realloc = 0x23,

  // stdlib.h conversion
  Itoa = 0x24,
  Strtol = 0x25,

  // time.h
  Time = 0x26,
  Clock = 0x27,
  Difftime = 0x28,

  // stdio.h input (scanf support)
  ReadInt = 0x29,
  ReadStr = 0x2A,
}
