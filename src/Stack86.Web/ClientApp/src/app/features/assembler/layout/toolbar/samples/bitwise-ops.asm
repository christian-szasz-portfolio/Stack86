; Bitwise operations demo
; Demonstrates AND, OR, XOR, NOT, SHL, SHR

MOV AX, 0xFF00   ; 1111111100000000b
MOV BX, 0x0F0F   ; 0000111100001111b

; AND — keeps only bits set in both
MOV CX, AX
AND CX, BX       ; CX = 0x0F00

; OR — sets bits from either
MOV DX, AX
OR  DX, BX       ; DX = 0xFF0F

; XOR — toggles bits
MOV AX, 0xAAAA
XOR AX, 0x5555   ; AX = 0xFFFF

; NOT — inverts all bits
MOV BX, 0x00FF
NOT BX            ; BX = 0xFF00

; Shift left (multiply by 4)
MOV AX, 10
SHL AX, 1        ; AX = 20
SHL AX, 1        ; AX = 40

; Shift right (divide by 2)
MOV AX, 100
SHR AX, 1        ; AX = 50

HLT
