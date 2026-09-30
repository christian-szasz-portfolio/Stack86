; Subroutine call demo
; Doubles the value in AX using a CALL/RET pair

MOV AX, 21       ; value to double
CALL double_ax   ; call subroutine
MOV BX, AX       ; BX = 42

MOV AX, 100
CALL double_ax
MOV CX, AX       ; CX = 200

HLT

double_ax:
  ADD AX, AX     ; AX = AX * 2
  RET
