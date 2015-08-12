Forays into Norrendrin
======================

A streamlined roguelike with deeply tactical combat.

<br>

[Read more about the game here.](http://forays.github.io/)

<br>

#### How to build the console version:

By default, the project will run in its own window, using
OpenTK. To make it run in a terminal, follow these steps:

1. Open ConsoleForays.csproj instead of opening the main
   Forays.sln or Forays.csproj, *or*, temporarily replace
   Forays.csproj with ConsoleForays.csproj and then open
   the main Forays.sln solution.
2. Change the first line of the Main method (in Main.cs)
   by uncommenting "Screen.GLMode = false;".
3. Compile!

