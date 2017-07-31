Welcome to the first public version of GlitchHelper!
This version is for review purposes only. Feel free to use it in private and tell me what needs improving, but don't go sharing it around just yet.
After the review period, it will be released under either GPL3.0, or AGPL3.0.

Here's a bunch of stuff I think is probably wrong with the program right now:

Generic Questions:
- I called the program "Glitch Helper" because that's what it's designed to do (help you glitch files), but in reality, a name more akin to "File Structure Editor" is more accurate. That said, I was also hoping I'd be able to come up with a name that has a funny/clever acronym, but as you can tell by the fact it's still called "Glitch Helper", I couldn't think of one.
- On both FormMain and HotfileManager I have a tab called "Edit", but I'm really not sure what I should put there/if I should even have it.
- I'm not sure how good my file/folder structure is, nor am I entirly confident that I'm using good naming conventions.
- How much should I be prioritzing readability over speed? For example: Throughout most (if not all) of the project I've refrained from using foreach loops, since I read that for loops are faster.
- The is one feature that is currently missing from this build, and that's the Header Editor. The reason it's missing is because I'm not entirly sure what the best way to go about making it is... Forcing each plugin to have it's own form would be too clunky, forcing each plugin to have its own set of buttons that would get added to a deticated form might be ok, or I could just have another DataGridView that plugins can add cells too. Any suggestions?

FormMain:
- I really think that I should be using data binding for displaying stuff, but I couldn't get it to work, so instead I'm using this other messy system...
- 868 - super messy that I have to give each plugin a copy of the selected cells for just ONE situation that not every plugin will use

HotfileManager
- 14 - wish that I could replace Form with FormMain, to simplify some stuff, but VS crashes whenever I try to return to the form in any way after making that change.

PluginLoader:
- Hopefully the fact that [this code](https://code.msdn.microsoft.com/windowsdesktop/Creating-a-simple-plugin-b6174b62/sourcecode?fileId=74454&pathId=1264648962) is licenced under Apache 2.0 won't cause any issues if I plan on using GPL/AGPL 3.0...
- Also, is this even a good way to be loading plugins?
- Plus, should I even have this in its own class? (Probably not?)

NetworkGraphics:
- I'm currently using a class (NetworkGraphic) to store all the data related to a png, but I'm not sure if I have to/should be. Should I just be using static lists or something?
- I'm pretty sure I have 3 or 4 different functions related to the calculation of the png's crc... 3:
- I think I'm safe to be using [crc code](https://www.w3.org/TR/PNG/D-CRCAppendix) adapted from the official png documentation...? [1.](https://www.w3.org/Consortium/Legal/2015/copyright-software-and-document)
- 129 - Supposed to be lists of all valid chunk types for the 3 different png types. I think I can turn these three long lists into to 4 short ones...? (Also, hopefully the list I've compiled is actually correct. 3:)
- 597 - Not sure if it's good practice to have a function reference an overload (or is it an overload referencing a function?).
- 1209 - Is this while loop a waste of space?
- 1247 - [Is code taken from stack exchange ok to use?](https://stackoverflow.com/questions/321370/how-can-i-convert-a-hex-string-to-a-byte-array)
- 1486 - I tried to refactor this, since I find it really messy that I have two lines that are exactly the same (e.CellStyle.BackColor = Color.White;), but I couldn't do it. Is there any way I didn't think of?
