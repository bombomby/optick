.model flat
@PushAllRegisters MACRO
									push eax
									push ebx
									push ecx
									push edx
									ENDM

@PopAllRegisters  MACRO
									pop edx
									pop ecx
									pop ebx
									pop eax
									ENDM

@FunctionBody MACRO index
							_HookFunctionASM&index proc
								@PushAllRegisters
								call _NextEvent																	;Check wheter profiler is active and create new EventData
								cmp eax, 0																			;Check for null
								je NotActiveProlog&index												;Jump if Profiler is not active

								mov ebx, eax																		;Store EventData in ebx
								call _GetTime																		;GetTime 
								mov dword ptr[ebx], eax													;StartTime
								mov eax, [_hookSlotData + index * 10h + 12h]			;Store description
								mov dword ptr[ebx + 8h], eax										;EventDescription
								mov [_hookSlotData + index * 10h + 8h], ebx			;StoreActive EventData

							NotActiveProlog&index:
								@PopAllRegisters

								pop  [_hookSlotData + index * 10h + 0h]					;Store return address
								call [_hookSlotData + index * 10h + 4h]					;Call original function
								push [_hookSlotData + index * 10h + 0h]					;Restore return address

								cmp [_hookSlotData + index * 10h + 8h], 0				;Check for active EventData
								je NotActiveEpilog&index												;If not active go to return
					
								@PushAllRegisters
								call _GetTime																		;Get Finish Time
								mov ebx, [_hookSlotData + index * 10h + 8h]			;Store EventData address
								mov [ebx + 8h], eax															;Store FinishTime
								@PopAllRegisters

							NotActiveEpilog&index:
								ret
							_HookFunctionASM&index endp
							ENDM

.data
extern _NextEvent : proc
extern _GetTime : proc
extern _hookSlotData : dword
; [hookSlotData + 0h ] - Return Address
; [hookSlotData + 4h ] - Original Function Address
; [hookSlotData + 8h ] - EventData
; [hookSlotData + 12h] - EventDescription

.code
@FunctionBody 0
@FunctionBody 1
@FunctionBody 2
@FunctionBody 3
@FunctionBody 4
@FunctionBody 5
@FunctionBody 6
@FunctionBody 7
@FunctionBody 8
@FunctionBody 9
@FunctionBody 10
@FunctionBody 11
@FunctionBody 12
@FunctionBody 13
@FunctionBody 14
@FunctionBody 15
@FunctionBody 16
@FunctionBody 17
@FunctionBody 18
@FunctionBody 19
@FunctionBody 20
@FunctionBody 21
@FunctionBody 22
@FunctionBody 23
@FunctionBody 24
@FunctionBody 25
@FunctionBody 26
@FunctionBody 27
@FunctionBody 28
@FunctionBody 29
@FunctionBody 30
@FunctionBody 31
@FunctionBody 32
@FunctionBody 33
@FunctionBody 34
@FunctionBody 35
@FunctionBody 36
@FunctionBody 37
@FunctionBody 38
@FunctionBody 39
@FunctionBody 40
@FunctionBody 41
@FunctionBody 42
@FunctionBody 43
@FunctionBody 44
@FunctionBody 45
@FunctionBody 46
@FunctionBody 47
@FunctionBody 48
@FunctionBody 49
@FunctionBody 50
@FunctionBody 51
@FunctionBody 52
@FunctionBody 53
@FunctionBody 54
@FunctionBody 55
@FunctionBody 56
@FunctionBody 57
@FunctionBody 58
@FunctionBody 59
@FunctionBody 60
@FunctionBody 61
@FunctionBody 62
@FunctionBody 63
@FunctionBody 64
@FunctionBody 65
@FunctionBody 66
@FunctionBody 67
@FunctionBody 68
@FunctionBody 69
@FunctionBody 70
@FunctionBody 71
@FunctionBody 72
@FunctionBody 73
@FunctionBody 74
@FunctionBody 75
@FunctionBody 76
@FunctionBody 77
@FunctionBody 78
@FunctionBody 79
@FunctionBody 80
@FunctionBody 81
@FunctionBody 82
@FunctionBody 83
@FunctionBody 84
@FunctionBody 85
@FunctionBody 86
@FunctionBody 87
@FunctionBody 88
@FunctionBody 89
@FunctionBody 90
@FunctionBody 91
@FunctionBody 92
@FunctionBody 93
@FunctionBody 94
@FunctionBody 95
@FunctionBody 96
@FunctionBody 97
@FunctionBody 98
@FunctionBody 99
@FunctionBody 100
@FunctionBody 101
@FunctionBody 102
@FunctionBody 103
@FunctionBody 104
@FunctionBody 105
@FunctionBody 106
@FunctionBody 107
@FunctionBody 108
@FunctionBody 109
@FunctionBody 110
@FunctionBody 111
@FunctionBody 112
@FunctionBody 113
@FunctionBody 114
@FunctionBody 115
@FunctionBody 116
@FunctionBody 117
@FunctionBody 118
@FunctionBody 119
@FunctionBody 120
@FunctionBody 121
@FunctionBody 122
@FunctionBody 123
@FunctionBody 124
@FunctionBody 125
@FunctionBody 126
@FunctionBody 127
end