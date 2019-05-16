# ThingsBeLikeBot

This is a bot that posts randomly generated memes on Twitter, always with the same format.

Works by using the Wordnik API to get a random word, then uses Google Customsearch to search for a related image and groups everything together with OpenGL, drawing everything into a renderbuffer, reading the pixels into a bitmap and saving it as JPG to be posted on twitter (using of course, the Twitter API).

Made in C# for Windows

## Libraries used
OpenGL Bindings - [OpenTK](https://github.com/opentk/opentk)

OpenTK Text rendering - [QuickFont](https://github.com/opcon/QuickFont)

Google Image Searching - [Google Customsearch](https://developers.google.com/custom-search/)

Twitter API for C# - [TweetSharp](https://www.nuget.org/packages/TweetSharp/)

Google Customsearch is configured to search the entire internet and has no website set so search specifically.

## What could be improved:
* TweetSharp is an old library. Updating to a newer one or using the Twitter API directly might be a much better idea than relying on this unmantained library.
* Using GDI instead of OpenGL to draw the images. This will yield better quality specially for the text rendering, where a current issue is that some letters have parts cut out and the kerning is wrong since QuickFont thinks it's better that everone so it calcualtes its own kerning information rather than use the one present in the TrueTypeFont file. This change might also give some nice performance benefits.

## How it runs
The Visual Studio 2017 solution contains two projects:
* ThingsBeLikeBot: designed to run on the background of the computer, this .exe runs automatically on startup (This was done by placing it on the Startup folder) and every so often runs a function that checks whether it should post again. Has code-configurable values for time between posts and posts per day. Also adds an Icon to the botton-right tray and on click opens a menu that gives you options to force post, check posting status (shows post today and when next one is coming) and another option for stopping the bot. When it decides to post, it runs the other project, ImageCreator.
* ImageCreator: Since it's pretty inefficient to have all of this constantly loaded in the background of your computer, this separate .exe does all the image work. This project manages the different APIs to create and post an image. This process' exit code is checked by ThingsBeLikeBot to warn you if any exception happens.

The ImageCreator project also has a logging mechanism that logs all that happens into a file. This can be disabled by removing the LOG_DATA compilation symbol. It is intended for bug testing.
ThingsBeLikeBot also currently shows a notification when a post is succesfully made, this is also temporary to check everything is working properly.

ImageCreator.exe has to be placed in %appdata%/ThingsBeLikeBot along with all it's required files (dlls and data folder, pretty much everything that goes out into the bin folder when you build the solution)
ThingsBeLikeBot.exe is supposed to be run on startup. I placed mine on the Startup folder. Note that this .exe will load it's icon file and read&write the botdata file from the %appdata%/ThingsBeLikeBot folder too!
