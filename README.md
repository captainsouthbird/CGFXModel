# CGFXModel
An attempt at being able to edit ACNL villagers and maybe some other stuff.


## **Usage of the Command Line Utility**

The tool currently is used one of two ways:

`CGFXConverter export model.bcres edited-model.ms3d`

This will export a BCRES / CGFX model file named `model.bcres` to a MilkShape 3D file named `edited-model.ms3d` along with a dump of all textures as appropriately named PNG files.


`CGFXConverter import model.bcres edited-model.ms3d model-out.bcres`

This will import a MilkShape 3D file named `edited-model.ms3d` into `model-out.bcres`. It will also read any appropriately named PNG files in the same folder and import them if available. (If you do not want to change textures, you can omit them; the original base model textures are used instead.)

Additionally in this mode, you also supply the original, unmodified file `model.bcres` which is used as "backing" data, i.e. the golden source of all important data, especially data that is NOT part of the MilkShape 3D file. It is currently prohibited that your output filename is the same as the base filename mostly to prevent accidental overwriting. In the future I may lift this restriction or allow the output to also act as the base.


## **What It Is**

**CGFXModel** is a WIP utility to allow actually editing in-game models using the BCRES / CGFX format. Specifically my interest is with Animal Crossing: New Leaf. It *may* work for other games, but I'm not specifically targeting them at this time.

This tool was written by gaining understanding of the BCRES / CGFX format from Ohana3DS ([original](https://github.com/dnasdw/Ohana3DS) and [Rebirth](https://github.com/gdkchan/Ohana3DS-Rebirth)) as well as from [SPICA](https://github.com/gdkchan/SPICA).

I didn't want to just change existing data, but be free to edit the geometry, within understandable limits. This utility understands how to read and write the file agnostic to its original structure, i.e. it will calculate offsets and be able to freely reposition model and texture data as needed.


## **Limitations**

First and foremost, remember that a 3DS is not a PC. It has some pretty restrictive internal limits because of the nature of its hardware. Do not expect to import photorealistic model meshes and everything will be OK. I've already hit some limits by having just *slightly* too complex geometry that crashed the game on start. (Though unfortunately I couldn't link it to any specific amount of what type of geometrical data was "too much.") The file was "valid" as far as viewers were concerned, but the game wasn't taking it!

**Conversion Format:** Right now this tool only converts to/from MilkShape 3D's format. I know this is a bit of bizarre choice especially since it's outdated software, but I did so because MilkShape is also very constrained and gives a reasonable expectation of what you can actually use my tool for. In the future, something more modern/common like DAE would be good, so long as it can observe limits. MilkShape can import/export some formats so it could possibly be an in-between tool.

**Skeleton:** Editing the skeleton of the model is not supported and you can't do that in MilkShape (beyond assigning vertices), but since ACNL villagers share animations, you wouldn't want to do that anyway. It either wouldn't work right or might even crash the game! You CAN assign vertices to the skeleton for use with new geometry, however!

**Meshes:** As the models are made up of one or more meshes, MilkShape's "group" construct works as a good translation of the concept. The conversion to the MS3D format assigns textures ("materials") but currently you cannot change what texture/material is assigned to the mesh/group. (It's ignored converting back to the BCRES / CGFX.) This is usually not much of a big deal since you can change the texture itself. I currently do not support adding NEW meshes. I'm actually not sure how ACNL would handle additional meshes. It might work fine or might crash. Would be interesting to experiment here. Basically just merge all your new geometry in with one of the existing groups/meshes. You can however still use Groups in a useful way in MilkShape. Simply following the naming convention of which mesh your group belongs to and they will be merged. For example, "mesh0", "mesh0-top", "mesh0-dongle" will all be merged into Mesh Index 0. 

**Triangles Only:** BCRES / CGFX models CAN have other geometry besides raw triangles (e.g. triangle fans, triangle strips, etc.) but currently I'm only supporting raw triangles. This doesn't seem to be a problem with ACNL on any model I've seen so far, however.


## **What You CAN Do (with ACNL villagers at least)**

You can add/edit/delete vertices, and you can add/edit/delete faces [triangles.] You can also assign new vertices to EXISTING bones. Externally you can also change textures (by altering the PNGs as noted above.) The tool currently still restricts them to be the same size as the original. (As I'm rewriting the file from scratch, I could experiment with allowing texture size changes. Not sure how the game will tolerate it, but might be fine!)


## **More on the Development**

My goal is to be able to edit the physical (geometrical) appearance of ACNL characters as well as the Player. It may currently or eventually support editing some other models too. I do not currently intend for this to be a general purpose converter for the entire BCRES / CGFX spectrum, but it could grow. (Definitely if you're development savvy and want to help expand this tool I'm interested.)

The CGFXModel underpinnings were developed by examining how Ohana3DS and SPICA loaded data out of the BCRES / CGFX file and creating *just enough* support to get it to load BCRES / CGFX files that contain "Model" and "Texture" data. (Suitable for most villager models as their animation data is common and external to them.) Other types of data that can be stored (e.g. animations) are currently not supported. SPICA has a lot more support for loading this additional data and if necessary I could import more functionality by further examining its code.

While SPICA does have a lot more support for the greater potential of the file format, I did not use it here because it currently cannot (as of this writing) write it back in a way that is usable by the game. This is mostly due to some errors in the binary serialization code it has. I imagine it could be corrected and eventually should be superior to my code, but I felt it would be faster for me to write my own purposed version than to try to fix the SPICA code. Maybe someday someone with the time to help fix up SPICA will get it closer to this goal. (In an object-oriented / .NET sense, SPICA's code is pretty nifty for what it strives to do.) I did borrow/adapt some of SPICA's code for things such as decoding / encoding the vertex stream.

I developed and tested my code against a set of three different species of villagers as well as a model out of the Player's set, and worked until the code wrote back a 1:1 version for each. Almost. The Player model has a single unknown, untracked 4-byte set of zeroes lurk, but these seem irrelevant. (I've edited and loaded this model into the game and it seemed to be fine.) But all the villagers, loaded into my classes but without "touching" their data, write back identical to the way they were read.

As a footnote, there is also a CGFXTestbed project included in the source, which is just where I ran tests to make sure I was getting byte-for-byte rewrite accuracy. If you're planning on trying to expand CGFXModel's support, this is a good place to make sure that you can load and save data identically. It also writes a log which includes notes for areas of the file it didn't read at all (useful to look for gaps in import code.)
