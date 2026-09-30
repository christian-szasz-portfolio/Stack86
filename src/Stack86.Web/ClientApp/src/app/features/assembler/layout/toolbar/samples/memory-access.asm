; Memory read/write demo
; Stores values in memory and reads them back

; Write values to memory
MOV AX, 0x1234
MOV WORD PTR [0x200], AX    ; store 1234h at address 200h

MOV AX, 0x5678
MOV WORD PTR [0x202], AX    ; store 5678h at address 202h

; Read them back into different registers
MOV BX, [0x200]             ; BX = 1234h
MOV CX, [0x202]             ; CX = 5678h

; Swap via memory
MOV WORD PTR [0x300], BX
MOV WORD PTR [0x302], CX
MOV BX, [0x302]             ; BX = 5678h
MOV CX, [0x300]             ; CX = 1234h

; Sum them
ADD BX, CX                  ; BX = 68ACh

HLT
