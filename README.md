# Sidekick Inspection Tools for Unity

[![openupm](https://img.shields.io/npm/v/com.sabresaurus.sidekick?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.sabresaurus.sidekick/) [![GitHub](https://img.shields.io/github/license/sabresaurus/Sidekick)](https://github.com/sabresaurus/Sidekick/blob/master/LICENSE.md) [![PRs Welcome](https://img.shields.io/badge/PRs-welcome-blue.svg)](http://makeapullrequest.com)

<img align="right" src="https://user-images.githubusercontent.com/17784523/126076796-cfd27caf-ee21-4a64-9a73-d45bd83a50b5.png" width="400" />

Sidekick is a set of tools that allow you to edit fields, properties and invoke methods in Unity's editor. It extends Unity's philosophy of real-time run-time editing and inspection by allowing you to edit much more than just serialised fields.

## Edit fields and properties 
Inspect and edit fields and properties on components, including statics and many more than natively supported in Unity's inspector

## Inspect hidden objects

Selection helpers allow you to select hidden assets that can't be selected in the Project window as well as runtime objects (such as Editor Windows, Scene Views, custom Editors, ECS Systems, etc) allowing you to debug and inspect all sorts of editor nuances.

## Modify Entities (ECS)

Sidekick supports writing to the fields on ECS Entities, simply select an entity in the Entity Debugger with Sidekick open and the fields will be displayed and change be dynamically changed.

## Fire methods and events

Fire arbitrary methods on components with support for parameters and return types

# License

Sidekick is licensed under MIT, see **LICENSE** for details.

# Installation

<details>
<summary>Add from OpenUPM <em>| via scoped registry, recommended</em></summary>

This package is available on OpenUPM: https://openupm.com/packages/com.sabresaurus.sidekick

To add it the package to your project:

- open `Edit/Project Settings/Package Manager`
- add a new Scoped Registry:
  ```
  Name: OpenUPM
  URL:  https://package.openupm.com/
  Scope(s): com.sabresaurus
  ```
- click <kbd>Save</kbd>
- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `com.sabresaurus.sidekick`
- click <kbd>Add</kbd>
</details>

<details>
<summary>Add from GitHub | <em>not recommended, no updates through UPM</em></summary>

You can also add it directly from GitHub on Unity 2020.3+. Note that you won't be able to receive updates through Package Manager this way, you'll have to update manually.

- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `https://github.com/sabresaurus/Sidekick.git`
- click <kbd>Add</kbd>  
**or**  
- Edit your `Packages/manifest.json` file to contain `"com.sabresaurus.sidekick": "https://github.com/sabresaurus/Sidekick.git"`,
  
To update the package with new changes, remove the lock from the `Packages/packages-lock.json` file.
</details>

To open Sidekick go to `Window → Sidekick`

# Remote Actions

[Previously](https://github.com/sabresaurus/Sidekick/tree/pre-remote-removal) we were hoping to include to make Sidekick work with remote builds, this however massively complicated the simplicity of Sidekick and the project has been abandoned. The network code has been split out into [Remote Actions](https://github.com/sabresaurus/Remote-Actions)
