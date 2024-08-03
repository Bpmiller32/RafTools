; Define the executable name to search for
exeName := "ms-teams.exe" ; Replace with the executable name of the window you want to close

; Check if the window associated with the executable is open
if WinExist("ahk_exe " exeName)
{
    ; If the window is found, close it
    WinClose("ahk_exe " exeName)
}