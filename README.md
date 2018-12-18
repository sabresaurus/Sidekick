<img align="right" src="https://user-images.githubusercontent.com/17784523/50124375-4c306e80-025c-11e9-878e-9aa5ff24e14a.png" width="400" />

# What is Sidekick?

Sidekick is a set of tools that allow you to edit fields, properties and invoke methods in deployed Unity projects on device and Unity editors. It extends Unity's philosophy of real-time run-time editing and inspection by allowing you to edit much more than just serialised fields.

## Device Inspection

Inspect and edit the hierarchy on device through a connected editor, particularly useful for those working with mobile only features, VR devices and console. If you want to inspect an object's state to debug a device only SDK or tune a value that only works on device this will help a lot!

## Edit fields and properties 
Inspect and edit fields and properties on components, including statics and many more than natively supported in Unity's inspector

## Fire methods

Fire arbitrary methods on components with support for parameters and return types


----------


**Coming soon**
> - Support for static classes found through Assemblies
> - Fire events


# How Sidekick works

## Unified API

All interactions are handled through a single unified API. This means if you're editing fields on device or in editor it uses a single code path. It may seem a little overkill for every request to be packed into binary then unpacked when just working in editor, but the unified workflow makes it much simpler to add new requests and have them supported in both use-cases without writing duplicate code.

### Current requests:
**GetHierarchy** - Fetches a list of scenes with the hierarchy of transforms inside them

**GetGameObject** - Fetches a list of components given a fully qualified path to a game object

**SetVariable** - Sets a field or property given an Object's instance ID and a value

**InvokeMethod** - Fires a method given an instance ID, method name and any parameters

## Editor Inspector

## Remote Inspector


### Note on Code Stripping

Note that Sidekick heavily uses reflection which relies on signatures being present in the remote assembly. For example if none of your code uses the background color property on a Camera component then that property may be stripped out at build time to keep build times and sizes down. In this case if you wanted to try changing the camera background color using Sidekick, then reflection would not be able to find the property (because it doesn't exist). IL2CPP backend builds do bytecode stripping by default ([details here](https://docs.unity3d.com/Manual/IL2CPP-BytecodeStripping.html)), but by creating one or more link.xml files in Assets we can define components that shouldn't have stripping enabled. If you have any issues with missing fields/properties then try creating a link.xml file as outlined in the Unity Docs link.

# Getting Started
## Installation

You can either download the latest version directly from the repository [here](https://github.com/sabresaurus/Sidekick/archive/master.zip) or check out our past releases [here](https://github.com/sabresaurus/Sidekick/releases). Past releases are likely to be more stable than what is in the repository but won't be as up to date.

Once you have downloaded the zip, unzip it somewhere in your project's Assets folder.

Sidekick requires Unity 5.6.0 or higher, although is principally developed on Unity 2017 and 2018. It has been tested in editor, on mobile, WebGL and on console. If you find a platform or a Unity version (> 5.6) that it doesn't support please create an [issue](https://github.com/sabresaurus/Sidekick/issues) and we'll add support for it as soon as possible.

## Running Sidekick

Open Sidekick from the menu bar **Tools / Sidekick**, this will open the Sidekick Inspector window. By default this targets your selection in the local editor, but clicking the Remote button will open a Remote Hierarchy window.

## Remote Debugging With Sidekick

The Remote Hierarchy window listens for players that have been built with Sidekick and automatically connects to those on the same network. Sidekick will automatically instantiate a RuntimeSidekickBridge object on startup in development builds (not release builds), the bridge handles broadcasting so that editors can find it and also sends responses to requests made by editors.

To use the Remote Hierarchy window, follow these steps:
- Build and deploy your project, making sure that Development Build is ticked in build settings.
- Wait for the device to appear in the remote window. Once connected you should see a connected player entry appear at the top of the Remote window, click Refresh Hierarchy to show the device's hierarchy and begin inspection.

# Get Involved

Sidekick is built by design to be optimised for extending. Want to help? Take a look at the [issues](https://github.com/sabresaurus/Sidekick/issues) page to see tasks that are good to start on, if you're unsure about one or want advice just post a comment on one of those issues.

Join us on Discord [here](https://discord.gg/6njbyaq)

# License

Sidekick is licensed under MIT, see **LICENSE** for details.
