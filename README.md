# ggg_dialogue_viewer

== GGG DIALOGUE VIEWER ===
Author: fiirewolvar

1) How to use
In order to use the viewer, you need to have the game, as it pulls dialogue from extracted asset files (which I'm unsure whether I'm legally allowed to provide along with the viewer).
Right now, it doesn't extract the dialogue tree assets from the compiled files by itself, so you will have to do this manually.
The way I would recommend doing this is as follows:
- First, download UABE: https://github.com/SeriousCache/UABE
- In UABE, go to File -> Open and then select the following files found in your copy of GGG's base directory: globalgamemanagers.assets, resources.assets
- Click on resources.assets in the left pane
- Sort the data that is shown in the right pane by Type and find all the MonoBehaviour : DialogueGraph.DialogueGraph_SO assets
- Ctrl + click on these to select all of them (or just the desired ones) then click Export Raw on the right side of the window
- Export these assets into the dialogues folder in the GGG dialogue viewer's base directory
- If this folder doesn't exist, create a folder named "dialogues" in the same directory as the viewer's .exe, and put these files in there

If anyone happens to know of how to extract the compiled dialogue trees from resources.assets without using UABE feel free to contact me - 
I might keep working on it myself anyway, since it would make the program much easier to use.

2) About the viewer
The left-hand pane contains a list of the files in /dialogues/. Select a file to load and open it.
This should load all separate conversations with that character into the right pane.
Double click on a line (dialogue node) to load nodes that follow it. If there are multiple possible nodes, all nodes will be shown.
If a specific dialogue node is able to be reached via multiple paths in a conversation, only the first instance of this node in the conversation will have the nodes following it loaded.
(This is to avoid infinite loops breaking the program, and might be changed in the future as it mainly only happens because I was lazy with checking for them.)

Info shown in a specific line is as follows:
<condition>: ID <ID> | C: <code> | <text> | <END/NEXT>
With different parts left out if not relevant.

<condition> - any conditions to show this line of dialogue
<ID> - this dialogue's ID in the character's dialogue tree
<code> - string for line of code associated with this dialogue, if any
<text> - the text of the line
<END/NEXT> - END if this node ends the conversation, NEXT followed by ID/s of next node/s if the node loops back to a different one in the same conversation

At the end of all conversations with a character, dev notes found in their dialogue files are marked by DEV NOTE.
Unfortunately they don't have associated dialogue nodes, so they can't be automatically associated with specific lines, but they might still be interesting.