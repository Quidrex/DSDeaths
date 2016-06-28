# DSDeaths

## What is this and what is it good for?

This is a Dark Souls death counter. It keeps reading your current death count from RAM while Dark Souls is running and writes it to a file when it changes. A sample use case is displaying your death count on stream as both OBS Classic and OBS Studio support reading a text source from a file.

Side Note: The death count is not reset when you defeat Gwyn and enter NG+.

## How do I run it?

Syntax: `DSDeaths.exe <Prefix> <Filename>`

For example executing `DSDeaths.exe "Deaths: " "%USERPROFILE%\ds.txt"` will write "Deaths: 779" to the file `ds.txt` in your User folder when you die for the 779th time. The easiest way to run it is to create a shortcut and append the arguments to the field "Target".
