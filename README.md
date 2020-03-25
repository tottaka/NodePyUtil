# NodePyUtil
A C# WinForms(.NET Framework 4.6.1) GUI utility for interfacing with the NodeMCU (ESP8266) running MicroPython.
Sorry for the sloppy code - if you would like to contribute to this project/clean it up, please do not hesitate!
I made this quick & dirty project as a proof of concept alternative to the [Adafruit MicroPython Tool](https://github.com/scientifichackers/ampy), and I use it for my own personal use. This project is meant to only be used as a base/learning experience for your own version, but I will keep the repo updated with bug fixes that are found.

## Installation
I made the project in Visual Studio 2019, so it's best to open it with that and either run it from there or build it and use the standalone executable file.

## Usage
You simply open the device in the 'Port Settings' tab with the specified settings.
Right-click files/folders to see all available operations.
If you double-click a file, it will download the file and open it with the default program for that file. Additionally, any time you write to the opened file it will automatically re-upload the changed file to the board.

![Screenshot](https://github.com/tottaka/NodePyUtil/blob/master/screenshots/window.png)
![Screenshot](https://github.com/tottaka/NodePyUtil/blob/master/screenshots/repl-window.png)
