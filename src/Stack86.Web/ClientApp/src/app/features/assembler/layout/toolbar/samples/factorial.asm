; Factorial of 5 (5! = 120)
; Uses MUL and a loop

MOV CX, 5         ; n = 5
MOV AX, 1         ; accumulator = 1

fact_loop:
  MUL CX          ; AX = AX * CX
  DEC CX
  JNZ fact_loop

; AX = 120 (0x0078)
MOV BX, AX        ; save result in BX
HLT
