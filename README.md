# CodeListener

Codelistener is part of the plugin [RhinoPython](https://github.com/ccc159/PythonScript). It is the RhinoPython server that runs in Rhino that listens to VS Code editor.

## Installation & Usage

For the instructions of installation and usage please refer to [RhinoPython readme](https://github.com/ccc159/PythonScript/blob/master/README.md).

## Commands

- **CodeListener**:  Start CodeListener in the background.
- **StopCodeListener**: Stop CodeListener, to allow other rhino instances to run CodeListener. Automatically stopped if current rhino instance exits.
- **CodeListenerVersion**: Check current CodeListener version.
- **CodeListenerExecute**: Allow Rhino to execute a file with given path. Can be used in combination with keyboard shortcut. For instance: `F2` binds to `_-CodeListenerExecute "C:\Program Files\Rhinoceros 5 (64-bit)\Plug-ins\IronPython\RhinoIronPython.chm" _Enter`
- **ResetScriptEngine**: Reset Rhino python script engine.