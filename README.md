# What is Sidekick?

Sidekick is a set of tools that allow you to edit fields, properties and invoke methods in deployed Unity projects on device and Unity editors. It extends Unity's philosophy of real-time run-time editing and inspection by allowing you to edit much more than just serialized fields.

## Device Inspection

Inspect and edit the hierarchy on device through a connected editor, particularly useful for those working with mobile only features, VR devices and console. If you want to inspect an object's state to debug a device only SDK or tune a value that only works on device this will help a lot!

## Edit fields and properties 
Inspect and edit fields and properties on components, including statics and many more than natively supported in Unity's inspector.

## Fire methods

Fire arbitrary methods on components with support for parameters and return types.


----------


**Coming soon**
> - Support for static classes found through Assemblies.
> - Fire events.


# How Sidekick works

## Unified API

All interactions are handled through a single unified API. This means if you're editing fields on device or in editor it uses a single code path. It may seem a little overkill for every request to be packed into binary then unpacked when just working in editor, but the unified workflow makes it much simpler to add new requests and have them supported in both use-cases without writing duplicate code.

### Current requests:
**GetHierarchy** - Fetches a list of scenes with the hierarchy of transforms inside them.

**GetGameObject** - Fetches a list of components given a fully qualified path to a game object.

**SetVariable** - Sets a field or property given an Object's instance ID and a value.

**InvokeMethod** - Fires a method given an instance ID, method name and any parameters.

## Editor Inspector

## Remote Inspector


### Note on Code Stripping

Note that Sidekick heavily uses reflection which relies on signatures being present in the remote assembly. For example if none of your code uses the background color property on a Camera component then that property may be stripped out at build time to keep build times and sizes down. In this case if you wanted to try changing the camera background color using Sidekick, reflection would not be able to find the property because it no longer exists. IL2CPP backend builds do bytecode stripping by default ([details here](https://docs.unity3d.com/Manual/IL2CPP-BytecodeStripping.html)), but by creating one or more link.xml files in Assets we can define components that shouldn't have stripping enabled. If you have any issues with missing fields/properties then try creating a link.xml file as outlined in the Unity Docs link.

# Getting Started
## Installation

You can either download the latest version directly from the repository [here](https://github.com/sabresaurus/Sidekick/archive/master.zip) or check out our past releases [here](https://github.com/sabresaurus/Sidekick/releases). Past releases are likely to be more stable but won't be as up to date.

Once you have downloaded the zip, for installation into existing projects copy the contents of the extracted Assets folder into your project's Assets folder.

Sidekick requires Unity 5.6.0 or higher, although is principally developed on Unity 2017 and 2018. It has been tested in editor, on mobile, WebGL and on console. If you find a platform or a Unity version (> 5.6) that it doesn't support please create an [issue](https://github.com/sabresaurus/Sidekick/issues) and we'll add support for it as soon as possible.

## Running Sidekick

Open Sidekick from the menu bar **Tools / Sidekick**, this will open the Sidekick Inspector window. By default this targets your selection in the local editor, but clicking the Remote button will open a Remote Hierarchy window.

The Remote Hierarchy window uses the connected player (as specified in the Console connected players or Profiler window). To use the Remote Hierarchy window you must first build your project now that Sidekick has been imported. The Sidekick editor code will connect to the player code in the remote build in a request-response structure. Once built and connected you should see a connected player entry appear at the top of the Remote window, click Refresh Hierarchy to show the device's hierarchy and begin inspection.

# Get Involved

Sidekick by design is built to be optimized for extending. Want to help? Take a look at the [issues](https://github.com/sabresaurus/Sidekick/issues) page to see tasks that are good to start on, if you're unsure about one or want advice just post a comment on one of those issues.

# License

Sidekick is licensed under MIT, see **LICENSE** for details.
