### Warning!
While I have been given permission to post it by the creator who made it on my behalf, this is not specifically made by me, I don't do Unity scripting.  This means i cannot currently guarantee any fixes or updates, it's provided as is.

**While it may work on other versions it's been designed to work on later versions of Unity 2019.**

---

## What is Squeak?

Squeak is a tool made by VRChat user and awesome human Gongo that bridges the gap between level editing in Blender to Unity by providing a way to procedurally instance prefabs and other GameObjects based on the instances or triangles that make up a mesh asset.

Drag a mesh asset into the Hierarchy, add Squeak, tweak a few settings and you're done!


#### Features

* Use either Instances or right angle triangle faces to generate prefab instances
* Provide multiple prefabs and Squeak will randomly pick from the list as it instances
* The auto-update function allows Squeak to automatically redo the instance generation as soon as it detects a change in the original model file.
* If you're using Instances, you can target objects with specific names in the file as well as what transform data is used when instancing.
* If you're using Face mode, it can create tens of thousands of instances quickly.

#### Installation Instructions

* Unzip the folder.
* Drag the folder anywhere in your project.
* Right-click a GameObject in the hierarchy and select, "Squeak" to start using it.


## Why Though?


I've been using Unity for almost two years for VRChat projects and it's surprisingly terrible for a lot of things, but level design is a particular failing of the engine - ProBuilder is only suitable for lo-fi aesthetics and prototyping mechanical design, the engine is buggy and unstable and will fail on you for unexpected reasons, and making it usable in any particular field requires either programming knowledge and time investment or buying a bunch of plugins and hoping the connections between them are strong enough to survive an entire project cycle.

So instead of putting up with that I do my environment design in Blender and use Capsule plus a custom Geometry Node toolset to sketch out designs and import them effortlessly into Unity.  It means I can pull off a a lot more interesting designs more quickly and my investment into particular skills or tools isn't limited to weird and isolated Unity plugins as Blender is such a universally important app.

But there's still the question of prefab instancing as they're important for things like interactivity and performance optimizations which is where this addon fills the gap.  This addon in particular was born out of trying to re-create the grass and foliage instancing system seen in Genshin Impact but by moving the level design to Blender instead.







