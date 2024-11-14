# DSDeaths

## Purpose

This is an automatic death counter for FromSoftware games. It keeps reading your current death count from RAM while the game is running and writes it to a file when it changes. A sample use case is displaying your death count on stream using a Text Source in OBS Studio reading from the created file.
The death count is not reset when you enter NG+.

## Which games are supported?

 * DARK SOULS: Prepare To Die Edition
 * DARK SOULS II
 * DARK SOULS II: Scholar of the First Sin
 * DARK SOULS III
 * DARK SOULS: REMASTERED
 * Sekiro: Shadows Die Twice
 * Elden Ring (offline, disable EAC)

 Note that only the current patch as of the time of release works. Please open a ticket if there is a new patch and it stops working.

## Elden Ring support

Elden Ring uses Easy Anti-Cheat to detect and deny trying to read from the process memory. Use your favorite search engine to find out how to disable EAC to play offline.

## How do I use it?

Just double click it. It writes the current death count into `DSDeaths.txt` in the current directory.
