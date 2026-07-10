# About

A SourceEngine-type developer console for executing commands, and setting variables.
This tool can be used in the Editor and in **runtime builds**.

<img width="570" height="370" alt="Unity_fdyTyOT9sC" src="https://github.com/user-attachments/assets/9f00b6e1-0240-4c78-9f82-465a6e7a4cf7" />

# Installation
Install this package by opening the package  manager and installing by git url and entering the following:

```
https://github.com/FarbodNejati/SourceDeveloperConsole.git
```

### Requirements:
* Unity 2021.3 or newer (Up to date for Unity 6000)
* UIElements (built-in unity package)

---
# Quick Start
The Built-In editor window can be accessed from `Toolbar > Tools > Developer Console`.

A pre-built, **ready to use** Runtime Console prefab is available inside the "Example Runtime Console" sample which can be installed from the package manager, or found in the `Samples~` folder of the repository.

## Registering Commands
You can register a method command by adding the `ConsoleMethod` attribute to a **static** method. example:
```cs
[ConsoleMethod("pow", "raise a to the power of b.")]
public static int CalculatePower(int a, int b){
    //The return object will be printed onto the console gui
    return (int)Math.Pow(a, b);
}
```


## Registering Console-Variables
You can register a variable command by adding the `ConsoleVariable` attribute to a static Field or Property, example:
```cs
// You can get/set fields through commands
[ConsoleVariable("coin", "directly set the player's coin count")]
public static int Coins = 200;
```
Property example with non static backing fields:
```cs
//Control properties with backing fields. the backing field can be on an object instance, while the property is static.
private int gameDifficulty = 0;
[ConsoleVariable("difficulty", "the difficulty index of the game. (0 to 3)")]
public static int GameDifficulty
{
    get{
        if (singleton_instance == null)
            throw new Exception("No GameManager instance is present");
        return singleton_instance.gameDifficulty;
    }
    set
    {
        if (singleton_instance == null)
            throw new Exception("No GameManager instance is present");
        if (value < 0 || value > 3)
            throw new ArgumentOutOfRangeException(nameof(value));
        singleton_instance.gameDifficulty = value;
    }
}
```


## Runtime User-Interface
You can use the provided `DefaultDeveloperConsole` element within your own menus, and apply your own custom styling.
This is the same UXMLElement used in the built-in editor window, and the element in use in my own games (just with custom styling for a unique look).

In-Game Example: https://youtu.be/OMn8B_0SZeE?si=4nUra9B3Bkx-hF2R

You can make your own custom GUI using UGUI or UIToolkit or.. anything else!
Take a look at [DefaultDeveloperConsole.cs](https://github.com/FarbodNejati/SourceDeveloperConsole/blob/main/Scripts/Core/UXMLElements/DefaultDeveloperConsole.cs) to see how to implement your own custom GUI.

## How It Works
* The static `DeveloperConsole` class is the one that executes the commands and has the callbacks for logging things to the console.

* The `CommandParser` class is in charge of parsing inputted strings into usable commands and arguments, which are then executed by the `DeveloperConsole`

* And the `ConsoleSuggestionHandler` class is for providing auto-completion suggestions, and command usage hints as you type, that tells you what arguments to type, and helps you see if your input is valid or not by color coding the hint text. (exaples: command names, enum types, boolean true/false)


---

* Created by [Farbod Nejati](https://github.com/FarbodNejati)
* Inspired by [ZeroByter](https://github.com/ZeroByter/SourceConsole/tree/master)
