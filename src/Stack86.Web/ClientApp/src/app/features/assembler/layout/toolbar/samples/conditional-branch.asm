; Conditional branching demo
; Finds the maximum of three numbers

MOV AX, 42       ; first number
MOV BX, 99       ; second number
MOV CX, 17       ; third number

; Compare AX and BX
CMP AX, BX
JGE check_cx     ; if AX >= BX, skip
MOV AX, BX       ; AX = max(AX, BX)

check_cx:
; Compare AX and CX
CMP AX, CX
JGE done         ; if AX >= CX, we're done
MOV AX, CX       ; AX = max(AX, CX)

done:
; AX = 99 (the largest)
MOV DX, AX       ; store result in DX
HLT
