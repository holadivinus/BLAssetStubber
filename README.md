# Bonelab Asset Stubber
This is a "mod" for the bonelab/Marrow SDK. This has two features, "stubs" and Placer Previews.

### Placer Previews:
On save, the editor will refresh, detect existing Spawnable Crate Placers, and try to load in a preview for them.
This works by findng bonelab's "Addressables Catalog" and then loading the prefabs out of it.
You will need to let the Asset Stubber know where bonelabs is installed, otherwise it won't work.

Example Before & After Images:
![Before & After image](https://cdn.discordapp.com/attachments/875811073624784967/1208897422626127942/prevew.png?ex=65e4f475&is=65d27f75&hm=865793c2ef0e692c8894b3c135678ea0d285198d8d36c278baa46afcf5b4185c&)
(Blue outlines can be toggled off via toggle gizmos button)

### Asset Stubbing:
Use the "Tools>Stub Creation Wizard" Menu to find and create asset stubs.
Asset stubs can be: Prefabs, Materials, Textures, Meshes, Shaders, and Audioclips. (audioclips arent finished and wont work lol)
Asset stubs are fakeout assets, that are swapped in&out with their real counterparts on save.
This lets you see and use SLZ's assets, without any ripping.
![Asset Stubber Wizard GUI&example](https://cdn.discordapp.com/attachments/875811073624784967/1208901586068312164/pee.png?ex=65e4f856&is=65d28356&hm=f84f271a62386177e0185761a3e4f1f8adea119ba1d5b80ee7b56ee402beae9d&)

Problem: You need a mod to make these stubs swap to their real counterparts ingame.
I was working on making it work without a mod, buuuut I've kindof given up on that.
I've got a working "destubber" mod for bonelab I just need to release it

### Installation
1. Download the latest released .unitypackage
2. Drag & Drop it into your project
3. Press import
4. Ensure it can find bonelab, by clicking on Tools>Stub Creation Wizard. if it just loads up, you're good. Otherwise, you'll need to input the path to your bonelab folder.

remember saving refreshes everything and Asset Stubs are unfinished + unstable
mod stubbing/preview support maybe later lol