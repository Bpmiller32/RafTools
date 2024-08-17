; Define the executable name
exeName := "ms-teams.exe"

; Set the title match mode
SetTitleMatchMode(2)

try {
    ; Find the window by executable name
    windowID := WinGetID("ahk_exe " exeName)

    ; If the window is found, activate it
    if (windowID) {
        WinActivate(windowID)
    } else {
        throw Error("Window not found")
    }
} 

; Sleep for 5 seconds
Sleep(5000)

; Attempt to close the window
try {
    if (windowID) {
        WinClose(windowID)
    } else {
        ; If no window was found earlier, try to close any window with the same executable name
        WinClose("ahk_exe " exeName)
    }
}