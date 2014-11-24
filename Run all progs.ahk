;
; AutoHotkey Version: 1.x
; Language:       English
; Platform:       Win9x/NT
; Author:         A.N.Other <myemail@nowhere.com>
;
; Script Function:
;	Template script (you can customize this template by editing "ShellNew\Template.ahk" in your Windows folder)
;

#NoEnv  ; Recommended for performance and compatibility with future AutoHotkey releases.
SendMode Input  ; Recommended for new scripts due to its superior speed and reliability.
SetWorkingDir %A_ScriptDir%  ; Ensures a consistent starting directory.

SetTitleMatchMode 2
Process, Exist, AlwaysOnTopMaker.exe
if ErrorLevel = 0
	Run AlwaysOnTopMaker.exe
Run, D:\Program Files (x86)\Opwekking\OPS 7.1\OPS Presenter.exe
WinWait, Presenter
WinRestore, Presenter
WinMove, Presenter 1300, 0
WinMaximize, Presenter
IfWinNotActive, Presenter, , WinActivate, Presenter, 
WinWaitActive, Presenter, 
Send, ^!t
Send, {F2}
Run, C:\Users\Aron List\Documents\ops.vmix
WinWait, vMix,
IfWinNotActive, vMix, , WinActivate, vMix, 
WinWaitActive, vMix, 
Sleep, 100
MouseClick, right,  132,  700
Sleep, 100
MouseClick, left,  176,  710
Sleep, 100
WinWait, Desktop Capture, 
IfWinNotActive, Desktop Capture, , WinActivate, Desktop Capture, 
WinWaitActive, Desktop Capture, 
Send +{TAB 7}
Send, {Down},
SetTitleMatchMode, Slow
Loop
{
	IfWinExist, Desktop Capture, OPS Live,
	{
	Send, {Enter}
	break
	}
	Send, {Down}
}
SetTitleMatchMode, Fast
run httpget.exe