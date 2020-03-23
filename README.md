# NodePyUtil
A C# WinForms(.NET Framework 4.6.1) GUI utility for interfacing with the NodeMCU (ESP8266) running MicroPython.
Sorry for the sloppy code - if you would like to contribute to this project/clean it up, please do not hesitate!
I made this quick & dirty project as an alternative to the [Adafruit MicroPython Tool](https://github.com/scientifichackers/ampy).

## Installation
I made the project in Visual Studio 2019, so it's best to open it with that and either run it from there or build it and use the standalone executable file.

## Usage
You simply open the device in the 'Port Settings' tab with the specified settings.
Right-click files/folders to see all available operations.
If you double-click a file, it will download the file and open it with the default program for that file. Additionally, any time you write to the opened file it will automatically re-upload the changed file to the board.

## Credits
TinyJSON - https://github.com/gering/Tiny-JSON

![Screenshot](https://github.com/tottaka/NodePyUtil/blob/master/Annotation%202020-03-23%20195617.png)
