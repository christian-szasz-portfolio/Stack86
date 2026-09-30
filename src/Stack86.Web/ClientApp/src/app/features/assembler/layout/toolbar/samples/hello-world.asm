; Print "Hello!" using INT 21h AH=02h
; AH=02h: print character in DL

MOV AX, 0x0200  ; AH=02
MOV DX, 72      ; 'H'
INT 0x21

MOV DX, 101     ; 'e'
INT 0x21

MOV DX, 108     ; 'l'
INT 0x21

MOV DX, 108     ; 'l'
INT 0x21

MOV DX, 111     ; 'o'
INT 0x21

MOV DX, 33      ; '!'
INT 0x21

HLT
