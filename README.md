# **TODO: The Great Rewrite**
I think every one of these parsers that I've seen has gone through or is in the process of a rewrite.
I have rewritten just about everything in the main section of the parser, and I will make a new exe with new functionalities soon™.
> *I'll look into it at some point when humidity and air pressure are favorable to such endeavors.*


# UncraftedName's Demo Parser
Used to parse source demos.
I stole most of the parser code from https://github.com/NeKzor/sdp.js and https://nekzor.github.io/dem#netsvc-message
(thanks nekz)

As an exe, this functions similiarly to listdemo+:
![](github-resources/example-usage.gif) \
In addition it can be run from the command line with a bunch of other options such as jump detection, verbose demo dumping and a few other things.
![](github-resources/console-usage.gif) \
Note: this incorrectly prints the tick timing - it does not include the 0th tick in the demo. This is to stay consistent with the existing tools such as listdemo+.
